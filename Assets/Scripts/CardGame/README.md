# Jeu de cartes tour par tour - Phase 1 (Duel vs Bot)

Référence : `Document/carte_spec_complete.md` et `Document/PROMPT_jeu_cartes.md`.

## Structure

- **Core/** : Moteur de jeu (sans Unity) — règles, état, résolution des effets, logging.
- **Data/** : Définition des decks Magicien et Guerrier.
- **Bot/** : IA simple (SimpleBot) pour le joueur 2.
- **Unity/** : GameController (pilote de partie), GameLogger, GameUI (interface minimale).

## Lancer une partie dans Unity

### Guide pas à pas (à faire dans l’éditeur Unity)

1. **Ouvrir ta scène** (ex. `SampleScene` ou ta scène de jeu).

2. **Créer le pilote de partie**
   - Clic droit dans la Hierarchy → **Create Empty**.
   - Renommer l’objet (ex. `GameController`).
   - Dans l’Inspector : **Add Component** → chercher **Game Controller** (script `CardGame.Unity.GameController`).

3. **Créer le Canvas UI**
   - Clic droit dans la Hierarchy → **UI** → **Canvas** (Unity crée un Canvas + EventSystem si besoin).
   - Sélectionner le **Canvas**.

4. **Sous le Canvas, créer les 3 textes TMP**
   - Clic droit sur Canvas → **UI** → **Text - TextMeshPro** (si demandé, importer les ressources TMP).
   - Créer 3 fois, les renommer par ex. : `TextStatus`, `TextPlayer0`, `TextPlayer1`.
   - Les placer où tu veux (en haut pour le statut, à gauche/droite pour les joueurs).

5. **Toujours sous le Canvas, créer le conteneur des cartes**
   - Clic droit sur Canvas → **Create Empty** → renommer `HandContainer`.
   - (Optionnel) Sur `HandContainer` : Add Component → **Horizontal Layout Group** pour aligner les boutons de cartes.

6. **Sous le Canvas, créer les 2 boutons**
   - Clic droit sur Canvas → **UI** → **Button - TextMeshPro** (ou Button puis remplacer le texte par du TMP).
   - Créer 2 boutons, les renommer `ButtonStrike` et `ButtonEndTurn`.
   - Changer le texte : « Frappe » et « Fin de tour ».

7. **Brancher GameUI**
   - Clic droit sur le **Canvas** (ou un enfant vide) → **Create Empty** → renommer `GameUI`.
   - Sur `GameUI` : **Add Component** → **Game UI** (script `CardGame.Unity.GameUI`).
   - Dans l’Inspector du script **Game UI** :
     - **Controller** : glisser l’objet `GameController` de la Hierarchy.
     - **_text Status** : glisser `TextStatus`.
     - **_text Player0** : glisser `TextPlayer0`.
     - **_text Player1** : glisser `TextPlayer1`.
     - **Hand Container** : glisser `HandContainer`.
     - **Button Strike** : glisser le bouton Frappe.
     - **Button End Turn** : glisser le bouton Fin de tour.
     - **Card Button Prefab** : laisser vide (des boutons seront créés automatiquement pour les cartes).

8. **Lancer**
   - **Play**. La partie démarre : toi = Joueur 0 (Magicien), le bot = Joueur 1 (Guerrier). Premier joueur = aléatoire.
   - À ton tour : clique sur une carte pour la jouer, ou sur « Frappe », ou « Fin de tour ».

### Résumé des scripts

| Objet        | Script à ajouter   | Rôle                          |
|-------------|--------------------|--------------------------------|
| GameController | GameController  | Lance la partie, fait jouer le bot. |
| GameUI (sous Canvas) | GameUI       | Affiche PV/mana/main et envoie tes actions. |

Les champs **Controller**, **_text Status**, **_text Player0**, **_text Player1**, **Hand Container**, **Button Strike**, **Button End Turn** doivent être assignés dans l’Inspector de GameUI, sinon l’affichage ou les boutons ne marcheront pas.

## Logging

Toutes les actions et changements d’état sont logués via **GameLogger** (Console Unity + fichier dans `Application.persistentDataPath` si activé).

## Évolution

- Phase 2 : brancher une couche P2P à la place du bot (même moteur, même `SubmitAction`).
