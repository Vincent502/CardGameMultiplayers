using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Gère les rapports de parties dans persistentDataPath/Rapport (compatible Windows et mobile).
    /// </summary>
    public static class GameReportManager
    {
        private static string RapportPath => Path.Combine(Application.persistentDataPath, "Rapport");

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
            public string Event;
            public string Data;
        }

        [Serializable]
        public class FullReport
        {
            public ReportSummary Summary;
            public List<ReportEntry> Entries = new List<ReportEntry>();
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

        /// <summary>Charge un rapport complet à partir de son chemin.</summary>
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
                    var parts = line.Split(new[] { '\t' }, 4);
                    if (parts.Length >= 4 && int.TryParse(parts[0], out int seq))
                    {
                        report.Entries.Add(new ReportEntry
                        {
                            Seq = seq,
                            Time = parts[1],
                            Event = parts[2],
                            Data = parts[3]
                        });
                    }
                }
            }

            if (report.Summary == null)
                report.Summary = new ReportSummary { FilePath = filePath };
            return report;
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
            var parts = victoryLine.Split(new[] { '\t' }, 4);
            if (parts.Length < 4) return null;
            string data = parts[3];
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
                var parts = gameStartLine.Split(new[] { '\t' }, 4);
                if (parts.Length >= 4)
                {
                    string data = parts[3];
                    summary.DeckJoueur1 = ExtractFromData(data, "deckJoueur1") ?? "?";
                    summary.DeckJoueur2 = ExtractFromData(data, "deckJoueur2") ?? "?";
                }
            }
            if (string.IsNullOrEmpty(summary.Id))
                summary.Id = Path.GetFileNameWithoutExtension(filePath);
            return summary;
        }

        private static string ExtractFromData(string data, string key)
        {
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
