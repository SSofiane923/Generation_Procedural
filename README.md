# Présentation des differentes methodes de génération procédural.

# 1. Simple Room Placement 

## Fonctionnement

La méthode **Simple Room Placement** génère des donjons en plaçant aléatoirement des salles sur une grille, puis en les connectant via des corridors.

### Principe général

L'algorithme se déroule en **3 phases principales** :

1. **Génération des salles** : Placement aléatoire de rectangles sur la grille
2. **Connexion des salles** : Création de corridors entre les salles adjacentes
3. **Construction du sol** : Ajout des tuiles de terrain de base

---

## Paramètres
```csharp
[Header("Room Parameters")]
[SerializeField] private int _maxRooms = 10;
[SerializeField] private Vector2Int _roomMinSize = new(5, 5);
[SerializeField] private Vector2Int _roomMaxSize = new(12, 8);
```

- **_maxRooms** : Limite le nombre de salles générées
- **_roomMinSize / _roomMaxSize** : Définissent la plage de tailles possibles pour les salles

---

## Details

### 1: Génération des salles
```csharp
for (int i = 0; i < _maxSteps; i++)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();
        
        if (roomsPlacedCount >= _maxRooms)
        {
            break;
        }
        
        attempts++;

        // choose a random size
        int width = RandomService.Range(_roomMinSize.x, _roomMaxSize.x + 1);
        int lenght = RandomService.Range(_roomMinSize.y, _roomMaxSize.y + 1);

        // choose random position so entire room fits into grid
        int x = RandomService.Range(0, Grid.Width - width);
        int y = RandomService.Range(0, Grid.Lenght - lenght);

        RectInt newRoom = new RectInt(x, y, width, lenght);

        if (!CanPlaceRoom(newRoom, 1)) 
            continue;
        
        PlaceRoom(newRoom);
        placedRooms.Add(newRoom);

        roomsPlacedCount++;

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken : cancellationToken);
    }
```

**Étapes: **
1. **Génération aléatoire** : Une taille et une position sont tirées aléatoirement
2. **Vérification** : `CanPlaceRoom()` vérifie qu'aucune salle n'occupe déjà cet espace
3. **Placement** : Si l'espace est libre, la salle est marquée sur la grille via `PlaceRoom()`
4. **Limite de tentatives** : `_maxSteps` empêche une boucle infinie si la grille est saturée

La méthode `PlaceRoom()` parcourt toutes les cellules du rectangle et les marque comme occupées :
```csharp
private void PlaceRoom(RectInt room)
{
    for (int ix = room.xMin; ix < room.xMax; ix++)
    {
        for (int iy = room.yMin; iy < room.yMax; iy++)
        {
            if (!Grid.TryGetCellByCoordinates(ix, iy, out var cell)) continue;
            
            AddTileToCell(cell, ROOM_TILE_NAME, true);
        }
    }
}
```

---

### 2: Connexion des salles
```csharp
for (int i = 0; i < placedRooms.Count - 1; i++)
{
    Vector2Int start = placedRooms[i].GetCenter();      // Centre de la salle actuelle
    Vector2Int end = placedRooms[i + 1].GetCenter();    // Centre de la salle suivante
    
    CreateDogLegCorridor(start, end);
}

private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
{
    bool horizontalFirst = RandomService.Chance(0.5f);
    
    if (horizontalFirst)
    {
        CreateHorizontalCorridor(start.x, end.x, start.y);  // Ligne horizontale
        CreateVerticalCorridor(start.y, end.y, end.x);      // Puis ligne verticale
    }
    else
    {
        CreateVerticalCorridor(start.y, end.y, start.x);    // Ligne verticale
        CreateHorizontalCorridor(start.x, end.x, end.y);    // Puis ligne horizontale
    }
}
```

### 3: Construction du sol
```csharp
private void BuildGround()
{
    var groundTemplate = ScriptableObjectDatabase.GetScriptableObject("Grass");
    
    for (int x = 0; x < Grid.Width; x++)
    {
        for (int z = 0; z < Grid.Lenght; z++)
        {
            if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell)) continue;
            
            GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
        }
    }
}
```

---

# 2. BSP (Binary Space Partitioning)

## Fonctionnement

La méthode **BSP (Binary Space Partitioning)** génère des donjons en divisant **récursivement** l'espace en zones de plus en plus petites.

### Principe général

Le programme utilise une approche **récursive** basée sur un arbre binaire :

1. **Division de l'espace** : La carte est découpée en deux zones (horizontalement ou verticalement)
2. **Récursion** : Chaque zone est à nouveau divisée jusqu'à atteindre une taille minimale
3. **Placement des salles** : Dans chaque feuille de l'arbre (zone non divisible), une salle est placée
4. **Connexion** : Les salles "sœurs" (issues du même parent) sont connectées par des corridors

