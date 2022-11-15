using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class BeeSpawnSystem : SystemBase
{

    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;
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

        initialBlueSpawns.Complete();
        initialYellowSpawns.Complete();
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