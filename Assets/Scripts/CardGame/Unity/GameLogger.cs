using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Implémentation du logger : Debug.Log + fichier dans persistentDataPath/Rapport (compatible Windows et mobile).
    /// Format lisible : une entrée par ligne, payload en JSON.
    /// </summary>
    public class GameLogger : IGameLogger
    {
        private readonly string _logPath;
        private readonly string _reportId;
        private readonly DateTime _startedAt;
        private int _sequence;

        public GameLogger(bool writeToFile = true)
        {
            _reportId = $"cardgame_{DateTime.Now:yyyyMMdd_HHmmss}";
            _startedAt = DateTime.UtcNow;

            if (writeToFile)
            {
                string rapportDir = Path.Combine(Application.persistentDataPath, "Rapport");
                try
                {
                    if (!Directory.Exists(rapportDir))
                        Directory.CreateDirectory(rapportDir);
                    _logPath = Path.Combine(rapportDir, $"{_reportId}.log");
                    string header = $"#META\t{{\"id\":\"{_reportId}\",\"startedAt\":\"{_startedAt:O}\"}}\n";
                    File.WriteAllText(_logPath, header);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CardGame] Impossible de créer le dossier Rapport: {ex.Message}");
                    _logPath = null;
                }
            }
            else
            {
                _logPath = null;
            }
        }

        public void Log(string eventType, object data)
        {
            _sequence++;
            string payload = data != null ? ToReadableJson(data.ToString()) : "{}";
            int turn = ExtractTurnFromPayload(payload);
            string line = $"{_sequence}\t{DateTime.UtcNow:O}\t{turn}\t{eventType}\t{payload}";
            Debug.Log($"[CardGame] {eventType}: {payload}");
            if (_logPath != null)
            {
                try { File.AppendAllText(_logPath, line + "\n"); }
                catch { }
            }
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
            if (_logPath == null || state == null) return;
            try
            {
                string winner = state.WinnerIndex >= 0 ? $"Joueur {state.WinnerIndex + 1}" : "";
                string deck1 = state.Players.Length > 0 ? state.Players[0].DeckKind.ToString() : "";
                string deck2 = state.Players.Length > 1 ? state.Players[1].DeckKind.ToString() : "";
                string summary = $"{{\"id\":\"{_reportId}\",\"startedAt\":\"{_startedAt:O}\",\"endedAt\":\"{DateTime.UtcNow:O}\",\"winner\":\"{winner}\",\"turnCount\":{state.TurnCount},\"deckJoueur1\":\"{deck1}\",\"deckJoueur2\":\"{deck2}\"}}";
                File.AppendAllText(_logPath, $"#SUMMARY\t{summary}\n");
            }
            catch { }
        }
    }
}
