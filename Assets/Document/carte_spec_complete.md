# Jeu de cartes tour par tour

- **34 cartes** par deck (dont 4 obligatoires)
- Plusieurs **types de cartes** avec des règles distinctes

---

## Règles générales

### Types de cartes

- **Equipé** : Carte placée sur le board et effective jusqu'à la fin de la partie. Les **4 cartes obligatoires** du deck sélectionné (conception par classe) sont des équipements, automatiquement posés sur le board. Elles **ne coûtent pas de mana** ; le **coût** indiqué est le **nombre de tours (rounds) nécessaires pour qu'elles s'activent** une fois posées. Les équipements s'activent au fur et à mesure des rounds. (Évolution possible : personnalisation des decks avec les mêmes principes.)
- **Normal** : Carte placée au **cimetière** après utilisation.
- **Éphémère** : Carte utilisable **une fois par partie**. Après utilisation, elle est en règle générale **retirée du jeu** (elle ne va pas au cimetière). **Exception** : si une carte précisée par son effet a été jouée avant (ex. « Discipline éternel » pour « Souffle éternel »), la carte Éphémère est envoyée au **cimetière** au lieu d'être retirée du jeu.
- **Rapide** : Carte jouable **uniquement pendant le tour adverse**, en réaction à une action (ex. une attaque). Les cartes Rapides sont **cumulables**. Si une carte Rapide a un coût en mana, le joueur utilise le mana qu'il avait **à la fin de son dernier tour** (encore valide pour réagir) ; au début de son prochain tour, son mana est recalculé (pioche + mana du tour) et le mana non dépensé pour des Rapides est perdu.

### Valeurs du joueur

- **PV** : Points de vie du joueur (**100 PV** au départ). Condition de défaite : 0 PV.
- **Bouclier** : Armure qui encaisse les dégâts **avant** les PV. Les dégâts touchent d'abord le bouclier, puis les PV. Au début de chaque tour du joueur, le bouclier est **remis à 0**.
- **Force** : Augmente les dégâts infligés. **Formule** : dégâts infligés = base × (1 + Force).
- **Résistance** : Augmente le bouclier reçu. **Formule** : bouclier reçu = base × (1 + Résistance).
- **Mana** : Énergie dépensée pour jouer des cartes (hors équipements déjà sur le board). Le mana non dépensé en fin de tour ne se cumule pas, **sauf** pour les cartes Rapides (voir ci-dessous). Plafond 3 mana à partir du tour 3, sauf si une carte l'augmente.

### Frappe

**Frappe** = action de porter un **coup physique** à l'adversaire (attaque avec l'arme / action d'attaque physique). Certains équipements ou cartes se déclenchent « à chaque frappe » ou « quand vous frappez ».

---

## Système de tour

- **Tour 1** : 3 cartes en main, 1 point de mana
- **Tour 2** : 4 cartes, 2 points de mana
- **Tour 3** : 5 cartes, 3 points de mana
- **À partir du tour 3** : plafond fixe — 5 cartes en pioche et 3 mana par tour (pas de carte ni mana supplémentaire au-delà).

```
1 tour = 1 joueur
```

**Effets « durant X tours »** : chaque **tour de jeu** compte pour 1, quel que soit le joueur (ex. « 2 tours » = 2 tours de jeu).

**Premier joueur** : ordre de jeu initial déterminé par **tirage au sort**.

### Déroulement d'un tour

1. **Début du tour** : Le joueur **défausse toute sa main**, puis les **boucliers sont remis à 0**.
2. **Résolution** des effets de cartes et d'équipements (début de tour).
3. **Tirage des cartes** :
   - Pioche selon le numéro de tour (3 / 4 / 5 cartes, plafond 5 à partir du tour 3).
   - Si le deck est vide : le joueur doit d'abord jouer avec les cartes déjà en main ; quand il doit piocher et que le deck est vide, il **mélange le cimetière** et le replace comme deck, puis pioche.
   - Si le deck a encore des cartes mais pas assez pour une pioche complète, le joueur ne pioche que les cartes restantes.
4. **Le joueur joue** (pose des cartes, déclare des frappes, etc.). **Timing des dégâts** : lorsqu'une carte de dégâts ou une frappe est activée, le joueur doit le **signaler** ; un **timing équitable** permet à l'adversaire de jouer des cartes Rapides **avant** la résolution des dégâts. Pendant le tour adverse, l'autre joueur peut jouer des cartes **Rapides** en réaction (cumulables).
5. **Résolution** des effets de cartes et d'équipements (fin de tour).
6. **Fin du tour**.

