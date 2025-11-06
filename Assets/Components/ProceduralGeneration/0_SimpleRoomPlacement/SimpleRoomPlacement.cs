using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]
    public class SimpleRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;
        
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {

            for (int i = 0; i < _maxSteps; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                int x = RandomService.Range(0, Grid.Width);
                int y = RandomService.Range(0, Grid.Lenght);
                RectInt room = new RectInt(x,y,10,10);
                if (CheckTile(room))
                { PlaceRoom(room); }
                // Waiting between steps to see the result.
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken : cancellationToken);
            }

            // Final ground building.
            BuildGround();
        }


        private void PlaceRoom(RectInt room)
        {
            for (int ix = room.xMin; ix < room.xMax; ix++)
            {
                for (int iy = room.yMin; iy < room.yMax; iy++)
                {
                    if (!Grid.TryGetCellByCoordinates(ix, iy, out Cell cell))
                        continue;

                    AddTileToCell(cell, ROOM_TILE_NAME, true);
                }
            }
        }

        private bool CheckTile(RectInt room)
        {
            Cell cell;
            for (int ix = room.xMin; ix < room.xMax; ix++)
            {
                for (int iy = room.yMin; iy < room.yMax; iy++)
                {
                    if (Grid.TryGetCellByCoordinates(ix, iy, out cell))
                    {
                        if (cell.ContainObject)
                            return false;
                        
                    }
                        
                }
            }
            return true;
        }

        private void BuildRoad()
        {

        }
        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");
            
            // Instantiate ground blocks
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int z = 0; z < Grid.Lenght; z++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                    {
                        Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                        continue;
                    }
                    
                    GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
                }
            }
        }
    }
}