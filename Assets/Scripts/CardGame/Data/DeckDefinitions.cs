using System.Collections.Generic;
using CardGame.Core;

namespace CardGame.Data
{
    /// <summary>
    /// Définition des decks Magicien et Guerrier selon carte_spec_complete.md (34 cartes, 4 obligatoires).
    /// </summary>
    public static class DeckDefinitions
    {
        public static IReadOnlyList<(CardData card, int count)> GetMagicienDeck()
        {
            return new List<(CardData, int)>
            {
                // Obligatoires Equipé
                (new CardData(CardId.CatalyseurArcanaiqueRestraint, "Catalyseur arcanaique restraint", CardType.Equipe, 0), 1),
                (new CardData(CardId.RuneEnergieArcanique, "Rune d'énergie arcanique", CardType.Equipe, 6), 1),
                (new CardData(CardId.RuneEssenceArcanique, "Rune d'essence arcanique", CardType.Equipe, 1), 1),
                (new CardData(CardId.RuneForceArcanique, "Rune de force arcanique", CardType.Equipe, 4), 1),
                // Autres
                (new CardData(CardId.ExplosionMagieEphemere, "Explosion de magie éphémère", CardType.Normal, 1), 1),
                (new CardData(CardId.Divination, "Divination", CardType.Normal, 1), 3),
                (new CardData(CardId.ArmurePsychique, "Armure psychique", CardType.Ephemere, 3), 3),
                (new CardData(CardId.BouleDeFeu, "Boule de feu", CardType.Ephemere, 3), 3),
                (new CardData(CardId.AttaquePlus, "Attaque +", CardType.Normal, 2), 2),
                (new CardData(CardId.DefensePlus, "Défense +", CardType.Normal, 2), 2),
                (new CardData(CardId.Concentration, "Concentration", CardType.Ephemere, 2), 2),
                (new CardData(CardId.LienKarmique, "Lien karmique", CardType.Normal, 2), 1),
                (new CardData(CardId.Defense, "Défense", CardType.Normal, 1), 3),
                (new CardData(CardId.Galvanisation, "Galvanisation", CardType.Normal, 1), 1),
                (new CardData(CardId.Evaluation, "Evaluation", CardType.Ephemere, 1), 2),
                (new CardData(CardId.Attaque, "Attaque", CardType.Normal, 1), 3),
                (new CardData(CardId.OrageDePoche, "Orage de poche", CardType.Ephemere, 3), 2),
                (new CardData(CardId.GlaceLocalisee, "Glace localisée", CardType.Ephemere, 1), 2),
            };
        }

        public static IReadOnlyList<(CardData card, int count)> GetGuerrierDeck()
        {
            return new List<(CardData, int)>
            {
                // Obligatoires Equipé
                (new CardData(CardId.HacheOublie, "Hache de l'oublié", CardType.Equipe, 0), 1),
                (new CardData(CardId.RuneEnduranceOublie, "Rune d'endurance de l'oublié", CardType.Equipe, 1), 1),
                (new CardData(CardId.RuneProtectionOublie, "Rune de protection de l'oublié", CardType.Equipe, 2), 1),
                (new CardData(CardId.RuneAgressiviteOublie, "Rune d'agressivité de l'oublié", CardType.Equipe, 5), 1),
                // Autres
                (new CardData(CardId.Repositionnement, "Repositionnement", CardType.Normal, 0), 2),
                (new CardData(CardId.ContreAttaque, "Contre-attaque", CardType.Rapide, 0), 3),
                (new CardData(CardId.Parade, "Parade", CardType.Rapide, 1), 3),
                (new CardData(CardId.PositionOffensive, "Position offensive", CardType.Ephemere, 1), 2),
                (new CardData(CardId.AppuisSolide, "Appuis solide", CardType.Ephemere, 1), 3),
                (new CardData(CardId.DefenseLourde, "Défense lourde", CardType.Normal, 1), 2),
                (new CardData(CardId.PositionDefensive, "Position défensive", CardType.Ephemere, 1), 2),
                (new CardData(CardId.SouffleEternel, "Souffle éternel", CardType.Ephemere, 0), 1),
                (new CardData(CardId.DisciplineEternel, "Discipline éternel", CardType.Normal, 3), 1),
                (new CardData(CardId.Guillotine, "Guillotine", CardType.Normal, 1), 2),
                (new CardData(CardId.FendoireMortel, "Fendoire mortel", CardType.Ephemere, 1), 1),
                (new CardData(CardId.AttaqueLourde, "Attaque lourde", CardType.Normal, 2), 2),
                (new CardData(CardId.AttaqueLegere, "Attaque légère", CardType.Normal, 1), 3),
                (new CardData(CardId.AttaqueTactique, "Attaque tactique", CardType.Normal, 0), 3),
            };
        }

        /// <summary>Retourne les données d'une carte par id (pour résolution d'effets).</summary>
        public static CardData GetCard(CardId id)
        {
            foreach (var (card, _) in GetMagicienDeck())
                if (card.Id == id) return card;
            foreach (var (card, _) in GetGuerrierDeck())
                if (card.Id == id) return card;
            return default;
        }
    }
}
