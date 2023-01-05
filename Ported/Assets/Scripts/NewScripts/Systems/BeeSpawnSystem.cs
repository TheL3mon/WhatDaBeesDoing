using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using static UnityEngine.Rendering.DebugUI;
using System;
using UnityEngine.UIElements;
using Unity.Collections;
using Unity.Jobs;
using static UnityEngine.ParticleSystem;

public partial class BeeSpawnSystem : SystemBase
{

    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;
    private Entity _resourcePrefab;
    private FieldData _fieldData;
    private ResourceData _resourceData;
    private EntityCommandBuffer _ecb;
    private float3 minPos = new float3(-30, 0, -13);
    private float3 maxPos = new float3(30, 0, 13);
    private float3 zero = new float3(0, 0, 0);
    public Random _random;
    bool buttonpressed = false;
    float timer;
    const int resourceSpawnPerFrame = 100;

    protected override void OnStartRunning()
    {
        _blueTeamPrefab = GetSingleton<BeePrefabs>().blueBee;
        _yellowTeamPrefab = GetSingleton<BeePrefabs>().yellowBee;
        _resourcePrefab = GetSingleton<BeePrefabs>().resource;
        _resourceData = GetSingleton<ResourceData>();
        _fieldData = GetSingleton<FieldData>();
        var beeSpawnData = GetSingleton<BeeSpawnData>();

        _random.InitState(4554);
        _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        for (int i = 0; i < beeSpawnData.initialSpawnPerTeam; i++)
        {
            var nextRandom = _random.NextFloat3(minPos, maxPos);
            var blueSpawn = new SpawnJob
            {
                ecb = _ecb,
                team = 0,
                bee = _blueTeamPrefab,
                position = nextRandom,
                seed = _random.NextUInt()
            }.Schedule();
            blueSpawn.Complete();
        }

        for (int i = 0; i < beeSpawnData.initialSpawnPerTeam; i++)
        {
            var nextRandom = _random.NextFloat3(minPos, maxPos);
            var yellowSpawn = new SpawnJob
            {
                ecb = _ecb,
                team = 1,
                bee = _yellowTeamPrefab,
                position = nextRandom,
                seed = _random.NextUInt()
            }.Schedule();
            yellowSpawn.Complete();
        }

        for (int i = 0; i < resourceSpawnPerFrame; i++)
        {
            var nextRandom = _random.NextFloat3(minPos, maxPos);
            var intialResourceSpawns = new SpawnJobResource
            {
                ecb = _ecb,
                resourcePrefab = _resourcePrefab,
                position = nextRandom,
                fieldData = _fieldData
            }.Schedule();
            intialResourceSpawns.Complete();
        }

        _ecb.Playback(EntityManager);
        _ecb.Dispose();

    }

    protected override void OnUpdate()
    {
        timer += Time.DeltaTime;


        if (Input.GetKeyDown(KeyCode.A))
        {
            buttonpressed = true;
            timer = 0;
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

            var blueBeeSpawnJob = new SpawnJob
            {
                ecb = ecb,
                team = 0,
                bee = _blueTeamPrefab,
                position = _random.NextFloat3(minPos, maxPos),
                seed = _random.NextUInt()
            }.Schedule();

            blueBeeSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            buttonpressed = true;
            timer = 0;
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

            var nextRandom = _random.NextFloat3(minPos, maxPos);

            var YellowBeeSpawnJob = new SpawnJob
            {
                ecb = ecb,
                team = 1,
                bee = _yellowTeamPrefab,
                position = nextRandom,
                seed = _random.NextUInt()
            }.Schedule();

            YellowBeeSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();

            for (int i = 0; i < resourceSpawnPerFrame; i++)
            {
                var position = _random.NextFloat3(minPos, maxPos);
                var resourceSpawn = new SpawnJobResource
                {
                    ecb = ecb,
                    resourcePrefab = _resourcePrefab,
                    fieldData = _fieldData,
                    position = position
                }.Schedule();
                resourceSpawn.Complete();

            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public int team;
    public uint seed;
    public Entity bee;
    public float3 position;
    //public int beesToSpawn;

    void Execute(in BeeSpawnData spawnData)
    {
        for (int i = 0; i < spawnData.spawnPerAction; i++)
        {
            //Debug.Log("Bee spawn should happen!");
            var newBee = ecb.Instantiate(bee);

            var newScale = new NonUniformScale
            {
                Value = new float3(1)
            };

            //position.x += 2 + i;
            var newTranslation = new Translation
            {
                Value = position
            };

            //ecb.SetComponent(newBee, new Bee
            //{
            //    seed = seed
            //});

            // ParticleSystem._instance.InstantiateSpawnFlashParticle(ecb, position, new float3(1, -10, 1));

            ecb.SetComponent(newBee, newTranslation);
            ecb.AddComponent(newBee, newScale);
        }
    }
}

[BurstCompile]
public partial struct SpawnJobResource : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resourcePrefab;
    public float3 position;
    public FieldData fieldData;

    void Execute(in ResourceData resourceData)
    {
        //Debug.Log("Spawned resource");
        var resourceEntity = ecb.Instantiate(resourcePrefab);

        ecb.SetComponent(resourceEntity, new Translation
        {
            Value = position
        }
        );

        //Pass data from spawnData to resourceData or generate data for resource

        var resource = new Resource();
        resource.position = position;
        resource.height = -1;
        resource.holderTeam = -1;

        var fallingResourceTag = new FallingResourceTag();

        ecb.AddComponent(resourceEntity, resource);
        ecb.AddComponent(resourceEntity, fallingResourceTag);
    }
}