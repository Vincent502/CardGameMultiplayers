# Phase 2 – Étape 2 : Couche réseau (Netcode + Relay)

Ce document décrit la mise en place de la connexion P2P par **code ami** avec **Netcode for GameObjects** et **Unity Relay**.

---

## 1. Packages (déjà ajoutés)

Dans `Packages/manifest.json` ont été ajoutés :

- **com.unity.netcode.gameobjects** (2.2.0) – multijoueur NGO
- **com.unity.services.multiplayer** (2.1.1) – Relay (allocations, code de join)

Après ouverture du projet dans Unity, les packages seront résolus. Si une version n’est pas trouvée, ajuster dans le Package Manager.

---

## 2. Configuration Unity (à faire dans l’éditeur)

### 2.1 Lier le projet aux Unity Gaming Services (Relay)

1. **Edit → Project Settings → Services** (ou fenêtre **Services**).
2. Créer ou lier un **projet Unity** à un **projet Cloud**.
3. Activer **Relay** (ou Multiplayer) pour le projet si demandé.
4. Sans projet Cloud lié, les appels Relay échoueront à l’exécution.

### 2.2 Scène Lobby : NetworkManager + Relay

1. Ouvrir la scène **Lobby**.
2. Créer un GameObject vide (ex. `NetworkManager`).
3. **Add Component** → **Network Manager** (Netcode for GameObjects).
4. Sur le même GameObject, vérifier qu’un **Transport** est présent (ex. **Unity Transport**).
5. Dans le composant **Unity Transport** :
   - **Protocol** : **Relay Unity Transport** (ou équivalent Relay).
6. (Optionnel) Désactiver **Auto-Start** sur le NetworkManager si tu démarres toi-même via Relay (Create/Join).

### 2.3 Scène Lobby : RelayManager et LobbyController

1. Créer un GameObject vide (ex. `LobbyLogic`).
2. **Add Component** → **Relay Manager** (`CardGame.Unity.RelayManager`).
3. **Add Component** → **Lobby Controller** (`CardGame.Unity.LobbyController`).
4. Dans l’Inspector du **Lobby Controller** :
   - **Relay Manager** → glisser le même GameObject (ou celui qui a `RelayManager`).
   - **Button Create** → bouton « Créer une partie ».
   - **Create Busy Indicator** → (optionnel) objet affiché pendant la création.
   - **Text Join Code** → Text UI où afficher le code ami (Host) ou les messages (Join).
   - **Input Join Code** → champ de saisie pour le code (Join).
   - **Button Join** → bouton « Rejoindre ».
   - **Join Busy Indicator** → (optionnel) objet affiché pendant la tentative de join.
   - **Button Back To Menu** → bouton « Retour au menu ».

### 2.4 UI Lobby minimale

Sous le Canvas (scène Lobby) :

- **Bouton** « Créer une partie » → brancher sur **Lobby Controller → Button Create**.
- **Text** (ex. UI Text) pour le code / statut → **Text Join Code**.
- **InputField** pour saisir le code → **Input Join Code**.
- **Bouton** « Rejoindre » → **Button Join**.
- **Bouton** « Retour au menu » → **Button Back To Menu**.

---

## 3. Comportement attendu

- **Créer une partie** : initialise Unity Services + Auth, crée une allocation Relay, démarre le Host, affiche le **code ami** dans **Text Join Code**. Un second joueur peut rejoindre avec ce code.
- **Rejoindre** : saisir le code dans **Input Join Code**, cliquer **Rejoindre** → connexion au Relay et démarrage Client. Un message de type « Connecté » peut s’afficher dans **Text Join Code**.
- **Retour au menu** : appelle `RelayManager.Shutdown()` puis charge la scène Menu.

---

## 4. Scripts créés / modifiés

| Script             | Rôle |
|--------------------|------|
| **RelayManager**   | `InitializeAsync()`, `StartHostWithRelayAsync()` (retourne le code ami), `StartClientWithRelayAsync(joinCode)`, `Shutdown()`. Utilise Unity Transport en Relay. |
| **LobbyController** | UI Create/Join, affichage du code, appel à `RelayManager`, Retour au menu. |

---

## 5. Suite (Étape 3)

Une fois deux clients connectés sur la scène Lobby, la prochaine étape est le **protocole de démarrage de partie** : choix deck (Magicien / Guerrier), synchronisation des choix, puis envoi de **StartGameParams** (firstPlayerIndex, deckJoueur1, deckJoueur2, seed) et chargement de la scène **MultiplayeurBoard** avec `GameSession.StartGame(...)`.
