using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class SpawnSystem : SystemBase
{

    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;
    private Entity _resourcePrefab;
    private BeeSpawnData _beeSpawnData;
    private FieldData _fieldData;
    private ResourceData _resourceData;
    private EntityCommandBuffer _ecb;
    private float3 minPos = new float3(-15, 0, -6);
    private float3 maxPos = new float3(15, 0, 6);
    public Random _random;
    private float spawnTimer = 0f;
    private float spawnRate = 0f;
    bool buttonpressed = false;
    float timer;
    const int resourceSpawnPerFrame = 100;
    public float spawnEnemyBeeTimer = 1.0f;
    public float spawnEnemyBeeCounter = 0.0f;

    protected override void OnCreate()
    {
        Enabled = true;
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        _blueTeamPrefab = GetSingleton<BeePrefabs>().blueBee;
        _yellowTeamPrefab = GetSingleton<BeePrefabs>().yellowBee;
        _resourcePrefab = GetSingleton<BeePrefabs>().resource;
        _resourceData = GetSingleton<ResourceData>();
        _fieldData = GetSingleton<FieldData>();
        _beeSpawnData = GetSingleton<BeeSpawnData>();

        _random.InitState(4554);
        _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        float3 pos = new float3(-1, 0, 0) * (-_fieldData.size.x * .4f + _fieldData.size.x * .8f * 0);
        var color = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
        for (int i = 0; i < _beeSpawnData.initialSpawnPerTeam; i++)
        {
            //var nextRandom = _random.NextFloat3(minPos, maxPos);
            var blueSpawn = new SpawnJob
            {
                ecb = _ecb,
                team = 0,
                beePrefab = _blueTeamPrefab,
                color = color,
                position = pos
            }.Schedule();
            blueSpawn.Complete();
        }

        pos = new float3(-1, 0, 0) * (-_fieldData.size.x * .4f + _fieldData.size.x * .8f * 1);
        color = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
        for (int i = 0; i < _beeSpawnData.initialSpawnPerTeam; i++)
        {
            //var nextRandom = _random.NextFloat3(minPos, maxPos);
            var yellowSpawn = new SpawnJob
            {
                ecb = _ecb,
                team = 0,
                beePrefab = _yellowTeamPrefab,
                color = color,
                position = pos
            }.Schedule();
            yellowSpawn.Complete();
        }


        for (int i = 0; i < _resourceData.startResourceCount; i++)
        {
            pos = new float3(_resourceData.minGridPos.x * .25f + _random.NextFloat() * _fieldData.size.x * .25f, _random.NextFloat() * 10f, _resourceData.minGridPos.y + _random.NextFloat() * _fieldData.size.z);
            var intialResourceSpawns = new SpawnResourceJob
            {
                ecb = _ecb,
                resourcePrefab = _resourcePrefab,
                position = pos,
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


        spawnEnemyBeeCounter += UnityEngine.Time.deltaTime;

        //if (spawnEnemyBeeCounter > spawnEnemyBeeTimer)
        //{
        //    var pos = new float3(-1, 0, 0) * (-_fieldData.size.x * .4f + _fieldData.size.x * .8f * 1);
        //    var color = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);

        //    _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        //    while (spawnEnemyBeeCounter > spawnEnemyBeeTimer)
        //    {
        //        var yellowSpawn = new SpawnJob
        //        {
        //            ecb = _ecb,
        //            team = 1,
        //            beePrefab = _yellowTeamPrefab,
        //            color = color,
        //            position = pos,
        //        }.Schedule();
        //        yellowSpawn.Complete();

        //        spawnEnemyBeeCounter -= spawnEnemyBeeTimer;
        //    }
        //    _ecb.Playback(EntityManager);
        //    _ecb.Dispose();

        //    spawnEnemyBeeCounter = 0.0f;
        //}

        //spawnEnemyBeeCounter += UnityEngine.Time.deltaTime;

        if (spawnEnemyBeeCounter > spawnEnemyBeeTimer)
        {
            _random.InitState(4554);
            _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

            var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
            var blueArr = blueTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
            var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
            var yellowArr = yellowTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
            var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
            var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

            int blueToSpawn = _beeSpawnData.initialSpawnPerTeam - blueArr.Length;
            int yellowToSpawn = _beeSpawnData.initialSpawnPerTeam - yellowArr.Length;
            int resourcesToSpawn = _resourceData.startResourceCount - resourceArr.Length;

            Debug.Log("Spawning should happen, blue: " + blueToSpawn +", yellow: " + ", resources: " + resourcesToSpawn);

            float3 pos = new float3(-1, 0, 0) * (-_fieldData.size.x * .4f + _fieldData.size.x * .8f * 0);
            var color = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
            for (int i = 0; i < blueToSpawn; i++)
            {
                //var nextRandom = _random.NextFloat3(minPos, maxPos);
                var blueSpawn = new SpawnJob
                {
                    ecb = _ecb,
                    team = 0,
                    beePrefab = _blueTeamPrefab,
                    color = color,
                    position = pos
                }.Schedule();
                blueSpawn.Complete();
            }

            pos = new float3(-1, 0, 0) * (-_fieldData.size.x * .4f + _fieldData.size.x * .8f * 1);
            color = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
            for (int i = 0; i < yellowToSpawn; i++)
            {
                //var nextRandom = _random.NextFloat3(minPos, maxPos);
                var yellowSpawn = new SpawnJob
                {
                    ecb = _ecb,
                    team = 0,
                    beePrefab = _yellowTeamPrefab,
                    color = color,
                    position = pos
                }.Schedule();
                yellowSpawn.Complete();
            }


            for (int i = 0; i < resourcesToSpawn; i++)
            {
                pos = new float3(_resourceData.minGridPos.x * .25f + _random.NextFloat() * _fieldData.size.x * .25f, _random.NextFloat() * 10f, _resourceData.minGridPos.y + _random.NextFloat() * _fieldData.size.z);
                var intialResourceSpawns = new SpawnResourceJob
                {
                    ecb = _ecb,
                    resourcePrefab = _resourcePrefab,
                    position = pos,
                    fieldData = _fieldData
                }.Schedule();
                intialResourceSpawns.Complete();
            }


            _ecb.Playback(EntityManager);
            _ecb.Dispose();

            spawnEnemyBeeCounter = 0.0f;
        }
    }

    public static void InstantiateBee(EntityCommandBuffer ecb, float3 position, Entity beePrefab)
    {
        //Debug.Log("Bee spawn should happen!");
        var newBee = ecb.Instantiate(beePrefab);

        var newScale = new NonUniformScale
        {
            Value = new float3(1)
        };

        //position.x += 2 + i;
        var newTranslation = new Translation
        {
            Value = position
        };

        ecb.SetComponent(newBee, newTranslation);
        ecb.AddComponent(newBee, newScale);
    }

    public static void InstantiateBee(int entityIndex, EntityCommandBuffer.ParallelWriter ecb, float3 position, Entity beePrefab)
    {
        //Debug.Log("Bee spawn should happen!");
        var newBee = ecb.Instantiate(entityIndex, beePrefab);

        var newScale = new NonUniformScale
        {
            Value = new float3(1)
        };

        //position.x += 2 + i;
        var newTranslation = new Translation
        {
            Value = position
        };

        ecb.SetComponent(entityIndex, newBee, newTranslation);
        ecb.AddComponent(entityIndex, newBee, newScale);
    }
}

[BurstCompile]
public partial struct SpawnJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public int team;
    public Color color;
    public uint seed;
    public Entity beePrefab;
    public float3 position;
    //public int beesToSpawn;

    void Execute(in BeeSpawnData spawnData)
    {
        for (int i = 0; i < spawnData.spawnPerAction; i++)
        {
            SpawnSystem.InstantiateBee(ecb, position, beePrefab);
        }
    }
}

[BurstCompile]
public partial struct SpawnResourceJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resourcePrefab;
    public float3 position;
    public FieldData fieldData;

    void Execute(in ResourceData resourceData)
    {
        ResourceSystem.InstantiateFallingResource(position, ecb, resourcePrefab);
    }
}