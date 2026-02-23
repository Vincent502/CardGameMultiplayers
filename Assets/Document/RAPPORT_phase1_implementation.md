# Rapport d'implémentation – Phase 1 (Duel vs Bot)

**Projet** : Jeu de cartes tour par tour  
**Référence** : `carte_spec_complete.md`, `PROMPT_jeu_cartes.md`  
**Périmètre** : Moteur de jeu, decks Magicien / Guerrier, interface Unity, bot, règles complètes.

---

## 1. Vue d'ensemble

- **Moteur** : C# sans dépendance Unity (Core), prêt pour une future couche P2P.
- **Plateforme** : Unity (PC/Mobile), interface minimale avec TextMeshPro.
- **Partie** : 2 joueurs (Joueur 1 = humain en solo, Joueur 2 = bot ou futur adversaire P2P), 34 cartes par deck, 4 équipements obligatoires par deck.

---

## 2. Moteur (Core)

### 2.1 État et modèles

- **GameState** : 2 joueurs, tour actuel, phase, premier joueur, effets à durée, vainqueur. Constantes `Player1Index` (0) et `Player2Index` (1) pour lisibilité. Propriété `WinnerDisplayNumber` (1 ou 2) pour l’affichage.
- **PlayerState** : PV (100), Bouclier, Force, Résistance, Mana, main, deck, cimetière, retirées du jeu, équipements, flags (attaque ce tour, frappes consécutives, bonus Force/Résistance temporaires, bonus dégâts arme ce tour, etc.).
- **EquipmentState** : carte, rounds avant activation, `IsFrozen` (Glace localisée).
- **ActiveDurationEffect** : effets « durant X tours » (dégâts chaque tour, bouclier temporaire, résistance temporaire) avec `CardId`, `Kind`, `CasterPlayerIndex`, `TargetPlayerIndex`, `TurnsRemaining`, `Value`.
- **GameSession** : démarrage de partie (`StartGame` avec humain = Joueur 1, decks Joueur 1/2), boucle de phases (StartTurn → ResolveStartOfTurn → Draw → Play → ResolveEndOfTurn → EndTurn), validation et application des actions (`SubmitAction` : PlayCard, Strike, EndTurn).
- **EffectResolver** : résolution des effets par carte (dégâts, bouclier, pioche, équipements à la frappe, dégel, etc.), formules dégâts = base × (1+Force), bouclier = base × (1+Résistance), gestion fin de tour (dégâts récurrents, expiration des buffs).

### 2.2 Règles implémentées

- **Tour** : défausse main, bouclier à 0, résolution début de tour, pioche (3/4/5 cartes, plafond 5), mana (1/2/3, plafond 3), phase Play, résolution fin de tour, fin de tour.
- **Frappe** : 1 frappe max par tour. Dégâts = base arme × (1+Force). Si seule arme gelée, la frappe peut servir à « briser le gel » (dégel sans dégâts).
- **Équipements** :
  - **À la frappe** : Catalyseur arcanaique (1 bouclier), Rune de force arcanique (+2 dégâts), Rune d’agressivité (+1 Force jusqu’à fin du tour), Rune de protection (2 bouclier si 2 frappes enchaînées — actuellement 1 frappe/tour donc non déclenchée).
  - **Début de tour** : Rune d’énergie arcanique (pioche +2), Rune d’endurance (＋3 PV).
  - **Fin de tour** : Rune d’essence arcanique (5 bouclier si Résistance = 0). Retrait des bonus Force temporaires (Galvanisation, Concentration, Rune agressivité).
- **Effets à durée** : Orage de poche (1 dégât/tour, 3 tours), Armure psychique (23 bouclier, retrait après 2 tours), Lien karmique (+3 Résistance, retrait après 3 tours).
- **Glace localisée** : gèle le premier équipement actif adverse. Dégel uniquement si le propriétaire joue une **carte qui fait des dégâts** (Attaque, Boule de feu, etc.) ou utilise la **Frappe** pour « briser le gel » (0 dégât). Pas de dégel en fin de tour. Affichage : équipement gelé en **bleu** dans l’UI.
- **Cartes Éphémère** : chaque **exemplaire** (instance) est jouable une fois ; les autres exemplaires du même type restent disponibles (pas de blocage par `CardId`).

### 2.3 Cartes et decks

