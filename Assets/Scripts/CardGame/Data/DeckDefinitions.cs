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
                (new CardData(CardId.CatalyseurArcanaiqueRestraint, "Catalyseur arcanaique restraint", CardType.Equipe, 0, "Donne 1 bouclier à chaque fois que vous frappez"), 1),
                (new CardData(CardId.RuneEnergieArcanique, "Rune d'énergie arcanique", CardType.Equipe, 6, "Pioche 2 cartes supplémentaires en début de tour"), 1),
                (new CardData(CardId.RuneEssenceArcanique, "Rune d'essence arcanique", CardType.Equipe, 1, "Donne 5 bouclier à la fin du tour, si résistance est à 0"), 1),
                (new CardData(CardId.RuneForceArcanique, "Rune de force arcanique", CardType.Equipe, 4, "Inflige 2 × 1 dégât quand vous frappez"), 1),
                (new CardData(CardId.ExplosionMagieEphemere, "Explosion de magie éphémère", CardType.Normal, 1, "Inflige (cartes défaussées ce tour) × 2 dégâts"), 1),
                (new CardData(CardId.Divination, "Divination", CardType.Normal, 1, "Pioche 2 cartes, choisit laquelle poser au-dessus du deck"), 3),
                (new CardData(CardId.ArmurePsychique, "Armure psychique", CardType.Ephemere, 3, "23 bouclier durant 2 tours"), 3),
                (new CardData(CardId.BouleDeFeu, "Boule de feu", CardType.Ephemere, 3, "15 dégâts"), 3),
                (new CardData(CardId.AttaquePlus, "Attaque +", CardType.Normal, 2, "9 dégâts"), 2),
                (new CardData(CardId.DefensePlus, "Défense +", CardType.Normal, 2, "15 bouclier"), 2),
                (new CardData(CardId.Concentration, "Concentration", CardType.Ephemere, 2, "3 Force ; prochain tour +3 Résistance"), 2),
                (new CardData(CardId.LienKarmique, "Lien karmique", CardType.Normal, 2, "3 Résistance pendant 3 tours"), 1),
                (new CardData(CardId.Defense, "Défense", CardType.Normal, 1, "4 bouclier ; +4 si aucune attaque ce tour"), 3),
                (new CardData(CardId.Galvanisation, "Galvanisation", CardType.Normal, 1, "1 Force par carte en main jusqu'à fin du tour"), 1),
                (new CardData(CardId.Evaluation, "Evaluation", CardType.Ephemere, 1, "Pioche 3 cartes"), 2),
                (new CardData(CardId.Attaque, "Attaque", CardType.Normal, 1, "5 dégâts"), 3),
                (new CardData(CardId.OrageDePoche, "Orage de poche", CardType.Ephemere, 3, "1 dégât avant fin de chaque tour, 3 tours"), 2),
                (new CardData(CardId.GlaceLocalisee, "Glace localisée", CardType.Ephemere, 1, "Gèle un équipement adverse (frappe pour dégeler)"), 2),
            };
        }

        public static IReadOnlyList<(CardData card, int count)> GetGuerrierDeck()
        {
            return new List<(CardData, int)>
            {
                (new CardData(CardId.HacheOublie, "Hache de l'oublié", CardType.Equipe, 0, "5 dégâts, 1 fois par tour"), 1),
                (new CardData(CardId.RuneEnduranceOublie, "Rune d'endurance de l'oublié", CardType.Equipe, 1, "Donne 3 PV au début du tour"), 1),
                (new CardData(CardId.RuneProtectionOublie, "Rune de protection de l'oublié", CardType.Equipe, 2, "Si 2 frappes enchaînées : 2 bouclier"), 1),
                (new CardData(CardId.RuneAgressiviteOublie, "Rune d'agressivité de l'oublié", CardType.Equipe, 5, "À chaque frappe : +1 Force jusqu'à fin du tour"), 1),
                (new CardData(CardId.Repositionnement, "Repositionnement", CardType.Normal, 0, "2 bouclier et pioche 1 carte"), 2),
                (new CardData(CardId.ContreAttaque, "Contre-attaque", CardType.Rapide, 0, "Annule une attaque adverse et inflige 2 dégâts"), 3),
                (new CardData(CardId.Parade, "Parade", CardType.Rapide, 1, "Annule une attaque adverse"), 3),
                (new CardData(CardId.PositionOffensive, "Position offensive", CardType.Ephemere, 1, "Octroie 1 Force"), 2),
                (new CardData(CardId.AppuisSolide, "Appuis solide", CardType.Ephemere, 1, "Votre arme fait 1 dégât supplémentaire"), 3),
                (new CardData(CardId.DefenseLourde, "Défense lourde", CardType.Normal, 1, "10 bouclier"), 2),
                (new CardData(CardId.PositionDefensive, "Position défensive", CardType.Ephemere, 1, "Donne 1 Résistance"), 2),
                (new CardData(CardId.SouffleEternel, "Souffle éternel", CardType.Ephemere, 0, "Rend 15 PV ; va au cimetière si après Discipline éternel"), 1),
                (new CardData(CardId.DisciplineEternel, "Discipline éternel", CardType.Normal, 3, "Invincible jusqu'au prochain tour"), 1),
                (new CardData(CardId.Guillotine, "Guillotine", CardType.Normal, 1, "Attaque avec arme, Force × 2, met fin au tour"), 2),
                (new CardData(CardId.FendoireMortel, "Fendoire mortel", CardType.Ephemere, 1, "20 dégâts"), 1),
                (new CardData(CardId.AttaqueLourde, "Attaque lourde", CardType.Normal, 2, "7 dégâts, 4 bouclier"), 2),
                (new CardData(CardId.AttaqueLegere, "Attaque légère", CardType.Normal, 1, "3 dégâts, 2 bouclier"), 3),
                (new CardData(CardId.AttaqueTactique, "Attaque tactique", CardType.Normal, 0, "2 dégâts, 1 bouclier"), 3),
            };
        }

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
