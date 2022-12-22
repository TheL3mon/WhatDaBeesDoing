//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.CompilerServices;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//public partial class BeeSpawnerSystem : SystemBase
//{
//    public readonly int team = 0;
//    public volatile int beesToSpawn = 0;

//    public int spawnMax = 10;
//    public int spawnCounter = 0;

//    public Mesh BeeMesh;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        Debug.Log("Spawner created");
//    }

//    protected override void OnUpdate()
//    {
//        // Some kind of if statement to check bee spawning.

//        //var ecb = new EntityCommandBuffer(Allocator.TempJob);
//        var ecb = new EntityCommandBuffer(Allocator.TempJob);
//        var pos = new Translation() { Value = new float3(0, 0, 0) };
//        var team = 0;
//        var beeData = new BeeDataOld();

//        var spawnJob = new BeeSpawnJob
//        {
//            ecb = ecb,
//            team = team,
//            beeData = beeData
//        };
        

//        spawnJob.Schedule();

//        //spawnJob.Complete();
//        Dependency.Complete();
//        ecb.Playback(EntityManager);

//        //spawnJob.Complete();
//        ecb.Dispose();
//    }
//}


//public partial struct BeeSpawnJob : IJobEntity
//{
//    public EntityCommandBuffer ecb;
//    public int team;
//    public BeeDataOld beeData;

//    public void Execute(Entity spawnEntity, ref BeeSpawnDataOld beeSpawnData)
//    {
//        ecb.DestroyEntity(spawnEntity);
//        Debug.Log("spawned bee!");
//        //Debug.Log("Entity: " + spawnEntity);
//        // var beeEntity = beeSpawnData.beeToSpawn;
//        //var beeData = new BeeData();
//        beeData.position = beeSpawnData.position;
//        //beeData.position = beeSpawnData.position;
//        beeData.team = team;

//        //ecb.Instantiate(beeEntity);

//        //var beeEntity = ecb.CreateEntity();
//        //ecb.SetComponent(beeEntity);

//        //ecb.SetComponent(beeEntity, beeData);


//        //Debug.Log("Entity: " + spawnEntity);
//        var beeEntity = ecb.CreateEntity();
//        ecb.AddComponent(beeEntity, beeData);
//    }
//}
