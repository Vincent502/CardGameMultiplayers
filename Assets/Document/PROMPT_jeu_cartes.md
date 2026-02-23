# Prompt : implémentation du jeu de cartes tour par tour

## Objectif

Implémenter un **jeu de cartes tour par tour** 2 joueurs (Magicien vs Guerrier) en respectant intégralement la spécification ci-dessous. **Toute action et tout changement d’état du jeu doivent être loggés** pour traçabilité, debug et rejouabilité.

---

## Plateforme et phasage

- **Moteur** : **Unity** (C#).
- **Cibles** : **PC** et **Mobile** (conception responsive / adaptée aux deux).

### Phase 1 (prioritaire) : version fonctionnelle duel vs bot

- **Scope** : une partie jouable **Joueur vs IA (bot)**.
- Le joueur humain choisit son deck (Magicien ou Guerrier), le bot utilise l’autre deck (ou un deck défini).
- Toutes les règles de **`carte_spec_complete.md`** doivent être respectées (tour, mana, cartes, effets, victoire).
- **Logging** : tout est loggé comme décrit dans la section « Exigence : logging ».
- L’architecture doit **préparer** une séparation nette entre :
  - **Moteur de jeu** (règles, état de la partie, résolution des effets) — sans dépendance réseau.
  - **Couche d’entrées** (actions du joueur humain vs décisions du bot).
  - Plus tard : **couche multijoueur P2P** qui remplacera le bot par les actions d’un joueur distant.

### Phase 2 (ultérieure) : multijoueur P2P

- Portage **multijoueur pair-à-pair (P2P)** : même moteur de jeu, les actions du « joueur 2 » viennent du réseau au lieu du bot.
- Ne pas implémenter le réseau dans la Phase 1 ; garder une interface (ex. « joueur 2 joue une carte ») qui pourra être branchée soit sur le bot, soit sur le P2P.

---

## Exigence : logging

**Tout doit être loggé.** À chaque étape du jeu, enregistrer (console, fichier, ou les deux) :

- **Début / fin de partie** : joueurs, deck choisi, premier joueur (tirage au sort).
- **Chaque tour** : joueur actif, numéro de tour, phase (début / résolution début / pioche / jeu / résolution fin / fin).
- **Pioche** : nombre de cartes piochées, cartes restantes dans le deck, mélange du cimetière si deck vide.
- **Défausse** : cartes défaussées en début de tour (main entière).
- **Bouclier** : remise à 0 en début de tour, gains et dégâts reçus (bouclier puis PV).
- **Mana** : mana disponible en début de tour, mana dépensé, mana restant (et si conservé pour Rapides).
- **Actions du joueur** : carte jouée (nom, type, coût), frappe déclarée, cartes Rapides jouées en réaction (par qui, en réponse à quoi).
- **Résolution des effets** : quel effet (carte ou équipement), cible, valeur (dégâts, bouclier, Force, Résistance), résultat (PV/bouclier avant → après).
- **Zones** : déplacement de cartes (main → plateau, main → cimetière, main → retirée du jeu, cimetière → deck).
- **Équipements** : activation après X rounds, gel (Glace localisée), rétablissement après un coup.
- **Victoire** : joueur gagnant, raison (PV adverses à 0).

Format recommandé : horodatage ou numéro d’ordre + type d’événement + données structurées (JSON ou clé-valeur), de façon à pouvoir rejouer une partie ou analyser un bug a posteriori.

---

## Spécification de référence

La règle du jeu est définie dans le fichier **`carte_spec_complete.md`** (même dossier). Résumé à respecter :

- **Types de cartes** : Equipé (4 obligatoires par deck, sur le board, activation après X rounds), Normal (→ cimetière), Éphémère (une fois par partie, retirée du jeu sauf exception Souffle éternel après Discipline éternel), Rapide (tour adverse, cumulables, mana de réaction).
- **Stats** : PV 100, bouclier (remis à 0 chaque début de tour, absorbe avant les PV), Force = dégâts = base × (1 + Force), Résistance = bouclier reçu = base × (1 + Résistance), Mana (1/2/3 selon tour, plafond 3 à partir du tour 3).
- **Tour** : 1 tour = 1 joueur. Début : défausse main, bouclier à 0. Puis résolution début de tour, pioche (3/4/5 cartes, max 5), jeu (avec fenêtre de réaction pour Rapides avant résolution des dégâts), résolution fin de tour, fin du tour.
- **Premier joueur** : tirage au sort.
- **Effets « durant X tours »** : 1 tour de jeu = 1 (quel que soit le joueur).
- **Victoire** : réduire les PV adverses à 0.

Les decks **Magicien** et **Guerrier** sont décrits en détail dans `carte_spec_complete.md` (cartes obligatoires Equipé + autres cartes, effets, types, coûts). Toutes les règles spéciales (Explosion « cartes consumées = défaussées ce tour », Divination choix carte sur le deck, Glace localisée gel + un coup pour rétablir, Défense bonus si aucune attaque, etc.) doivent être implémentées telles que spécifiées.

---

## Livrable attendu (Phase 1)

- **Unity** (C#), projet compatible **PC et Mobile**.
- **Moteur de jeu** : structure de données, moteur de tour, résolution des effets, formules Force/Résistance, indépendant du réseau et réutilisable pour le P2P plus tard.
- **Duel vs bot** : partie jouable Joueur vs IA ; le bot prend des décisions (jouer des cartes, frapper, réagir avec des Rapides) selon une stratégie simple ou heuristique.
- **Système de logging** couvrant tous les points listés ci-dessus, utilisable pour debug et relecture de partie.
- Utilisation de **`carte_spec_complete.md`** comme unique référence des règles et des decks.
- **Architecture** : séparation claire moteur / entrées (humain + bot) pour faciliter le port P2P en Phase 2.

Tu peux maintenant implémenter la **Phase 1** (duel vs bot sur Unity, PC/Mobile) en prenant **`Document/carte_spec_complete.md`** comme spec, en **loggant tout** comme décrit dans « Exigence : logging », et en gardant le moteur de jeu prêt pour un futur multijoueur P2P.
