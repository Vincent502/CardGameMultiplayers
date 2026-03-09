# 🃏 Card Game – Duel tour par tour

Jeu de cartes **2 joueurs** en tour par tour pour Unity (PC/Mobile). Moteur C# découplé, UI Unity, support **Solo** (vs bot) et **Multijoueur** (P2P via Netcode/Relay).

---

## ✨ Fonctionnalités

### Gameplay

- **2 decks** : Magicien et Guerrier (34 cartes chacun, 4 équipements obligatoires)
- **Passifs** : Magicien = +1 mana par carte Éphémère jouée ; Guerrier = +1 Force et +1 Résistance de base
- **Types de cartes** : Équipé, Normal, Éphémère, Rapide
- **Effets à durée** : Orage de poche, Armure psychique, Lien karmique (compteur par tour)
- **Glace localisée** : gel d’équipement adverse, dégel après 2 tours du joueur, affichage en bleu
- **Cartes Éphémère** : chaque exemplaire jouable une fois par partie
- **Divination** : choix de carte à remettre sur le deck
- **Réactions** : fenêtre de 1 s pour jouer une carte Rapide (Parade, Contre-attaque, etc.)

### Modes de jeu

- **Solo** : vs bot (SimpleBot)
- **Multijoueur** : P2P via Unity Netcode + Relay (code à partager pour rejoindre)

### Profil joueur

- Création de profil, stats par bloc (Global, Solo, Multi, Ghost)
- Succès débloquables
- Persistance : `Rapport/Profile/player_profile.json`

### UI

- **GameUI** : PV, mana, main, Frappe, Fin de tour, équipements, effets à durée
- **Historique** : journal des parties avec pseudos et couleurs (gagnant/perdant)
- **Lobby** : affichage des pseudos pendant le choix des decks
- **Tooltip équipements** : survol (PC) ou clic (mobile)

---

## 🛠 Technologies

- **Unity** (PC / Mobile)
- **C#** – moteur dans `Core/` sans dépendance Unity
- **TextMeshPro** pour tous les textes
- **Unity Netcode** pour le multijoueur P2P
- **Unity Relay** pour la connexion via code (Create/Join)

---

## 📁 Structure du projet

```
Scripts/CardGame/
├── Core/           # Moteur de jeu (état, règles, résolution des effets)
├── Data/           # Définition des decks (Magicien, Guerrier)
├── Bot/            # IA SimpleBot
├── Network/        # Netcode, Lobby, Relay, StartGameParams
├── Unity/          # Controllers, UI, Profil, Historique
├── Editor/         # Outils éditeur (MenuProfilBuilder, CreateProfileSceneBuilder)
└── README.md
```

| Dossier  | Rôle |
|----------|------|
| **Core** | `GameState`, `GameSession`, `EffectResolver`, `PlayerState`, `EquipmentState`, `ActiveDurationEffect` |
| **Data** | `DeckDefinitions`, `CardData`, `CardId` |
| **Bot**  | `SimpleBot` – IA pour Joueur 2 en solo |
| **Network** | `NetworkGameController`, `LobbyNetworkState`, `RelayManager`, `GameNetworkBehaviour` |
| **Unity** | `GameController`, `GameUI`, `ProfileManager`, `HistoryController`, `LobbyController` |

---

## 🚀 Installation et lancement (Unity)

### Solo

1. Ouvrir la scène **Menu** ou **SoloBoard**.
2. Créer un profil si nécessaire (écran de création).
3. Choisir le deck et lancer la partie.
4. Joueur 1 = humain, Joueur 2 = bot. Premier joueur aléatoire.

### Multijoueur

1. Ouvrir la scène **Lobby**.
2. **Créer** : génère un code à partager. **Rejoindre** : saisir le code (6–12 caractères).
3. Choisir son deck (Magicien / Guerrier) et confirmer.
4. Quand les deux ont confirmé, la partie se lance automatiquement.

---

## 🎮 Règles rapides

- **Joueur 1** = Host (index 0), **Joueur 2** = Client (index 1).
- **1 frappe par tour** ; si l’arme est gelée, la frappe n'est pas possible.
- **Équipement gelé** (bleu) : dégel après 2 tours du joueur propriétaire.
- **Éphémère** : chaque exemplaire n’est jouable qu’une fois par partie.
- **Rapide** : jouable pendant la fenêtre de réaction (1 s) après une attaque adverse.

---

## 📋 Logs et rapports

- **GameLogger** : logs en console + fichier dans `persistentDataPath/Rapport/Historique/`
- **GameReportManager** : 10 derniers rapports conservés. Pseudos dans les logs.
- **Historique** : affichage avec pseudos et couleurs (gagnant vert, perdant rouge).

---

*Dernière mise à jour : mars 2025*
