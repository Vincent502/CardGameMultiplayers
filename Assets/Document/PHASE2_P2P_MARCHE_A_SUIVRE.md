# Phase 2 – Marche à suivre : intégration P2P

Ce document décrit **comment procéder** pour intégrer le jeu en réseau P2P (2 joueurs humains) en s’appuyant sur le moteur Phase 1 existant.

---

## 0. Flux global retenu (menu + lobby)

### Scène Menu (nouvelle)

- **Solo** → charge la scène de jeu Phase 1 (duel vs bot). Moteur et UI actuels inchangés.
- **Multiplayer (P2P)** → connexion par **code ami** : soit « Créer une partie » (j’obtiens un code à partager), soit « Rejoindre » (j’entre le code de l’autre).
- **Quitter le jeu** → fermeture de l’application.

### Multiplayer : de la connexion au lancement

1. **Connexion** : un joueur crée la partie (Host) et reçoit un **code ami** (ex. 6 caractères). L’autre saisit ce code et rejoint. Les deux sont alors connectés via un **relay** (traverse les NAT, pas besoin d’IP directe).
2. **Lobby** : une fois connectés, les deux voient l’écran lobby.
   - Chaque joueur **choisit son deck** : Magicien ou Guerrier.
   - Chacun a un bouton **Confirmer** (ou équivalent). Quand **les deux ont confirmé**, la partie se lance.
3. **Lancement** : les deux reçoivent les mêmes paramètres (deck Joueur 1, deck Joueur 2, premier joueur, graine). Les deux appellent `StartGame(...)` en local et chargent la scène de jeu. Le jeu tourne en **lockstep** (échange des actions uniquement).

**Intérêt de cette approche** : une seule scène menu pour tout le jeu, expérience claire (Solo vs Multiplayer), et le lobby évite de lancer la partie avant que les deux aient choisi leur deck.

---

## 1. Objectifs Phase 2

- **Deux joueurs humains** en réseau (pas de bot).
- **Même moteur** : `GameSession`, `SubmitAction`, `GameState` restent la référence.
- **Synchronisation** : les deux clients partagent le même déroulement de partie (mêmes actions, même état dérivé).
- **Pas d’autorité serveur dédiée** : on vise du P2P (ou un hébergeur léger), pas un serveur de jeu central.

---

## 2. Architecture recommandée : **Lockstep (actions synchronisées)**

Le moteur est **déterministe** : mêmes entrées → même état. On peut donc :

- **Ne pas** envoyer tout l’état à chaque frame.
- **Envoyer uniquement les actions** (`GameAction`) et les paramètres de démarrage (decks, premier joueur, graine aléatoire).
- **Chaque client** possède sa propre `GameSession` et applique les **mêmes actions dans le même ordre** → les deux restent synchronisés.

**Avantages** : peu de bande passante, pas de “maître” unique, réconciliation simple (réappliquer les actions en ordre).

**À garantir** :
- Aléatoire déterministe : **graine partagée** (ou premier joueur + graine envoyés au démarrage).
- Ordre des actions **identique** chez les deux (séquence numérotée ou ordre d’arrivée convenu).

---

## 3. Stack technique retenue : simple et efficace

### 3.1 Connexion P2P : **Unity Netcode for GameObjects** + **Unity Relay**

Pour le **code ami** et la connexion derrière NAT sans serveur de jeu dédié :

- **Unity Netcode for GameObjects (NGO)** : package officiel Unity pour le multijoueur. Gère la connexion, l’envoi de messages RPC ou custom, et s’intègre bien avec les services Unity.
- **Unity Relay** (Unity Gaming Services) : les deux clients se connectent à un **relay** (serveur relais). Un joueur « crée une allocation » et obtient un **code de join** (ex. 6 caractères) ; l’autre rejoint avec ce code. Pas besoin d’ouvrir de port, ça fonctionne derrière NAT. Gratuit dans les limites du quota.

**Alternative si on veut éviter le cloud Unity** : connexion **Host/Client directe** (IP + port) avec Netcode ; le « code ami » deviendrait alors un code court qui encode l’IP ou un identifiant (nécessite un petit serveur de rendez-vous ou partage d’IP manuel). Plus fragile derrière NAT. Pour la Phase 2, **Relay + Netcode** est le plus simple et le plus fiable.

