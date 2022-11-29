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

public partial class BeeSpawnSystem : SystemBase
{

    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;
    private Entity _resourcePrefab;
    private FieldData _fieldData;
    private ResourceData _resourceData;
    private EntityCommandBuffer _ecb;
    private float3 minPos = new float3(-50, 10, -50);
    private float3 maxPos = new float3(50, 10, 50);
    private float3 zero = new float3(0, 0, 0);
    public Random _random;
    bool buttonpressed = false;
    float timer;

    protected override void OnStartRunning()
    {
        _blueTeamPrefab = GetSingleton<BeePrefabs>().blueBee;
        _yellowTeamPrefab = GetSingleton<BeePrefabs>().yellowBee;
        _resourcePrefab = GetSingleton<BeePrefabs>().resource;
        _resourceData = GetSingleton<ResourceData>();
        _fieldData = GetSingleton<FieldData>();

        //_fieldData.
        //_fieldPrefab = GetSingleton<BeePrefabs>().resource;
        //_ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        _random.InitState(4554);
        _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var initialBlueSpawns = new InitialSpawnJob
        {
            ecb = _ecb,
            team = 0,
            bee = _blueTeamPrefab,
            position = zero
        }.Schedule();

        var initialYellowSpawns = new InitialSpawnJob
        {
            ecb = _ecb,
            team = 1,
            bee = _yellowTeamPrefab,
            position = zero
        }.Schedule();

        var intialResourceSpawns = new SpawnJobResource
        {
            ecb = _ecb,
            resourcePrefab = _resourcePrefab,
            position = zero,
            resourceData = _resourceData,
            fieldData = _fieldData,
            random = _random
        }.Schedule();

        initialBlueSpawns.Complete();
        initialYellowSpawns.Complete();
        intialResourceSpawns.Complete();
        _ecb.Playback(EntityManager);

    }

    protected override void OnUpdate()
    {
        timer += Time.DeltaTime;


        if (Input.GetKeyDown(KeyCode.A))
        {
            buttonpressed = true;
            timer = 0;
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            //_ecb;

            Debug.Log("starting job");
            var blueBeeSpawnJob = new SpawnJob
            {
                ecb = ecb,
                team = 0,
                bee = _blueTeamPrefab,
                position = _random.NextFloat3(minPos, maxPos)
            }.Schedule();

            blueBeeSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            //Enabled = false;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            buttonpressed = true;
            timer = 0;
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            //_ecb;

            var nextRandom = _random.NextFloat3(minPos, maxPos);

            Debug.Log("starting job");
            var YellowBeeSpawnJob = new SpawnJob
            {
                ecb = ecb,
                team = 1,
                bee = _yellowTeamPrefab,
                position = nextRandom
                //beesToSpawn = 10
            }.Schedule();

            YellowBeeSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            //Enabled = false;
        }

        // Spawn resources
        var ecb2 = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        var resourceSpawn = new SpawnJobResource
        {
            ecb = ecb2,
            resourcePrefab = _resourcePrefab,
            resourceData = _resourceData,
            fieldData = _fieldData,
            position = _random.NextFloat3(minPos, maxPos)
    }.Schedule();
        resourceSpawn.Complete();
        ecb2.Playback(EntityManager);
        ecb2.Dispose();

        //_ecb.Playback(EntityManager);


    }
}

[BurstCompile]
public partial struct InitialSpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public int team;
    public Entity bee;
    public float3 position;
    //public int beesToSpawn;

    void Execute(in BeeSpawnData spawnData)
    {
        for (int i = 0; i < spawnData.initialSpawnPerTeam; i++)
        {
            var newBee = ecb.Instantiate(bee);
            
            if (team == 0)
            {
                //position.x += 2;
                var newTranslation = new Translation { 
                    Value = position };

                ecb.SetComponent(newBee, newTranslation);
            } 
            if (team == 1)
            {
                //position.x -= 2;
                var newTranslation = new Translation
                {
                    Value = position
                };

                ecb.SetComponent(newBee, newTranslation);
            }
        }
    }
}

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public int team;
    public Entity bee;
    public float3 position;
    //public int beesToSpawn;

    void Execute(in BeeSpawnData spawnData)
    {
        for (int i = 0; i < spawnData.spawnPerAction; i++)
        {
            var newBee = ecb.Instantiate(bee);

            //position.x += 2 + i;
            var newTranslation = new Translation
            {
                Value = position
            };

            ecb.SetComponent(newBee, newTranslation);

        }
    }
}

[BurstCompile]
public partial struct SpawnJobResource : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resourcePrefab;
    public ResourceData resourceData;
    public float3 position;
    public FieldData fieldData;
    public Random random;
    //public int beesToSpawn;

    //void Execute(in ResourceSpawnData resourceSpawnData)
    //{
    //    Debug.Log("rd spawn");
    //}

    void Execute(Entity spawnEntity, ref ResourceSpawnData resourceSpawnData)
    {
        Debug.Log("Spawned resource");
        var resourceEntity = ecb.Instantiate(resourcePrefab);

        /*
        ecb.AddComponent(resourceEntity, new Translation
        {
            Value = position
        }
        );
        */

        ecb.SetComponent(resourceEntity, new Translation
        {
            Value = calculatePosition()
        }
        );

        //Pass data from spawnData to resourceData or generate data for resource

        //var resource = GetResource();



        //ecb.AddComponent(resourceEntity, resource);

        //this.fieldData = fieldData;

        //ecb.SetComponent(resourceEntity, resourceData);


        ecb.DestroyEntity(spawnEntity); //Should be cached
    }

    float3 calculatePosition()
    {
        //return null;
        var rd = resourceData;
        //var 

        //var size_x = 10.0f;
        //var size_x = 10.0f;

        float3 pos = new float3(rd.minGridPos.x * .25f + random.NextFloat() * fieldData.size.x * .25f, random.NextFloat() * 10f, rd.minGridPos.y + random.NextFloat() * fieldData.size.z);
        //float3 pos = new float3(rd.minGridPos.x * .25f + random.NextFloat() * 1.0f * .25f, random.NextFloat() * 10f, rd.minGridPos.y + random.NextFloat() * 1.0f);
        return pos;
    }

    Resource GetResource()
    {
        var resource = new Resource();
        resource.position = calculatePosition();
        return resource;
    }
}