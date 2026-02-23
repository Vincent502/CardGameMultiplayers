# 🃏 Card Game – Duel tour par tour

Jeu de cartes **2 joueurs** en tour par tour pour Unity (PC/Mobile). Phase 1 : **duel contre un bot**. Moteur en C# sans réseau, conçu pour être branché plus tard en P2P.

---

## ✨ Fonctionnalités

- **2 decks** : Magicien et Guerrier (34 cartes chacun, 4 équipements obligatoires)
- **Règles complètes** : PV, bouclier, Force, Résistance, mana, pioche, frappe (1/tour), équipements actifs à la frappe / début / fin de tour
- **Effets à durée** : Orage de poche, Armure psychique, Lien karmique (compteur de tours, expiration)
- **Glace localisée** : gel d’un équipement adverse, dégel par carte dégâts ou frappe « briser le gel », affichage en bleu
- **Cartes Éphémère** : chaque exemplaire jouable une fois, les autres exemplaires restent disponibles
- **IA** : bot simple (SimpleBot) pour le Joueur 2
- **UI** : TextMeshPro, statut joueurs (Joueur 1 humain / Joueur 2), main, Frappe, Fin de tour, équipements, effets à durée

---

## 🛠 Technologies

- **Unity** (compatible PC / Mobile)
- **C#** – moteur dans `Core/` sans dépendance Unity
- **TextMeshPro** pour tous les textes

---

## 📁 Structure du projet

```
Scripts/CardGame/
├── Core/           # Moteur de jeu (état, règles, résolution des effets)
├── Data/           # Définition des decks (Magicien, Guerrier)
├── Bot/            # IA SimpleBot
├── Unity/          # GameController, GameUI, GameLogger
└── README.md
```

| Dossier  | Rôle |
|----------|------|
| **Core** | `GameState`, `GameSession`, `EffectResolver`, `PlayerState`, `EquipmentState`, `ActiveDurationEffect`, types de cartes et actions |
| **Data** | `DeckDefinitions`, `CardData`, `CardId` |
| **Bot**  | `SimpleBot` – choix d’action pour le Joueur 2 |
| **Unity**| Pilote de partie, interface, logs |

---

## 🚀 Installation et lancement (Unity)

1. **Ouvrir la scène** dans Unity.
2. **Créer un objet vide** → ajouter le script **GameController**.
3. **Créer un Canvas** (UI) puis sous le Canvas :
   - Textes TMP : statut, Joueur 1, Joueur 2
   - Conteneur vide pour la main (ex. `HandContainer`)
   - 2 boutons : **Frappe**, **Fin de tour**
   - (Optionnel) Conteneurs pour équipements et effets par joueur
4. **Créer un objet** sous le Canvas → ajouter le script **GameUI**.
5. **Brancher dans l’Inspector** :
   - Controller → `GameController`
   - _text Status, _text Joueur1, _text Joueur2
   - Hand Container, Button Strike, Button End Turn
   - Containers équipements / effets si utilisés
6. **Play** : Joueur 1 = toi (humain), Joueur 2 = bot. Premier joueur aléatoire.

Référence détaillée : `Assets/Document/carte_spec_complete.md`  
Rapport d’implémentation : `Assets/Document/RAPPORT_phase1_implementation.md`

---

## 🎮 Règles rapides

- **Joueur 1** = humain (index 0), **Joueur 2** = adversaire (index 1).
- **1 frappe par tour** ; si l’arme est gelée, la frappe peut servir à briser le gel (sans dégâts).
- **Équipement gelé** (bleu) : se dégèle en jouant une carte qui fait des dégâts ou en utilisant Frappe pour briser le gel.
- **Éphémère** : chaque exemplaire n’est jouable qu’une fois ; les autres exemplaires restent jouables.

---

## 📋 Configuration (Inspector)

**GameController**

- Humain = Joueur 1 (coché par défaut)
- Decks : Joueur 1 (ex. Magicien), Joueur 2 (ex. Guerrier)
- Logs dans un fichier (optionnel)

**GameUI**

- Tous les champs texte et conteneurs doivent être assignés pour un affichage complet.

---

## 📄 Logs

Les actions et changements d’état sont logués via **GameLogger** (Console Unity + fichier dans `Application.persistentDataPath` si l’option est activée).

---

## 🔜 Évolution

- **Phase 2** : remplacer le bot par une couche P2P (même moteur, même `SubmitAction`).

---

## 📜 Licence et références

Spécifications : `Document/carte_spec_complete.md`, `Document/PROMPT_jeu_cartes.md`.
