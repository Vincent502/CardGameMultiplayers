# Phase 2 – Étapes 3 à 5 : Lobby (deck) + démarrage partie + boucle P2P

Ce document décrit la suite après la connexion Relay : **choix deck dans le Lobby**, **StartGameParams**, et **boucle de jeu P2P** sur la scène MultiplayeurBoard.

---

## 1. Résumé

- **Lobby** : après connexion (Create/Join), chaque joueur choisit son deck (Magicien / Guerrier) et confirme. Quand les deux ont confirmé, le Host génère premier joueur + graine, envoie **StartGameParams**, et les deux chargent **MultiplayeurBoard**.
- **MultiplayeurBoard** : les deux clients ont la même **GameSession** (même seed + paramètres). Chaque action (PlayCard, Strike, EndTurn) est appliquée localement puis envoyée à l’autre (Host → ClientRpc, Client → ServerRpc).
- **GameUI** : réutilisée telle quelle ; elle utilise **IGameController** (GameController en Solo, NetworkGameController en P2P).

---

## 2. Prefabs à créer dans Unity

### 2.1 LobbyState (pour le Lobby)

1. Créer un **GameObject vide** (ex. `LobbyStatePrefab`).
2. **Add Component** → **Network Object** (Netcode).
3. **Add Component** → **Lobby Network State** (`CardGame.Unity.LobbyNetworkState`).
4. Sauvegarder en **Prefab** (glisser dans un dossier Prefabs), puis le retirer de la scène si nécessaire.
5. Dans la scène **Lobby**, dans **Lobby Controller** (Inspector), assigner **Lobby State Prefab** → ce prefab.

### 2.2 GameNetwork (pour MultiplayeurBoard)

1. Créer un **GameObject vide** (ex. `GameNetworkPrefab`).
2. **Add Component** → **Network Object** (Netcode).
3. **Add Component** → **Game Network Behaviour** (`CardGame.Unity.GameNetworkBehaviour`).
4. Sauvegarder en **Prefab**.
5. Dans la scène **MultiplayeurBoard**, sur l’objet qui a **Network Game Controller**, assigner **Game Network Prefab** → ce prefab.

---

## 3. Scène Lobby – UI choix deck

Sous le Canvas (scène Lobby), en plus de Create/Join/Retour :

- **Panel** (ex. `PanelDeckSelection`) : désactivé par défaut. Il s’affiche quand on est connecté (Host ou Client).
  - À l’intérieur : **Bouton** « Magicien », **Bouton** « Guerrier », **Bouton** « Confirmer », **Text** (statut : Joueur 1 : ? | Joueur 2 : ?).
- Dans **Lobby Controller** (Inspector) :
  - **Panel Deck Selection** → ce panel.
  - **Button Deck Magicien** / **Button Deck Guerrier** / **Button Confirm Deck** / **Text Deck Status** → les éléments ci‑dessus.
  - **Lobby State Prefab** → le prefab LobbyState.

Comportement : Host = Joueur 1 (choix deck 1), Client = Joueur 2 (choix deck 2). Quand les deux ont confirmé, le Host envoie StartGameParams et tout le monde charge **MultiplayeurBoard**.

---

## 4. Scène MultiplayeurBoard

1. Ouvrir la scène **MultiplayeurBoard** (copie de la scène Solo sans bot).
2. Remplacer (ou ajouter) un GameObject avec **Network Game Controller** (`CardGame.Unity.NetworkGameController`).
3. Assigner **Game Network Prefab** → le prefab GameNetwork.
4. **GameUI** : dans **Controller Mono**, assigner le même GameObject que **Network Game Controller** (ou laisser vide pour que le script trouve automatiquement **NetworkGameController**).
5. La scène doit contenir un **NetworkManager** (soit DontDestroyOnLoad depuis le Menu/Lobby, soit une copie dans la scène avec les mêmes réglages Relay).

---

## 5. Scripts créés (référence)

| Fichier | Rôle |
|--------|------|
| **StartGameParams** (+ NetworkGameParamsHolder) | Struct INetworkSerializable (firstPlayer, decks, seed). Holder static lu par NetworkGameController au chargement. |
| **LobbyNetworkState** | NetworkBehaviour : NetworkVariables pour choix deck Host/Client, ClientRpc LaunchGame(params) → charge MultiplayeurBoard. |
| **NetworkActionMessage** | Sérialisation réseau des GameAction (PlayCard, Strike, EndTurn). |
| **GameNetworkBehaviour** | NetworkBehaviour : SendActionToOtherClient (ClientRpc), ReceiveFromClientServerRpc. |
| **NetworkGameController** | IGameController : StartGame avec params du Holder, boucle Step(), envoi/réception des actions. |
| **IGameController** | Interface pour GameUI (GameController + NetworkGameController). |

---

## 6. Vérification

1. Menu → Multiplayer → Lobby.
2. Host : Créer une partie → code affiché. Client : Rejoindre avec le code.
3. Les deux voient le panel deck. Host choisit Magicien ou Guerrier (Joueur 1), Client idem (Joueur 2), chacun clique Confirmer.
4. Quand les deux ont confirmé → chargement automatique de **MultiplayeurBoard**, partie démarre avec les bons decks et la même graine.
5. Tour par tour : chaque joueur joue de son côté ; les actions sont synchronisées (lockstep).

---

## 7. Suite (Étape 6)

Gestion des **déconnexions** : détecter la perte de lien, afficher « Adversaire déconnecté », proposer de revenir au menu.
