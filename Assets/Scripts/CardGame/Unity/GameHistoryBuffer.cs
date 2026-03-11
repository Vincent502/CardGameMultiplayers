using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>
    /// Buffer en mémoire des entrées de log de la partie en cours. Utilisé pour afficher l'historique en temps réel.
    /// </summary>
    public static class GameHistoryBuffer
    {
        [Serializable]
        public struct Entry
        {
            public int Seq;
            public string Time;
            public int Turn;
            public string EventType;
            public string DisplayText;

            public string TimeShort => !string.IsNullOrEmpty(Time) && Time.Length >= 19 ? Time.Substring(11, 8) : Time ?? "";
        }

        private static readonly List<Entry> _entries = new List<Entry>();
        private static int _sequence;

        public static IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Efface le buffer (appelé au démarrage d'une nouvelle partie).</summary>
        public static void Clear()
        {
            _entries.Clear();
            _sequence = 0;
        }

        /// <summary>Ajoute une entrée (appelé par GameLogger).</summary>
        public static void Add(string eventType, int turn, string displayText)
        {
            _sequence++;
            _entries.Add(new Entry
            {
                Seq = _sequence,
                Time = DateTime.UtcNow.ToString("O"),
                Turn = turn,
                EventType = eventType,
                DisplayText = displayText ?? $"[{eventType}]"
            });
        }
    }
}
