using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère les rapports de parties dans persistentDataPath/Rapport/Historique (compatible Windows et mobile).
    /// </summary>
    public static class GameReportManager
    {
        private static string RapportPath => Path.Combine(Application.persistentDataPath, "Rapport", "Historique");

        private const int MaxLogsToKeep = 10;

        /// <summary>Supprime les anciens logs pour ne garder que les 10 plus récents (appelé avant création d'un nouveau log).</summary>
        public static void PruneOldLogs()
        {
            if (!Directory.Exists(RapportPath)) return;
            var files = Directory.GetFiles(RapportPath, "*.log");
            int toKeep = MaxLogsToKeep - 1;
            if (files.Length <= toKeep) return;

            var sorted = new List<(string path, DateTime time)>();
            foreach (string path in files)
            {
                try
                {
                    var info = new FileInfo(path);
                    sorted.Add((path, info.LastWriteTimeUtc));
                }
                catch { }
            }
            sorted.Sort((a, b) => b.time.CompareTo(a.time));

            for (int i = toKeep; i < sorted.Count; i++)
            {
                try
                {
                    File.Delete(sorted[i].path);
                    Debug.Log($"[GameReportManager] Ancien log supprimé : {Path.GetFileName(sorted[i].path)}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameReportManager] Impossible de supprimer {sorted[i].path}: {ex.Message}");
                }
            }
        }

        /// <summary>Résumé d'une partie pour l'affichage dans la liste historique.</summary>
        [Serializable]
        public class ReportSummary
        {
            public string Id;
            public string StartedAt;
            public string EndedAt;
            public string Winner;
            public int TurnCount;
            public string DeckJoueur1;
            public string DeckJoueur2;
            public string FilePath;

            public string DisplayDate => !string.IsNullOrEmpty(EndedAt) && DateTime.TryParse(EndedAt, out var dt)
                ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : StartedAt;

            public string DisplayTitle => string.IsNullOrEmpty(Winner) || Winner == "Partie non terminée"
                ? $"{DeckJoueur1} vs {DeckJoueur2} - {Winner}"
                : $"{DeckJoueur1} vs {DeckJoueur2} - {Winner} gagne";
        }

        /// <summary>Rapport complet avec toutes les entrées de log.</summary>
        [Serializable]
        public class ReportEntry
        {
            public int Seq;
            public string Time;
            public int Turn;
            public string Event;
            public string Data;

            /// <summary>Convertit en ActivityRecord avec détails parsés.</summary>
            public ActivityRecord ToActivityRecord()
            {
                var record = new ActivityRecord
                {
                    Seq = Seq,
                    Time = Time,
                    Turn = Turn,
                    EventType = Event
                };
                record.Detail = ParseActivityDetail(Data);
                return record;
            }
        }

        /// <summary>Groupe d'événements d'un même tour (modèle timeline).</summary>
        [Serializable]
        public class TurnGroup
        {
            public int TurnIndex;
            public string Joueur;
            public int TurnNumber;
            public List<ReportEntry> Entries = new List<ReportEntry>();
        }

        [Serializable]
        public class FullReport
        {
            public ReportSummary Summary;
            public List<ReportEntry> Entries = new List<ReportEntry>();
            /// <summary>Entrées regroupées par tour (modèle timeline).</summary>
            public List<TurnGroup> TurnGroups = new List<TurnGroup>();
        }

        /// <summary>Liste tous les rapports disponibles, du plus récent au plus ancien.</summary>
        public static List<ReportSummary> GetAllSummaries()
        {
            var list = new List<ReportSummary>();
            if (!Directory.Exists(RapportPath)) return list;

            foreach (string filePath in Directory.GetFiles(RapportPath, "*.log"))
            {
                try
                {
                    var summary = ReadSummary(filePath);
                    if (summary != null)
                        list.Add(summary);
                }
                catch { /* ignorer les fichiers corrompus */ }
            }

            list.Sort((a, b) => string.Compare(b.EndedAt ?? b.StartedAt, a.EndedAt ?? a.StartedAt, StringComparison.Ordinal));
            return list;
        }

        /// <summary>Charge un rapport complet à partir de son chemin. Modèle timeline : regroupement par tour.</summary>
        public static FullReport LoadFullReport(string filePath)
        {
            var report = new FullReport();
            if (!File.Exists(filePath)) return report;

            var lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("#SUMMARY\t"))
                {
                    string json = line.Substring(8);
                    report.Summary = ParseSummaryJson(json);
                    if (report.Summary != null)
                        report.Summary.FilePath = filePath;
                }
                else if (line.StartsWith("#META\t"))
                    continue;
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    var entry = ParseLogLine(line);
                    if (entry != null)
                        report.Entries.Add(entry);
                }
            }

            BuildTurnGroups(report);
            if (report.Summary == null)
                report.Summary = new ReportSummary { FilePath = filePath };
            return report;
        }

        /// <summary>Parse une ligne de log. Supporte ancien format (4 cols) et nouveau (5 cols avec turn).</summary>
        private static ReportEntry ParseLogLine(string line)
        {
            var parts = line.Split(new[] { '\t' }, 5);
            if (parts.Length < 4 || !int.TryParse(parts[0], out int seq)) return null;

            int turn = -1;
            string time, evt, data;
            if (parts.Length >= 5 && int.TryParse(parts[2], out int t))
            {
                time = parts[1];
                turn = t;
                evt = parts[3];
                data = parts[4];
            }
            else
            {
                time = parts[1];
                evt = parts[2];
                data = parts[3];
            }
            return new ReportEntry { Seq = seq, Time = time, Turn = turn, Event = evt, Data = data };
        }

        /// <summary>Construit les groupes par tour à partir des entrées (StartTurn = nouveau tour).</summary>
        private static void BuildTurnGroups(FullReport report)
        {
            report.TurnGroups.Clear();
            TurnGroup current = null;
            foreach (var e in report.Entries)
            {
                if (e.Event == "StartTurn")
                {
                    string joueur = ExtractFromData(e.Data, "joueur");
                    string tn = ExtractFromData(e.Data, "turnNumber");
                    int turnNum = int.TryParse(tn, out int n) ? n : 0;
                    current = new TurnGroup
                    {
                        TurnIndex = report.TurnGroups.Count + 1,
                        Joueur = joueur ?? "?",
                        TurnNumber = turnNum
                    };
                    report.TurnGroups.Add(current);
                }
                if (current == null)
                {
                    current = new TurnGroup { TurnIndex = 0, Joueur = "Début", TurnNumber = 0 };
                    report.TurnGroups.Add(current);
                }
                current.Entries.Add(e);
            }
        }

        private static ReportSummary ReadSummary(string filePath)
        {
            string firstMeta = null;
            string gameStartLine = null;
            string victoryLine = null;
            string lastLine = null;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                while (!sr.EndOfStream)
                {
                    lastLine = sr.ReadLine();
                    if (lastLine != null)
                    {
                        if (lastLine.StartsWith("#META\t") && firstMeta == null)
                            firstMeta = lastLine.Substring(6).TrimStart();
                        if (lastLine.StartsWith("#SUMMARY\t"))
                            firstMeta = null;
                        if (gameStartLine == null && lastLine.Contains("\tGameStart\t"))
                            gameStartLine = lastLine;
                        if (lastLine.Contains("\tVictory\t"))
                            victoryLine = lastLine;
                    }
                }
            }

            if (lastLine != null && lastLine.StartsWith("#SUMMARY\t"))
            {
                var summary = ParseSummaryJson(lastLine.Substring(8));
                if (summary != null)
                {
                    summary.FilePath = filePath;
                    return summary;
                }
            }

            if (!string.IsNullOrEmpty(victoryLine))
            {
                var summary = ParseVictoryLine(victoryLine, firstMeta, filePath);
                if (summary != null) return summary;
            }

            return BuildSummaryFromPartial(firstMeta, gameStartLine, filePath);
        }

        private static ReportSummary ParseVictoryLine(string victoryLine, string metaJson, string filePath)
        {
            var parts = victoryLine.Split(new[] { '\t' }, 5);
            if (parts.Length < 4) return null;
            string data = parts.Length >= 5 ? parts[4] : parts[3];
            string gagnant = ExtractFromData(data, "gagnant");
            string turnCountStr = ExtractFromData(data, "turnCount");
            int turnCount = int.TryParse(turnCountStr, out int tc) ? tc : 0;
            string deck1 = ExtractFromData(data, "deckJoueur1");
            string deck2 = ExtractFromData(data, "deckJoueur2");
            if (string.IsNullOrEmpty(gagnant)) return null;

            string endedAt = parts.Length >= 2 ? parts[1] : "";
            var summary = new ReportSummary
            {
                FilePath = filePath,
                Winner = gagnant,
                TurnCount = turnCount,
                EndedAt = endedAt,
                DeckJoueur1 = deck1 ?? "?",
                DeckJoueur2 = deck2 ?? "?"
            };
            if (!string.IsNullOrEmpty(metaJson))
            {
                try
                {
                    var m = JsonUtility.FromJson<ReportSummaryJson>(metaJson);
                    summary.Id = m.id ?? "";
                    summary.StartedAt = m.startedAt ?? "";
                }
                catch { }
            }
            if (string.IsNullOrEmpty(summary.Id))
                summary.Id = Path.GetFileNameWithoutExtension(filePath);
            return summary;
        }

        private static ReportSummary BuildSummaryFromPartial(string metaJson, string gameStartLine, string filePath)
        {
            var summary = new ReportSummary { FilePath = filePath, Winner = "Partie non terminée", TurnCount = 0 };
            if (!string.IsNullOrEmpty(metaJson))
            {
                try
                {
                    var m = JsonUtility.FromJson<ReportSummaryJson>(metaJson);
                    summary.Id = m.id ?? "";
                    summary.StartedAt = m.startedAt ?? "";
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(gameStartLine))
            {
                var parts = gameStartLine.Split(new[] { '\t' }, 5);
                if (parts.Length >= 4)
                {
                    string data = parts.Length >= 5 ? parts[4] : parts[3];
                    summary.DeckJoueur1 = ExtractFromData(data, "deckJoueur1") ?? "?";
                    summary.DeckJoueur2 = ExtractFromData(data, "deckJoueur2") ?? "?";
                }
            }
            if (string.IsNullOrEmpty(summary.Id))
                summary.Id = Path.GetFileNameWithoutExtension(filePath);
            return summary;
        }

        /// <summary>Parse le JSON des détails d'activité en ActivityDetail.</summary>
        public static ActivityDetail ParseActivityDetail(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return new ActivityDetail();
            try
            {
                var detail = JsonUtility.FromJson<ActivityDetail>(data);
                return detail ?? new ActivityDetail();
            }
            catch
            {
                return new ActivityDetail();
            }
        }

        private static string ExtractFromData(string data, string key)
        {
            if (string.IsNullOrEmpty(data)) return null;
            // Format JSON : "key": "value" ou "key": 123
            var jsonMatch = Regex.Match(data, $"\"{key}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (jsonMatch.Success) return jsonMatch.Groups[1].Value;
            var jsonNumMatch = Regex.Match(data, $"\"{key}\"\\s*:\\s*([^,}}]+)", RegexOptions.IgnoreCase);
            if (jsonNumMatch.Success) return jsonNumMatch.Groups[1].Value.Trim();
            // Ancien format C# : key = value
            string search = key + " = ";
            int i = data.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            i += search.Length;
            int j = data.IndexOf(',', i);
            if (j < 0) j = data.IndexOf('}', i);
            if (j < 0) return null;
            return data.Substring(i, j - i).Trim();
        }

        private static ReportSummary ParseSummaryJson(string json)
        {
            try
            {
                var s = JsonUtility.FromJson<ReportSummaryJson>(json);
                return new ReportSummary
                {
                    Id = s.id,
                    StartedAt = s.startedAt,
                    EndedAt = s.endedAt,
                    Winner = s.winner,
                    TurnCount = s.turnCount,
                    DeckJoueur1 = s.deckJoueur1 ?? "",
                    DeckJoueur2 = s.deckJoueur2 ?? ""
                };
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        private class ReportSummaryJson
        {
            public string id;
            public string startedAt;
            public string endedAt;
            public string winner;
            public int turnCount;
            public string deckJoueur1;
            public string deckJoueur2;
        }
    }
}
