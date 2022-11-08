using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class BeeSpawnerSystem : SystemBase
{
    public readonly int team = 0;
    public volatile int beesToSpawn = 0;

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);


        var spawnJob = new BeeSpawnJob
        {
            ecb = ecb
        }.Schedule();
    }
}


public partial struct BeeSpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public void Execute(ref BeeSpawnData beeSpawnData)
    {
        var beeEntity = beeSpawnData.beeToSpawn;
        ecb.SetComponent(beeEntity, new Translation
        {
            Value = new float3(0, 0, 0)
        });
        ecb.Instantiate(beeEntity);
    }
}
