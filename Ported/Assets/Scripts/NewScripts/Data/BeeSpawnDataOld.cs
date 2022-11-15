using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct BeeSpawnDataOld : IComponentData
{
    public Entity beeToSpawn;
    public float3 position;
    public int team;
}