---

## Paramètres

```csharp
[Header("Split Parameters")]
[Range(0,1)] public float HorizontalSplitChance = 0.5f;  
public Vector2 SplitRatio = new(0.3f, 0.7f);
public int MaxSplitAttempt = 5;

[Header("Leafs Parameters")]
public Vector2Int LeafMinSize = new(8, 8);
public Vector2Int RoomMaxSize = new(7, 7);
public Vector2Int RoomMinSize = new(5, 5);
```

**Explications :**
- **HorizontalSplitChance** : Détermine si la division sera horizontale ou verticale
- **SplitRatio** : Position du découpage 
- **LeafMinSize** : Empêche de créer des zones trop petites
- **RoomMinSize/MaxSize** : Contrôle la taille des salles placées dans les feuilles

---

### 1: Initialisation et construction de l'arbre
```csharp
protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        Tree = new List<Node>();
        
        var allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        // Create all tree.
        var root = new Node(RandomService, this, allGrid);
        Tree.Add(root);
        
        root.ConnectSisters();
    }
```

**Fonctionnement: **
1. Un **nœud racine** représente toute la grille
2. Le constructeur `Node()` déclenche automatiquement la division récursive
3. `ConnectSisters()` crée les corridors entre les salles

---

### 2: Division récursive (méthode Split)
```csharp
 private void Split()
    {
        RectInt splitBoundsLeft = default;
        RectInt splitBoundsRight = default;
        bool splitFound = false;

        for (int i = 0; i < _bsp2.MaxSplitAttempt; i++)
        {
            bool horizontal = _randomService.Chance(_bsp2.HorizontalSplitChance);
            float splitRatio = _randomService.Range(_bsp2.SplitRatio.x, _bsp2.SplitRatio.y);
        
            if (horizontal)
            {
                if (!CanSplitHorizontally(splitRatio, out splitBoundsLeft, out splitBoundsRight))
                {
                    continue;
                }
            }
            else
            {
                if (!CanSplitVertically(splitRatio, out splitBoundsLeft, out splitBoundsRight))
                {
                    continue;
                }
            }
            
            splitFound = true;
            break;
        }

        // Stop recursion, it's a Leaf !
        if (!splitFound)
        {
            _isLeaf = true;
            PlaceRoom(_room);
            
            return;
        }

        _child1 = new Node(_randomService, _bsp2, splitBoundsLeft);
        _child2 = new Node(_randomService, _bsp2, splitBoundsRight);
        
        _bsp2.Tree.Add(_child1);
        _bsp2.Tree.Add(_child2);
    }
```

**Étapes: **

1. **Tentatives de division** : Jusqu'à `MaxSplitAttempt` essais pour trouver un découpage valide
2. **Choix du type** : Horizontal (gauche/droite) ou Vertical (haut/bas)
3. **Calcul du ratio** : Position du découpage (ex: 35% / 65%)
4. **Validation** : Les deux zones résultantes doivent respecter `LeafMinSize`
5. **Récursion ou arrêt** :
   - Si division réussie → Création de 2 enfants qui se diviseront à leur tour
   - Si division impossible → **Feuille** détectée → Placement d'une salle

---

### 3: Placement des salles
```csharp
 private void PlaceRoom(RectInt room)
    {
        // Add some randomness to the room size.
        var newRoomLength = _randomService.Range(_bsp2.RoomMinSize.x, _bsp2.RoomMaxSize.x + 1);
        var newRoomWidth = _randomService.Range(_bsp2.RoomMinSize.y, _bsp2.RoomMaxSize.y + 1);
        
        room.width = newRoomWidth;
        room.height = newRoomLength;
        
        // Reinject into room to get the correct center.
        _room = room;
        
        for (int ix = room.xMin; ix < room.xMax; ix++)
        {
            for (int iy = room.yMin; iy < room.yMax; iy++)
            {
                if (!_gridGenerator.Grid.TryGetCellByCoordinates(ix, iy, out var cell)) 
                    continue;
                    
                var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Room");
                _gridGenerator.AddGridObjectToCell(cell, groundTemplate, true);
            }
        }
    }
```

---

### 4: Connexion des corridors
```csharp
public void ConnectSisters()
    {
        // It's a leaf, nothing to do here.
        if (_child1 == null || _child2 == null) 
            return;
        
        // Connect sisters
        ConnectNodes(_child1, _child2);
            
        // Connect child of sisters
        _child1.ConnectSisters();
        _child2.ConnectSisters();
    }

private void ConnectNodes(Node node1, Node node2)
    {
        var center1 = node1.GetLastChild()._room.GetCenter();
        var center2 = node2.GetLastChild()._room.GetCenter();
        
        CreateDogLegCorridor(center1, center2);
    }
    
    /// Creates an L-shaped corridor between two points, randomly choosing horizontal-first or vertical-first
private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
    {
        bool horizontalFirst = _randomService.Chance(0.5f);

        if (horizontalFirst)
        {
            // Draw horizontal line first, then vertical
            CreateHorizontalCorridor(start.x, end.x, start.y);
            CreateVerticalCorridor(start.y, end.y, end.x);
        }
        else
        {
            // Draw vertical line first, then horizontal
            CreateVerticalCorridor(start.y, end.y, start.x);
            CreateHorizontalCorridor(start.x, end.x, end.y);
        }
    }
```


