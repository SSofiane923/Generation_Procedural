using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(menuName = "Procedural Generation Method/Simplex")]
public class Simplex : ProceduralGenerationMethod
{
    [Header("Noise parameter")]
    [SerializeField] public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [Range(0.0f, 1f)] public float frequency;
    [Range(0.0f, 1f)] public float amplitude;
    [Header("Fractar parameter")]
    [SerializeField] public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    [Range(0, 15)] public int octave;
    [Range(0.0f, 1f)] public float lacunarity;
    [Range(0.0f, 1f)] public float persistence;
    [Header("Heights")]
    [Range(-1f, 1f)] public float waterHeight;
    [Range(-1f, 1f)] public float sandHeight;
    [Range(-1f, 1f)] public float grassHeight;
    [Range(-1f, 1f)] public float RockHeight;


    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        var noise = new FastNoiseLite(RandomService.Seed);
        noise.SetFrequency(frequency);
        noise.SetFractalOctaves(octave);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(persistence);
        float[][] noisemap = new float[Grid.Lenght][];
        for (int index = 0; index < Grid.Lenght; index++)
        {
            noisemap[index] = new float[Grid.Width];
        }
        for (int x = 0; x < Grid.Lenght; x++)
        {
            for (int y = 0; y < Grid.Width; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if(!Grid.TryGetCellByCoordinates(x,y, out var cell))
                {
                    continue;
                }
                noisemap[x][y] = noise.GetNoise(x, y);
                var tileName = GRASS_TILE_NAME;
                if (noisemap[x][y] < waterHeight)
                {
                    tileName = WATER_TILE_NAME;
                }
                else if (noisemap[x][y]> waterHeight && noisemap[x][y] < sandHeight)
                {
                    tileName = SAND_TILE_NAME;
                }
                else if (noisemap[x][y] > sandHeight && noisemap[x][y] < grassHeight)
                {
                    tileName = GRASS_TILE_NAME;
                }
                else if (noisemap[x][y] > grassHeight)
                {
                    tileName = ROCK_TILE_NAME;
                }

                AddTileToCell(cell, tileName, true);
            }
        }
    }


}
