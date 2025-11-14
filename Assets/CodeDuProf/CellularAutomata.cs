using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;

namespace Components.ProceduralGeneration._2_CellularAutomata
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Cellular Automata")]
    public class CellularAutomata : ProceduralGenerationMethod
    {
        [SerializeField, Tooltip("The percentage of ground tile on the map."), Range(0,100)] private int _groundDensity = 10;
        [SerializeField, Tooltip("How many ground neighbour a cell need to be consider as a ground itself.")] private int _minGroundNeighbourCount = 4;
        
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // Generate noise grid
            GenerateNoiseGrid(_groundDensity);
            
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

            for (int i = 0; i < _maxSteps; i++)
            {
                Debug.Log($"Step {i}");
                cancellationToken.ThrowIfCancellationRequested();

                // Jagged array of bools.
                bool[][] isGround = new bool[Grid.Width][];
                for (int index = 0; index < Grid.Width; index++)
                {
                    isGround[index] = new bool[Grid.Lenght];
                }
                
                // Compute each cell's based on its neighbors and save the result in the array.
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
        }

        private bool ScanCell(Cell scannedCell)
        {
            int neighbourGroundCount = 0;

            for (int x = scannedCell.Coordinates.x - 1; x <= scannedCell.Coordinates.x + 1; x++)
            {
                for (int y = scannedCell.Coordinates.y - 1; y <= scannedCell.Coordinates.y + 1; y++)
                {
                    // Skip itself.
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
    }
}