# 3. Cellular Automata

## Fonctionnement

La méthode **Cellular Automata** génère des terrains organiques en utilisant un système inspiré du **Jeu de la Vie** de Conway. Au lieu de placer des salles, elle crée des patterns naturels comme des cavernes, îles ou lacs.

### Principe général

L'algorithme fonctionne en **3 phases** :

1. **Génération de bruit ou noise generation** : Placement aléatoire de tuiles selon une densité
2. **Évolution** : Chaque cellule évolue selon ses voisins
3. **Lissage** : Les patterns deviennent progressivement cohérents et organiques

---

## Paramètres
```csharp
[SerializeField, Tooltip("The percentage of ground tile on the map."), Range(0,100)] 
private int _groundDensity = 10;

[SerializeField, Tooltip("How many ground neighbour a cell need to be consider as a ground itself.")] 
private int _minGroundNeighbourCount = 4;
```

**Explications :**
- **_groundDensity** : Pourcentage de tuiles "sol" dans la grille initiale (10% = beaucoup d'eau, 90% = beaucoup de terre)
- **_minGroundNeighbourCount** : Nombre minimum de voisins "sol" pour qu'une cellule devienne/reste un "sol" (règle d'évolution)

---

## Details

### 1: Initialisation, Génération de bruit
```csharp
protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
{
    GenerateNoiseGrid(_groundDensity);
    
    await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    
    // Suite : itérations de lissage...
}
```

La première étape crée une grille de **bruit aléatoire** (white noise) :
```csharp
private void GenerateNoiseGrid(int noiseDensity)
{
    for (int x = 0; x < Grid.Width; x++)
    {
        for (int z = 0; z < Grid.Lenght; z++)
        {
            if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
            {
                Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                continue;
            }
            
            bool grassTile = RandomService.Range(0,100) < noiseDensity;
            AddTileToCell(chosenCell, grassTile ? GRASS_TILE_NAME : WATER_TILE_NAME, true);
        }
    }
}
```

**Fonctionnement :**
- Parcourt **chaque cellule** de la grille
- Génère un nombre aléatoire entre 0 et 100
- Si le nombre < `noiseDensity` → tuile "sol" (grass)
- Sinon → tuile "eau" (water)
---

### 2: Lissage
```csharp
for (int i = 0; i < _maxSteps; i++)
{
    Debug.Log($"Step {i}");
    cancellationToken.ThrowIfCancellationRequested();

    bool[][] isGround = new bool[Grid.Width][];
    for (int index = 0; index < Grid.Width; index++)
    {
        isGround[index] = new bool[Grid.Lenght];
    }
    
    for (int x = 0; x < Grid.Width; x++)
    {
        for (int z = 0; z < Grid.Lenght; z++)
        {
            if (!Grid.TryGetCellByCoordinates(x, z, out var scannedCell))
            {
                Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                continue;
            }

            isGround[x][z] = ScanCell(scannedCell);
        }
    }

    for (int x = 0; x < Grid.Width; x++)
    {
        for (int z = 0; z < Grid.Lenght; z++)
        {
            if (!Grid.TryGetCellByCoordinates(x, z, out var cell))
            {
                Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                continue;
            }

            AddTileToCell(cell, isGround[x][z] ? GRASS_TILE_NAME : WATER_TILE_NAME, true);
        }
    }
    
    await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
}
```

### 3: Scan des voisins
```csharp
private bool ScanCell(Cell scannedCell)
{
    int neighbourGroundCount = 0;

    for (int x = scannedCell.Coordinates.x - 1; x <= scannedCell.Coordinates.x + 1; x++)
    {
        for (int y = scannedCell.Coordinates.y - 1; y <= scannedCell.Coordinates.y + 1; y++)
        {
            if (x == scannedCell.Coordinates.x && y == scannedCell.Coordinates.y)
            {
                continue;
            }
            
            if (!Grid.TryGetCellByCoordinates(x, y, out var neighbourCell))
            {
                continue;
            }
            
            if (neighbourCell.ContainObject && neighbourCell.GridObject.Template.Name == GRASS_TILE_NAME)
            {
                neighbourGroundCount++;
            }
        }
    }
    
    return neighbourGroundCount >= _minGroundNeighbourCount;
}
```