- **Decks** : Magicien et Guerrier définis dans `DeckDefinitions.cs` (34 cartes chacun, 4 équipements obligatoires). Choix par joueur dans l’Inspector (`DeckKind`).
- **Types** : Equipe, Normal, Ephemere, Rapide. Coût équipement = nombre de tours avant activation.
- **Cartes avec effet** : Attaque, Attaque+, Boule de feu, Défense, Défense+, Galvanisation, Concentration, Lien karmique, Armure psychique, Orage de poche, Glace localisée, Appuis solide, Explosion magie éphémère, Divination, etc. (voir `EffectResolver` et spec).

---

## 3. Interface et présentation

### 3.1 Conventions d’affichage

- **Joueur 1** : index 0, affiché « Joueur 1 - humain » (toujours humain en solo).
- **Joueur 2** : index 1, affiché « Joueur 2 » (bot ou futur P2P).
- **Fin de partie** : « Gagnant : Joueur 1 » ou « Joueur 2 » (`WinnerDisplayNumber`).
- **Variables** : constantes `GameState.Player1Index` / `Player2Index`, champs UI renommés (ex. `_textJoueur1`, `_textJoueur2`, `_equipmentsJoueur1Container`, etc.) avec `FormerlySerializedAs` pour compatibilité des scènes.

### 3.2 Unity (GameController, GameUI, GameLogger)

- **GameController** : crée la session, lance la boucle de jeu, délègue au bot quand ce n’est pas le tour du joueur. Paramètres : humain = Joueur 1, decks Joueur 1/2, logs fichier.
- **GameUI** : TextMeshPro pour statut, tour, PV/bouclier/Force/Résistance/mana/main pour chaque joueur, boutons cartes (main), Frappe, Fin de tour, listes équipements et effets à durée par joueur. Bouton Frappe actif seulement si `CanStrike` (arme active ou arme gelée pour briser le gel).
- **GameLogger** : logs Console + fichier (optionnel) dans `Application.persistentDataPath`.

---

## 4. Bot

- **SimpleBot** : choisit une action (jouer une carte jouable au hasard, frapper si possible, fin de tour). Respecte 1 frappe par tour et coût mana / type de carte.

---

## 5. Fichiers principaux

| Dossier / Fichier | Rôle |
|-------------------|------|
| `Core/GameState.cs` | État global, constantes joueur, `WinnerDisplayNumber`. |
| `Core/GameSession.cs` | Phases, `StartGame`, `SubmitAction`, `CanStrike`, fin de tour (bonus Force, pas de dégel auto). |
| `Core/EffectResolver.cs` | Effets cartes, dégâts/bouclier, frappe, équipements à la frappe, dégel (carte dégâts + frappe briser le gel), `UnfreezeOneEquipmentIfAny`, effets à durée. |
| `Core/ActiveDurationEffect.cs` | Modèle des effets à durée. |
| `Core/PlayerState.cs` | État joueur, `WeaponDamageBonusThisTurn`, bonus Force/Résistance. |
| `Core/EquipmentState.cs` | Équipement, `IsFrozen`, `IsActive`. |
| `Data/DeckDefinitions.cs` | Définition des decks Magicien et Guerrier. |
| `Unity/GameController.cs` | Pilote de partie, `CanStrike`, paramètres Joueur 1/2. |
| `Unity/GameUI.cs` | Affichage Joueur 1/2, équipements (vert actif, bleu gelé, gris inactif), effets à durée. |
| `Bot/SimpleBot.cs` | IA joueur 2. |

---

## 6. Récapitulatif des comportements clés

- **Frappe** : 1 par tour ; dégâts si arme active, sinon possibilité de « briser le gel » (dégel sans dégât).
- **Équipements gelés** : affichés en bleu ; dégel par carte dégâts (joueur qui a l’équipement joue la carte) ou par frappe briser le gel ; pas de dégel en fin de tour.
- **Éphémère** : chaque exemplaire jouable une fois, les autres exemplaires du même type restent jouables.
- **Effets à durée** : correctement créés et expirés (bouclier/résistance retirés à l’expiration).

---

## 7. Évolutions prévues

- Phase 2 : couche P2P (remplacer le bot, même moteur et `SubmitAction`).
- Cartes Rapides : réaction pendant le tour adverse (timing, mana réservé).
- Choix de cible pour Glace localisée (quel équipement adverse geler) si besoin.
