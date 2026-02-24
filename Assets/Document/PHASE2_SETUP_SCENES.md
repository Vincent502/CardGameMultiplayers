# Phase 2 – Mise en place des scènes (Unity)

À faire dans l’éditeur Unity pour que le menu et le flux Solo / Multiplayer fonctionnent.

---

## 1. Scènes existantes

- **Menu** – déjà présente
- **SoloBoard** – déjà présente (partie vs bot)
- **MultiplayeurBoard** – déjà présente (partie P2P, à brancher plus tard)
- **Lobby** – à créer (voir ci‑dessous)

---

## 2. Scène Menu

1. Ouvrir la scène **Menu**.
2. Créer ou vérifier qu’il y a **3 boutons** : **Solo**, **Multiplayer**, **Quitter** (texte au choix).
3. Créer un GameObject vide (ex. `MenuController`) → **Add Component** → **Menu Controller** (script `CardGame.Unity.MenuController`).
4. Dans l’Inspector, brancher :
  - **Button Solo** → le bouton Solo
  - **Button Multiplayer** → le bouton Multiplayer
  - **Button Quit** → le bouton Quitter

---

## 3. Créer la scène Lobby

1. **File → New Scene** (ou dupliquer une scène vide).
2. Sauvegarder sous **Scenes/Lobby.unity**.
3. Ajouter un **Canvas** (UI → Canvas) si besoin.
4. Sous le Canvas :
  - Un **texte** (ex. TMP) : « Lobby – Create/Join puis choose deck » (ou « Phase 2 – bientôt »).
  - **Bouton** « Créer une partie », **InputField** (code), **Bouton** « Rejoindre », **Text** (affichage code / statut), **Bouton** « Retour au menu ».
5. Créer un GameObject vide (ex. `NetworkManager`) → **Add Component** → **Network Manager** (Netcode) + **Unity Transport** en mode **Relay** (voir **PHASE2_ETAPE2_NETCODE_RELAY.md**).
6. Créer un GameObject vide (ex. `LobbyLogic`) → **Add Component** → **Relay Manager** + **Lobby Controller**.
7. Brancher dans **Lobby Controller** : Relay Manager, Button Create, Input Join Code, Button Join, Text Join Code, Button Back To Menu (détails dans **PHASE2_ETAPE2_NETCODE_RELAY.md**).

---

## 4. Build Settings

1. **File → Build Settings**.
2. **Add Open Scenes** (ou glisser les scènes) dans cet ordre :
  - **Menu** (index 0, optionnel pour démarrage)
  - **SoloBoard**
  - **Lobby**
  - **MultiplayeurBoard**
3. Pour démarrer directement sur le menu : cocher **Menu** et faire **Ctrl+Shift+B** ou lancer Play avec la scène Menu ouverte.

---

## 5. Vérification

- Lancer le jeu depuis la scène **Menu**.
- **Solo** → charge **SoloBoard** (partie vs bot).
- **Multiplayer** → charge **Lobby** (pour l’instant écran avec Retour au menu ; le flux Create/Join + deck sera ajouté avec Netcode).
- **Quitter** → quitte le jeu (ou arrête le Play dans l’éditeur).

---

## 6. Scripts créés


| Script              | Scène | Rôle                                                                                                                       |
| ------------------- | ----- | -------------------------------------------------------------------------------------------------------------------------- |
| **MenuController**  | Menu  | Boutons Solo (→ SoloBoard), Multiplayer (→ Lobby), Quitter. Constantes de noms de scènes dans `MenuController.SceneNames`. |
| **LobbyController** | Lobby | Create/Join avec code ami (RelayManager), affichage du code (Host), saisie du code (Join), Retour au menu. Choix deck à l’étape suivante. |
| **RelayManager**    | Lobby | Connexion Relay : StartHostWithRelayAsync (code ami), StartClientWithRelayAsync(code), Shutdown. Voir **PHASE2_ETAPE2_NETCODE_RELAY.md**. |


