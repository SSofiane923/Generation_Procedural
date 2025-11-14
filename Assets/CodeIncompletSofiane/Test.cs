using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VTools.Grid;
using VTools.RandomService;


[CreateAssetMenu(menuName = "Procedural Generation Method/Test algo")]
public class Test : ProceduralGenerationMethod
{

    [SerializeField] private int _Iteration = 4;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        Debug.Log("Test Algo");
        int x = RandomService.Range(0, Grid.Width);
        int y = RandomService.Range(0, Grid.Lenght);
        var allGrid = new RectInt(x,y,Grid.Width, Grid.Lenght);
        var root =  new TestNode(allGrid, RandomService, _Iteration);

        root.Divide();
    }
}

public class TestNode
{
    private RectInt _bounds;
    private RandomService _randomService;
    private int _iteration;
    private int _currentIteration;
    private TestNode _child1, _child2;
    public TestNode(RectInt bounds, RandomService randomService, int iteration, int currentIteration = 0)
    {
        _bounds = bounds;
        _randomService = randomService;
        _iteration = iteration;
        _currentIteration = currentIteration;
    }
    public void Split()
    {
        RectInt splitBoundsLeft = new RectInt(_bounds.xMin, _bounds.yMin, _bounds.width / 2, _bounds.height);
        RectInt splitBoundsRight = new RectInt(_bounds.xMin + _bounds.width / 2, _bounds.yMax, _bounds.width / 2, _bounds.height);

        _child1 = new TestNode(splitBoundsLeft, _randomService, _iteration, _currentIteration + 1);
        _child2 = new TestNode(splitBoundsRight, _randomService, _iteration, _currentIteration + 1);
        Debug.Log("Child" + _currentIteration + ": " + _child1._bounds);
        Debug.Log("Child" + _currentIteration + ": " + _child2._bounds);
        //PlaceBlock();


    }
    
    public void Divide()
    {
        if (_iteration == _currentIteration) { return; }
        Split();
        _child1.Divide();
        _child2.Divide();
    }


    /*private void PlaceBlock()
    {
        for (int x = _bounds.xMin; x <= _bounds.xMax; x++)
        {
            for (int y = _bounds.yMin; y <= _bounds.yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out Cell cell)) continue 
            }
        }
    }*/
}