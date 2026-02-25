# 🃏 Card Game – Duel tour par tour

Jeu de cartes **2 joueurs** en tour par tour développé avec **Unity**. Mode **Solo** (contre IA) et mode **Multiplayer P2P** (2 joueurs en réseau via code ami).

---

## 📖 Présentation

Un duel de cartes stratégique où deux joueurs s'affrontent avec des decks **Magicien** ou **Guerrier**. Chaque deck compte 34 cartes (dont 4 équipements obligatoires) avec des mécaniques distinctes : dégâts, bouclier, Force, Résistance, mana, frappe, et cartes Rapides pour réagir aux attaques adverses.

### Modes de jeu

| Mode | Description |
|------|-------------|
| **Solo** | Duel contre une IA. Choix de ton deck (Magicien/Guerrier), l'IA reçoit un deck aléatoire. |
| **Multiplayer** | Partie en ligne P2P. Un joueur crée une partie et obtient un code à partager ; l'autre rejoint avec ce code. Chaque joueur choisit son deck dans le lobby. |

---

## ✨ Fonctionnalités principales

- **2 decks** : Magicien (magie, bouclier, contrôle) et Guerrier (force, résistance, cartes Rapides)
- **Règles complètes** : PV (100), bouclier, Force, Résistance, mana, pioche progressive (3→5 cartes), frappe (1/tour)
- **Types de cartes** : Équipé, Normal, Éphémère, Rapide
- **Cartes Rapides** : Parade et Contre-attaque — jouables en réaction aux attaques (fenêtre de 3 secondes)
- **Effets à durée** : Orage de poche, Armure psychique, Lien karmique
- **Glace localisée** : gel d'un équipement adverse, dégel par carte dégâts ou frappe
- **IA** : bot simple pour le mode Solo
- **P2P** : Unity Netcode for GameObjects + Unity Relay (code ami, connexion derrière NAT)

---

## 🛠 Technologies

- **Unity** (PC / Mobile / Android)
- **C#** — moteur de jeu dans `Core/` sans dépendance Unity
- **Unity Netcode for GameObjects** — multijoueur
- **Unity Relay** — connexion P2P par code ami
- **TextMeshPro** — interface utilisateur

---

## 📁 Structure du projet

```
CardeGameProject/
├── Assets/
│   ├── Scenes/
│   │   ├── Menu.unity          # Menu principal (Solo / Multiplayer / Quitter)
│   │   ├── SoloBoard.unity     # Partie Solo vs IA
│   │   ├── Lobby.unity         # Lobby P2P (choix deck, code ami)
│   │   └── MultiplayeurBoard.unity  # Partie P2P
│   └── Scripts/CardGame/
│       ├── Core/               # Moteur de jeu (état, règles, effets)
│       ├── Data/               # Définition des decks
│       ├── Bot/                # IA SimpleBot
│       ├── Unity/              # Contrôleurs, UI, Menu, Lobby
│       ├── Network/            # P2P, Relay, messages réseau
│       └── README.md           # Documentation détaillée du moteur
└── README.md
```

| Dossier | Rôle |
|---------|------|
| **Core** | `GameSession`, `GameState`, `EffectResolver`, `PlayerState`, actions et résolution des effets |
| **Data** | `DeckDefinitions`, `CardData`, `CardId` |
| **Bot** | `SimpleBot` — IA pour le Joueur 2 en Solo |
| **Unity** | `GameController`, `NetworkGameController`, `GameUI`, `MenuController`, `LobbyController` |
| **Network** | `GameNetworkBehaviour`, `NetworkActionMessage`, `LobbyNetworkState`, `RelayManager` |

---

## 🚀 Installation et lancement

1. **Ouvrir le projet** dans Unity (version compatible avec Netcode for GameObjects).
2. **Configurer Unity Relay** : créer un projet Unity Gaming Services et activer Relay (voir [Unity Relay](https://docs.unity.com/relay/)).
3. **Lancer** : ouvrir la scène `Menu` et cliquer sur **Play**.

### Scènes

- **Menu** : Solo (choix deck) ou Multiplayer (Create/Join)
- **SoloBoard** : partie contre l'IA
- **Lobby** : connexion P2P, choix des decks, confirmation
- **MultiplayeurBoard** : partie en ligne 2 joueurs

---

## 🎮 Règles rapides

- **Joueur 1** = toi (humain), **Joueur 2** = adversaire (IA ou humain)
- **1 frappe par tour** ; arme gelée → frappe pour briser le gel (sans dégâts)
- **Cartes Rapides** : 3 secondes pour réagir à une attaque (Parade = annuler, Contre-attaque = annuler + 2 dégâts)
- **Passif Guerrier** : +1 Force et +1 Résistance de base
- **Victoire** : réduire les PV adverses à 0

---

## 📜 Licence

Projet personnel / éducatif. Spécifications et règles détaillées dans `Assets/Document/` (hors dépôt Git).
