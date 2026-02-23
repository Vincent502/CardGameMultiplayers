using System;
using System.IO;
using CardGame.Core;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Implémentation du logger : Debug.Log + fichier optionnel (spec PROMPT - tout logger).
    /// </summary>
    public class GameLogger : IGameLogger
    {
        private readonly string _logPath;
        private int _sequence;

        public GameLogger(bool writeToFile = true)
        {
            _logPath = writeToFile ? Path.Combine(Application.persistentDataPath, $"cardgame_{DateTime.Now:yyyyMMdd_HHmmss}.log") : null;
            if (_logPath != null)
                File.WriteAllText(_logPath, $"=== CardGame Log {DateTime.Now:O} ===\n");
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
    }
}