### Main et cimetière

- **Pas de limite de main** : une fois la pioche réalisée, le joueur peut garder autant de cartes en main qu'il en a.
- **Cartes utilisées** : vont au **cimetière** sauf les cartes **Éphémère** (retirées du jeu, sauf exception comme Souffle éternel après Discipline éternel → cimetière).

---

## Condition de victoire

La victoire est accordée au joueur ayant **réduit les points de vie adverses à 0**.

---

# Deck Magicien

### Cartes obligatoires (Equipé)

- **Catalyseur arcanaique restraint**
  - **Nombre** : 1
  - **Effets** : Donne 1 bouclier à chaque fois que vous frappez
  - **Type** : Equipé
  - **Coût** : 0 (nombre de tours pour s'activer une fois posé)

- **Rune d'énergie arcanique**
  - **Nombre** : 1
  - **Effets** : Pioche 2 cartes supplémentaires en début de tour
  - **Type** : Equipé
  - **Coût** : 6 (nombre de tours pour s'activer une fois posé)

- **Rune d'essence arcanique**
  - **Nombre** : 1
  - **Effets** : Donne 5 bouclier à la fin du tour, si résistance est à 0
  - **Type** : Equipé
  - **Coût** : 1 (nombre de tours pour s'activer une fois posé)

- **Rune de force arcanique**
  - **Nombre** : 1
  - **Effets** : Inflige 2 × 1 dégât quand vous frappez
  - **Type** : Equipé
  - **Coût** : 4 (nombre de tours pour s'activer une fois posé)

### Autres cartes

- **Explosion de magie éphémère**
  - **Nombre** : 1
  - **Effets** : Inflige (nombre de cartes consumées) × 2 dégâts. **Cartes consumées** = cartes **défaussées ce tour** (par le joueur).
  - **Type** : Normal
  - **Coût** : 1

- **Divination**
  - **Nombre** : 3
  - **Effets** : Pioche 2 cartes ; le joueur **choisit** laquelle poser au-dessus du deck, **l'autre reste en main**.
  - **Type** : Normal
  - **Coût** : 1

- **Armure psychique**
  - **Nombre** : 3
  - **Effets** : 23 bouclier durant 2 tours
  - **Type** : Éphémère
  - **Coût** : 3

- **Boule de feu**
  - **Nombre** : 3
  - **Effets** : 15 dégâts
  - **Type** : Éphémère
  - **Coût** : 3

- **Attaque +**
  - **Nombre** : 2
  - **Effets** : 9 dégâts
  - **Type** : Normal
  - **Coût** : 2

- **Défense +**
  - **Nombre** : 2
  - **Effets** : 15 bouclier
  - **Type** : Normal
  - **Coût** : 2

- **Concentration**
  - **Nombre** : 2
  - **Effets** : Donne 3 Force ; pendant le prochain tour octroie 3 Résistance
  - **Type** : Éphémère
  - **Coût** : 2

- **Lien karmique**
  - **Nombre** : 1
  - **Effets** : Donne 3 Résistance pendant 3 tours
  - **Type** : Normal
  - **Coût** : 2

- **Défense**
  - **Nombre** : 3
  - **Effets** : 4 bouclier ; octroie 4 bouclier supplémentaires si **aucune attaque** (frappe ou carte portant coup et dégâts) n'est faite durant le tour
  - **Type** : Normal
  - **Coût** : 1

- **Galvanisation**
  - **Nombre** : 1
  - **Effets** : Donne 1 Force pour chaque carte en main, jusqu'à la fin du tour
  - **Type** : Normal
  - **Coût** : 1

- **Evaluation**
  - **Nombre** : 2
  - **Effets** : Pioche 3 cartes
  - **Type** : Éphémère
  - **Coût** : 1

- **Attaque**
  - **Nombre** : 3
  - **Effets** : 5 dégâts
  - **Type** : Normal
  - **Coût** : 1

- **Orage de poche**
  - **Nombre** : 2
  - **Effets** : Inflige 1 dégât avant la fin de chaque tour, n'est pas influencé par la Force, effectif durant 3 tours
  - **Type** : Éphémère
  - **Coût** : 3

- **Glace localisée**
  - **Nombre** : 2
  - **Effets** : Gèle une carte Equipé de l'adversaire. L'équipement est **gelé au début du tour suivant** l'activation de Glace localisée (effet désactivé). **Un coup (frappe)** suffit à rétablir l'équipement (le propriétaire déclare une frappe avec l'arme gelée pour briser le gel).
  - **Type** : Éphémère
  - **Coût** : 1

---

# Deck Guerrier

**Passif de classe** : le joueur possédant le deck Guerrier a **+1 Force** et **+1 Résistance** de base (en permanence).

### Cartes obligatoires (Equipé)

- **Hache de l'oublié**
  - **Nombre** : 1
  - **Effets** : 5 dégâts, 1 fois par tour
  - **Type** : Equipé
  - **Coût** : 0 (nombre de tours pour s'activer une fois posé)

- **Rune d'endurance de l'oublié**
  - **Nombre** : 1
  - **Effets** : Donne 3 PV au début du tour
  - **Type** : Equipé
  - **Coût** : 1 (nombre de tours pour s'activer une fois posé)

- **Rune de protection de l'oublié**
  - **Nombre** : 1
  - **Effets** : Si 2 frappes sont enchaînées, donne 2 bouclier
  - **Type** : Equipé
  - **Coût** : 2 (nombre de tours pour s'activer une fois posé)

- **Rune d'agressivité de l'oublié**
  - **Nombre** : 1
  - **Effets** : À chaque frappe, octroie 1 Force jusqu'à la fin du tour
  - **Type** : Equipé
  - **Coût** : 5 (nombre de tours pour s'activer une fois posé)

### Autres cartes

- **Repositionnement**
  - **Nombre** : 2
  - **Effets** : Donne 2 bouclier et pioche 1 carte
  - **Type** : Normal
  - **Coût** : 0

- **Contre-attaque**
  - **Nombre** : 3
  - **Effets** : Si pendant le tour adverse vous possédez cette carte en main, vous pouvez annuler une attaque de l'ennemi et infliger 2 dégâts
  - **Type** : Rapide
  - **Coût** : 0

- **Parade**
  - **Nombre** : 3
  - **Effets** : Si pendant le tour adverse vous possédez cette carte en main, vous pouvez annuler une attaque de l'ennemi
  - **Type** : Rapide
  - **Coût** : 1

- **Position offensive**
  - **Nombre** : 2
  - **Effets** : Octroie 1 Force
  - **Type** : Éphémère
  - **Coût** : 1

- **Appuis solide**
  - **Nombre** : 3
  - **Effets** : Votre arme fait 1 dégât supplémentaire
  - **Type** : Éphémère
  - **Coût** : 1

- **Défense lourde**
  - **Nombre** : 2
  - **Effets** : Donne 10 bouclier
  - **Type** : Normal
  - **Coût** : 1

- **Position défensive**
  - **Nombre** : 2
  - **Effets** : Donne 1 Résistance
  - **Type** : Éphémère
  - **Coût** : 1

- **Souffle éternel**
  - **Nombre** : 1
  - **Effets** : Rend 15 PV au joueur. La carte n'est pas détruite (va au cimetière) si elle est utilisée après **« Discipline éternel »**
  - **Type** : Éphémère
  - **Coût** : 0

- **Discipline éternel**
  - **Nombre** : 1
  - **Effets** : Invincible jusqu'au prochain tour
  - **Type** : Normal
  - **Coût** : 3

- **Guillotine**
  - **Nombre** : 2
  - **Effets** : Attaque avec votre arme, la Force compte double, met fin au tour
  - **Type** : Normal
  - **Coût** : 1

- **Fendoire mortel**
  - **Nombre** : 1
  - **Effets** : 20 dégâts
  - **Type** : Éphémère
  - **Coût** : 1

- **Attaque lourde**
  - **Nombre** : 2
  - **Effets** : 7 dégâts, 4 bouclier
  - **Type** : Normal
  - **Coût** : 2

- **Attaque légère**
  - **Nombre** : 3
  - **Effets** : 3 dégâts, 2 bouclier
  - **Type** : Normal
  - **Coût** : 1

- **Attaque tactique**
  - **Nombre** : 3
  - **Effets** : 2 dégâts, 1 bouclier
  - **Type** : Normal
  - **Coût** : 0