### 3.2 Rôle Host / Client

- **Host** : celui qui « crée la partie » (génère le code). Il est Joueur 1 côté réseau ; on peut décider que le Host est toujours Joueur 1 en local et le Client toujours Joueur 2, ou attribuer selon le premier joueur de la partie (aléatoire avec graine).
- **Client** : celui qui « rejoint avec le code ». Joueur 2 côté réseau.
- Les deux ont une `GameSession` locale ; le Host envoie les paramètres de `StartGame` (firstPlayerIndex, deckJoueur1, deckJoueur2, **seed**) après confirmation du lobby ; les deux appliquent les mêmes paramètres.

---

## 4. Étapes d’intégration (ordre proposé)

### Étape 0 – Scènes de jeu : Solo vs Multiplayer (deux scènes distinctes)

- **Ne pas mélanger** Solo et P2P dans une seule scène. Garder deux scènes de jeu séparées pour plus de clarté :
  - **Scène Solo** (actuelle) : celle utilisée en Phase 1, duel vs bot. Elle reste inchangée : `GameController`, `GameUI`, `SimpleBot`. Aucune logique réseau.
  - **Scène Multiplayer** : **dupliquer** la scène Solo (même disposition Canvas, joueurs, main, Frappe, Fin de tour). Sur cette copie, on remplace (ou on ajoute) un **controller réseau** (ex. `NetworkGameController`) qui pilote la partie en P2P au lieu du bot. La même `GameUI` peut être réutilisée (elle lit le `GameState`), seuls le pilote et la source des actions changent.
- **Intérêt** : séparation nette des modes, pas de risque de casser le Solo en modifiant pour le P2P, maintenance plus simple.

### Étape 0a – Scène Menu

- Créer une **scène Menu** avec trois options : **Solo**, **Multiplayer**, **Quitter**.
- **Solo** : charge la **scène de jeu Solo** (Phase 1, duel vs bot).
- **Multiplayer** : affiche un écran « Créer une partie » (affiche le code ami après création) ou « Rejoindre » (champ pour saisir le code + bouton Rejoindre). Une fois connecté, transition vers le Lobby ; après confirmation des deux, charge la **scène de jeu Multiplayer** (P2P).
- **Quitter** : `Application.Quit()` (ou équivalent selon plateforme).

### Étape 0b – Lobby (après connexion P2P) **(fait)**

- Écran **Lobby** : chaque joueur choisit son deck (Magicien / Guerrier) et confirme.
- Synchroniser les choix (Host envoie le sien, Client envoie le sien ; les deux affichent « Joueur 1 : Magicien, Joueur 2 : Guerrier » une fois les deux reçus).
- Quand **les deux ont confirmé** : le Host tire le premier joueur et la graine, envoie `StartGameParams` (firstPlayerIndex, deckJoueur1, deckJoueur2, seed), puis les deux chargent la scène de jeu et appellent `StartGame(...)` avec ces paramètres. Voir **PHASE2_ETAPE3_LOBBY_ET_JEU.md**.

### Étape 1 – Préparer le moteur pour le déterministe **(fait)**

- Introduire une **graine (seed)** utilisée pour tout tirage aléatoire (mélange des decks, premier joueur si on le tire côté moteur).
- Remplacer les `Random` / `_rng` par un générateur **initialisé avec cette graine** (ex. `System.Random(seed)` ou équivalent déterministe).
- S’assurer que `StartGame(firstPlayerIndex, deckJoueur1, deckJoueur2)` + graine produit le **même état initial** sur les deux clients (deck mélangé identique, etc.).

### Étape 2 – Couche réseau (Netcode + Relay)

- Installer **Netcode for GameObjects** et configurer **Unity Relay** (projet Unity Gaming Services, clés dans Unity).
- Mettre en place **création de partie** : Host crée une allocation Relay, récupère un **code de join** (court, affiché à l’écran). **Rejoindre** : Client saisit le code, rejoint l’allocation Relay.
- Une fois connectés, les deux ont un `NetworkManager` (ou équivalent) et peuvent s’envoyer des messages (RPC ou `NetworkVariable` / messages custom pour le lobby et les `GameAction`).

