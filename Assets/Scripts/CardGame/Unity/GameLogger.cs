using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Implémentation du logger : Debug.Log + fichier dans persistentDataPath/Rapport/Historique (compatible Windows et mobile).
    /// Format lisible : une entrée par ligne, payload en JSON.
    /// </summary>
    public class GameLogger : IGameLogger
    {
        private readonly string _logPath;
        private readonly string _reportId;
        private readonly DateTime _startedAt;
        private int _sequence;
        private readonly SessionStats _sessionStats;
        private readonly ProfileManager.GameMode _gameMode;
        private readonly int _localPlayerIndex;
        private readonly string _namePlayer1;
        private readonly string _namePlayer2;

        public GameLogger(bool writeToFile = true, SessionStats sessionStats = null, ProfileManager.GameMode gameMode = ProfileManager.GameMode.Solo,
            string namePlayer1 = null, string namePlayer2 = null, int localPlayerIndex = 0)
        {
            _reportId = $"cardgame_{DateTime.Now:yyyyMMdd_HHmmss}";
            _startedAt = DateTime.UtcNow;

            if (writeToFile)
            {
                string rapportDir = Path.Combine(Application.persistentDataPath, "Rapport", "Historique");
                try
                {
                    if (!Directory.Exists(rapportDir))
                        Directory.CreateDirectory(rapportDir);
                    GameReportManager.PruneOldLogs();
                    _logPath = Path.Combine(rapportDir, $"{_reportId}.log");
                    string header = $"#META\t{{\"id\":\"{_reportId}\",\"startedAt\":\"{_startedAt:O}\"}}\n";
                    File.WriteAllText(_logPath, header);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardGame] Impossible de créer le dossier Rapport/Historique: {ex.Message}");
                    _logPath = null;
                }
            }
            else
            {
                _logPath = null;
            }
            _sessionStats = sessionStats;
            _gameMode = gameMode;
            _localPlayerIndex = localPlayerIndex;
            _namePlayer1 = !string.IsNullOrWhiteSpace(namePlayer1) ? namePlayer1 : "Joueur 1";
            _namePlayer2 = !string.IsNullOrWhiteSpace(namePlayer2) ? namePlayer2 : "Joueur 2";
            GameHistoryBuffer.Clear();
        }

        public void Log(string eventType, object data)
        {
            _sequence++;
            string payload = data != null ? ToReadableJson(data.ToString()) : "{}";
            payload = ReplacePlayerNamesInPayload(payload);
            int turn = ExtractTurnFromPayload(payload);
            string line = $"{_sequence}\t{DateTime.UtcNow:O}\t{turn}\t{eventType}\t{payload}";
            Debug.Log($"[CardGame] {eventType}: {payload}");
            if (_logPath != null)
            {
                try { File.AppendAllText(_logPath, line + "\n"); }
                catch (Exception ex) { Debug.LogWarning($"[CardGame] Erreur écriture log: {ex.Message}"); }
            }
            if (_sessionStats != null)
                ProfileManager.OnGameEvent(_sessionStats, eventType, payload);
            string displayText = FormatDisplayText(eventType, payload);
            GameHistoryBuffer.Add(eventType, turn, displayText);
        }

        private string FormatDisplayText(string eventType, string payload)
        {
            try
            {
                var entry = new GameReportManager.ReportEntry { Event = eventType, Data = payload };
                var record = entry.ToActivityRecord();
                string display = record.Detail?.ToDisplayText(eventType);
                return !string.IsNullOrEmpty(display) ? display : $"[{eventType}]";
            }
            catch
            {
                return $"[{eventType}]";
            }
        }

        private string ReplacePlayerNamesInPayload(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return payload;
            return payload.Replace("\"Joueur 1\"", $"\"{EscapeJson(_namePlayer1)}\"").Replace("\"Joueur 2\"", $"\"{EscapeJson(_namePlayer2)}\"");
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>Extrait turnNumber ou turnCount du payload JSON pour le modèle timeline.</summary>
        private static int ExtractTurnFromPayload(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return -1;
            var m = Regex.Match(payload, @"""turnNumber""\s*:\s*(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int t)) return t;
            m = Regex.Match(payload, @"""turnCount""\s*:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out t) ? t : -1;
        }

        /// <summary>Convertit le format C# { key = value } en JSON lisible {"key": "value"}.</summary>
        private static string ToReadableJson(string csharpFormat)
        {
            if (string.IsNullOrWhiteSpace(csharpFormat)) return "{}";
            var sb = new System.Text.StringBuilder();
            sb.Append("{ ");
            var matches = Regex.Matches(csharpFormat, @"(\w+)\s*=\s*([^,}]+)");
            for (int i = 0; i < matches.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                string key = matches[i].Groups[1].Value;
                string val = matches[i].Groups[2].Value.Trim();
                sb.Append('"').Append(key).Append("\": ");
                if (int.TryParse(val, out _) || val == "True" || val == "False")
                    sb.Append(val.ToLowerInvariant());
                else
                    sb.Append('"').Append(val.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public void FinalizeReport(GameState state)
        {
            if (state == null) return;
            try
            {
                if (_logPath != null)
                {
                    string winner = state.WinnerIndex >= 0 ? (state.WinnerIndex == 0 ? _namePlayer1 : _namePlayer2) : "";
                    string deck1 = state.Players.Length > 0 ? state.Players[0].DeckKind.ToString() : "";
                    string deck2 = state.Players.Length > 1 ? state.Players[1].DeckKind.ToString() : "";
                    string summary = $"{{\"id\":\"{_reportId}\",\"startedAt\":\"{_startedAt:O}\",\"endedAt\":\"{DateTime.UtcNow:O}\",\"winner\":\"{EscapeJson(winner)}\",\"winnerIndex\":{state.WinnerIndex},\"turnCount\":{state.TurnCount},\"deckJoueur1\":\"{deck1}\",\"deckJoueur2\":\"{deck2}\",\"namePlayer1\":\"{EscapeJson(_namePlayer1)}\",\"namePlayer2\":\"{EscapeJson(_namePlayer2)}\"}}";
                    File.AppendAllText(_logPath, $"#SUMMARY\t{summary}\n");
                }
                if (_sessionStats != null)
                {
                    Debug.Log("[CardGame] Finalisation du rapport — mise à jour du profil.");
                    ProfileManager.FinalizeGame(state, _sessionStats, _gameMode, _localPlayerIndex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardGame] Erreur FinalizeReport: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
