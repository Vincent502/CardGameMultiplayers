using System;
using System.IO;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Implémentation du logger : Debug.Log + fichier dans persistentDataPath/Rapport (compatible Windows et mobile).
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
            string payload = data != null ? data.ToString() : "";
            string line = $"{_sequence}\t{DateTime.UtcNow:O}\t{eventType}\t{payload}";
            Debug.Log($"[CardGame] {eventType}: {payload}");
            if (_logPath != null)
            {
                try { File.AppendAllText(_logPath, line + "\n"); }
                catch { }
            }
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
