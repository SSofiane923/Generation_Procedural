using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Microsoft.Unity.VisualStudio.Editor;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;
using VTools.ScriptableObjectDatabase;

[CreateAssetMenu(menuName = "Procedural Generation Method/Test Cellular Automata")]
public class TestCellularAutomata : ProceduralGenerationMethod
{
    [SerializeField] public int noiseDensity = 50;
    private CellularAutomata _cel;
    private VTools.Grid.Grid _grid;
    private bool[,] cell;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        await UniTask.Yield(cancellationToken);
    }

    public void Build()
    {

        for(int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Lenght; y++)
            {
                if (RandomService.Range(0, 100) < noiseDensity)
                    IsWater(x, y);
                else IsGround(x, y);

                /*if (_cel.IsWater(x, y) == true)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    {
                        GridGenerator.AddGridObjectToCell(cell, waterTemplate, true);
                    }
                }
                else if (_cel.IsGround(x, y) == true)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    {
                        GridGenerator.AddGridObjectToCell(cell, groundTemplate, true);
                    }
                }*/
            }
        }
    }
    private void SetUpArea()
    {
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Lenght; y++)
            {
                cell[x, y] = true;
            }
        }
    }

    private void IsWater(int x, int y)
    {
        var waterTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(WATER_TILE_NAME);
        if (_grid.TryGetCellByCoordinates(x, y, out var gridCell))
        {
            GridGenerator.AddGridObjectToCell(gridCell, waterTemplate, true);
        }
    }

    private void IsGround(int x, int y)
    {
        var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>(GRASS_TILE_NAME);
        if (_grid.TryGetCellByCoordinates(x, y, out var gridCell))
        {
            GridGenerator.AddGridObjectToCell(gridCell, groundTemplate, true);
        }
    }
}

public class CellularAutomata
{
    public CellularAutomata(VTools.Grid.Grid grid)
    {
    }

}