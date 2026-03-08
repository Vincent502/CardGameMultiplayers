using System;
using System.Collections.Generic;
using CardGame.Core;

namespace CardGame.Unity
{
    /// <summary>
    /// Statistiques collectées pendant une partie. Fusionnées dans le profil en fin de partie.
    /// </summary>
    public class SessionStats
    {
        public string DeckJoueur1 { get; private set; }
        public string DeckJoueur2 { get; private set; }
        public int TurnCount { get; private set; }
        public int? WinnerIndex { get; private set; }
        public string Gagnant { get; private set; }

        private readonly Dictionary<string, int> _cartesJouees = new Dictionary<string, int>();
        private int _degatsCeTour;
        private int _bouclierCeTour;
        private int _degatsInfligesTotal;
        private int _bouclierGagneTotal;
        private int _cartesPiocheesTotal;
        private int _maxBouclierUnCoup;
        private bool _contreAttaqueJouee;
        private int _currentTurnNumber;

        public IReadOnlyDictionary<string, int> CartesJouees => _cartesJouees;
        public int DegatsCeTour => _degatsCeTour;
        public int BouclierCeTour => _bouclierCeTour;
        public int DegatsInfligesTotal => _degatsInfligesTotal;
        public int BouclierGagneTotal => _bouclierGagneTotal;
        public int CartesPiocheesTotal => _cartesPiocheesTotal;
        public int MaxBouclierUnCoup => _maxBouclierUnCoup;
        public bool ContreAttaqueJouee => _contreAttaqueJouee;

        /// <summary>Enregistre un événement à partir du payload JSON.</summary>
        public void Record(string eventType, string payloadJson)
        {
            var d = GameReportManager.ParseActivityDetail(payloadJson);
            switch (eventType)
            {
                case "GameStart":
                    DeckJoueur1 = d.deckJoueur1 ?? "";
                    DeckJoueur2 = d.deckJoueur2 ?? "";
                    break;
                case "StartTurn":
                    _degatsCeTour = 0;
                    _bouclierCeTour = 0;
                    _currentTurnNumber = d.turnNumber;
                    break;
                case "EndTurn":
                case "EndTurnRequested":
                    _currentTurnNumber = d.turnNumber;
                    break;
                case "Victory":
                    WinnerIndex = d.winnerIndex;
                    Gagnant = d.gagnant ?? "";
                    TurnCount = d.turnCount;
                    break;
                case "Draw":
                    _cartesPiocheesTotal += d.drawn;
                    break;
                case "PlayCard":
                case "PlayRapid":
                case "RapidPlayed":
                    string cardId = d.cardId ?? d.carte ?? "Inconnu";
                    RecordCarteJouee(cardId);
                    if (cardId == "ContreAttaque") _contreAttaqueJouee = true;
                    break;
                case "DamageApplied":
                    int dmg = d.damageTotal > 0 ? d.damageTotal : d.baseDamage;
                    _degatsCeTour += dmg;
                    _degatsInfligesTotal += dmg;
                    break;
                case "ShieldApplied":
                case "ShieldBuffReapplied":
                    int shield = d.amount > 0 ? d.amount : (d.baseShield > 0 ? d.baseShield : ExtractShieldFromPayload(payloadJson));
                    _bouclierCeTour += shield;
                    _bouclierGagneTotal += shield;
                    if (shield > _maxBouclierUnCoup) _maxBouclierUnCoup = shield;
                    break;
            }
        }

        private void RecordCarteJouee(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (!_cartesJouees.TryGetValue(cardId, out int c))
                c = 0;
            _cartesJouees[cardId] = c + 1;
        }

        private static int ExtractShieldFromPayload(string json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            var m = System.Text.RegularExpressions.Regex.Match(json, @"""amount""\s*:\s*(\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int a)) return a;
            m = System.Text.RegularExpressions.Regex.Match(json, @"""baseShield""\s*:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out a) ? a : 0;
        }

        /// <summary>Met à jour le nombre de tours (appelé à la fin).</summary>
        public void SetTurnCount(int count) => TurnCount = count;

        /// <summary>Met à jour le deck du joueur 1 (fallback si GameStart n'a pas été parsé).</summary>
        public void SetDeckJoueur1(string deck) => DeckJoueur1 = deck ?? "";
    }
}