### Étape 3 – Protocole de démarrage de partie **(fait)**

- Définir les **messages de démarrage** :
  - Host → Client : `StartGameParams` (firstPlayerIndex, deckJoueur1, deckJoueur2, seed).
  - Les deux appellent localement `GameSession.StartGame(...)` avec les **mêmes** paramètres et la **même** graine.
- Chaque client crée sa `GameSession` et sa `GameState` ; après cette étape, les deux ont le **même état initial**. Voir **PHASE2_ETAPE3_LOBBY_ET_JEU.md**.

### Étape 4 – Envoi et réception des actions **(fait)**

- **Sérialiser** les `GameAction` (PlayCard, Strike, EndTurn, etc.) en messages (NetworkActionMessage + INetworkSerializable).
- Le joueur qui joue envoie son action (Host → ClientRpc, Client → ServerRpc) ; l’autre la reçoit et l’applique sur sa `GameSession` avec `SubmitAction`.

### Étape 5 – Boucle de jeu Unity côté P2P **(fait)**

- **NetworkGameController** : même moteur que GameController, pas de bot ; quand c’est mon tour j’envoie l’action + application locale, quand c’est son tour j’attends le RPC puis j’applique.
- **GameUI** utilise **IGameController** (GameController en Solo, NetworkGameController en P2P). Voir **PHASE2_ETAPE3_LOBBY_ET_JEU.md**.

### Étape 6 – Gestion des erreurs et déconnexions

- Détecter la **déconnexion** (perte de lien réseau).
- Afficher un message (“Adversaire déconnecté”) et proposer de quitter ou de sauvegarder (optionnel).
- (Optionnel) Reprise de partie : sauvegarder la séquence d’actions + état initial ; plus tard, permettre de “rejoindre” avec rejeu des actions.

### Récapitulatif ordre de réalisation

1. **Menu** (Solo / Multiplayer / Quitter) + chargement scène jeu en Solo.  
2. **Moteur déterministe** (graine partagée). **(fait)**  
3. **Netcode + Relay** : connexion par code ami, puis **Lobby** (choix deck, confirmation).  
4. **Protocole StartGame** + chargement scène jeu en Multiplayer. **(fait)**  
5. **Envoi / réception des GameAction** et boucle de jeu P2P. **(fait)**  
6. **Déconnexion** et retour menu si besoin.

---

## 5. Résumé des messages à définir

| Message | Sens | Contenu type |
|--------|------|-----------------------------|
| **CreateGame** / **JoinGame** | Création / join | (Optionnel) deck choisi, pseudo |
| **StartGame** | Démarrage partie (Host → Client ou accord) | firstPlayerIndex, deckJoueur1, deckJoueur2, **seed** |
| **GameAction** | Une action de jeu | Type (PlayCard / Strike / EndTurn) + paramètres (handIndex, etc.) |
| **Ping / Pong** | (Optionnel) Latence | - |
| **Disconnect** | Fin de connexion | - |

---

## 6. Points d’attention

- **Aléatoire** : tout doit reposer sur la **graine** partagée (mélanges, tirage premier joueur si fait dans le moteur). Pas de `Random.Range` ou `Random()` sans graine côté jeu.
- **Ordre des actions** : si les deux envoient (ex. réactions Rapides), définir un ordre (ex. numéro de tour + ordre d’émission) pour que les deux appliquent dans le même ordre.
- **Validation** : chaque client peut appeler `SubmitAction` ; si l’action est invalide, ne pas l’appliquer et éventuellement signaler une erreur (désync ou triche). En lockstep honnête, les deux ne devraient jamais envoyer d’action invalide.
- **UI** : garder l’affichage “Joueur 1” / “Joueur 2” ; côté local, “moi” = Joueur 1 ou 2 selon qui a créé ou rejoint (à définir dans les paramètres de connexion).

---

## 7. Prochaine étape concrète

Commencer par **l’étape 1** : introduire une **graine** dans le moteur et rendre **déterministes** les mélanges et tirages (premier joueur inclus si on le tire dans le Core). Une fois cela fait, le reste (transport, messages, boucle P2P) pourra s’appuyer sur un état reproductible des deux côtés.
