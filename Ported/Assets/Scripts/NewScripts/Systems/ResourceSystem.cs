﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ResourceSystem : SystemBase
{
    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;
    private Entity _particlePrefab;

    private Entity _resourcePrefab;

    private FieldData _fieldData;
    public static ResourceData _resourceData;

    public static NativeList<int> _stackHeights;

    protected override void OnCreate()
    {
        this.Enabled = true;

        base.OnCreate();
    }

    private void SetupResource()
    {
        _fieldData = GetSingleton<FieldData>();
        _resourceData = GetSingleton<ResourceData>();

        var rd = _resourceData;

        var gridCounts = new int2((int)(_fieldData.size.x / rd.resourceSize), (int)(_fieldData.size.y / rd.resourceSize));
        var gridSize = new Vector2(_fieldData.size.x / gridCounts.x, _fieldData.size.z / gridCounts.y);
        var minGridPos = new Vector2((gridCounts.x - 1f) * -.5f * gridSize.x, (gridCounts.y - 1f) * -.5f * gridSize.y);

        _resourceData.gridCounts = gridCounts;
        _resourceData.gridSize = gridSize;
        _resourceData.minGridPos = minGridPos;

        int size_x = _resourceData.gridCounts[0];
        int size_y = _resourceData.gridCounts[1];

        _stackHeights = new NativeList<int>(size_x * size_y, Allocator.Persistent);

        for (int i = 0; i < (size_x * size_y); i++)
        {
            _stackHeights.Add(0);
        }
    }

    protected override void OnStartRunning()
    {
        SetupResource();
        _blueTeamPrefab = GetSingleton<BeePrefabs>().blueBee;
        _yellowTeamPrefab = GetSingleton<BeePrefabs>().yellowBee;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _stackHeights.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        _particlePrefab = GetSingleton<ParticleData>().particlePrefab;

        var dt = Time.DeltaTime;

        var fallingResourceJob = new FallingResourceJob
        {
            dt = dt,
            ecb = ecb,
            rd = _resourceData,
            fd = _fieldData,
            stackHeights = _stackHeights
        };

        var spawnBeeFromResourceJob = new SpawnBeeFromResourceJob
        {
            ecb = ecb.AsParallelWriter(),
            rd = _resourceData,
            blueBee = _blueTeamPrefab,
            yellowBee = _yellowTeamPrefab,
            particlePrefab = _particlePrefab
        };

        var jobFallingResource = fallingResourceJob.Schedule();
        var jobHandleSpawn = spawnBeeFromResourceJob.Schedule(jobFallingResource);
        Dependency = jobHandleSpawn;

        Dependency.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct FallingResourceJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity resourcePrefab;
        public ResourceData rd;
        public FieldData fd;
        public float dt;
        public NativeList<int> stackHeights;
        //public NativeParallelHashMap<int, Resource> resourceHashMap;

        void Execute(Entity resourceEntity, ref Resource resource, in FallingResourceTag frt)
        {
            //Apply gravity on resource
            var g = fd.gravity * new float3(0.0f, -1.0f, 0.0f);
            resource.velocity += g * dt;
            resource.position += resource.velocity;

            //Snap to grid
            var gridIndex = GetGridIndex(resource);
            resource.gridX = gridIndex[0];
            resource.gridY = gridIndex[1];

            int index = resource.gridX + resource.gridY * rd.gridCounts.x;

            //Debug.Log("gridPos1: (" + resource.gridX + ", " + resource.gridY + ") index: " + index);
            int height = stackHeights[index];

            var pos = GetStackPos(resource.gridX, resource.gridY, height);
            var floorY = pos.y;

            if (resource.position.y < floorY)
            {
                stackHeights[index]++;
                resource.height = stackHeights[index];
                resource.position = pos;
                resource.velocity = float3.zero;

                if (Mathf.Abs(resource.position.x) > fd.size.x * .3f)
                {
                    int team = 0;
                    if (resource.position.x > 0f)
                    {
                        team = 1;
                    }
                    var spawnBeeTag = new SpawnBeeTag();
                    spawnBeeTag.team = team;
                    ecb.AddComponent(resourceEntity, spawnBeeTag);
                    //Debug.Log("spawnBeeTag added");
                }
                //Debug.Log("Final pos: (" + resource.position + ") Grid coords: (" + resource.gridX + ", " + resource.gridY + ")");
                //Debug.Log("Grid coords: " + resource.position);

                ecb.RemoveComponent<FallingResourceTag>(resourceEntity);
            }

            ecb.SetComponent(resourceEntity, new Translation
            {
                Value = resource.position
            });
        }

        int2 GetGridIndex(Resource resource)
        {
            var pos = resource.position;
            var minGridPos = rd.minGridPos;
            var gridSize = rd.gridSize;
            var gridCounts = rd.gridCounts;

            var gridX = Mathf.FloorToInt((pos.x - minGridPos.x + gridSize.x * .5f) / gridSize.x);


            var gridY = Mathf.FloorToInt((pos.z - minGridPos.y + gridSize.y * .5f) / gridSize.y);

            gridX = Mathf.Clamp(gridX, 0, gridCounts.x - 1);
            gridY = Mathf.Clamp(gridY, 0, gridCounts.y - 1);

            return new int2(gridX, gridY);
        }

        float3 GetStackPos(int x, int y, int height)
        {
            var minGridPos = rd.minGridPos;
            var gridSize = rd.gridSize;
            return new float3(minGridPos.x + x * gridSize.x, -fd.size.y * .5f + (height + .5f) * rd.resourceSize, minGridPos.y + y * gridSize.y);
        }
    }


    [BurstCompile]
    public partial struct SpawnBeeFromResourceJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public ResourceData rd;
        [ReadOnly] public Entity blueBee;
        [ReadOnly] public Entity yellowBee;
        [ReadOnly] public Entity particlePrefab;

        void Execute(Entity resourceEntity, [EntityInQueryIndex] int entityIndex, ref Resource resource, in Translation position, in SpawnBeeTag sbt)
        {

            for (int i = 0; i < rd.beesPerResource; i++)
            {
                Entity newBee;

                if (sbt.team == 0)
                    newBee = ecb.Instantiate(entityIndex, yellowBee);
                else
                    newBee = ecb.Instantiate(entityIndex, blueBee);

                var newTranslation = new Translation
                {
                    Value = resource.position
                };

                var newScale = new NonUniformScale
                {
                    Value = new float3(1)
                };

                //ParticleSystem.InstantiateBloodParticle(entityIndex, ecb, particlePrefab, positions[e].Value, new float3(1, -10, 1));
                ParticleSystem.InstantiateSpawnFlashParticle(entityIndex, ecb, particlePrefab, position.Value, new float3(1, -10, 1));

                ecb.SetComponent(entityIndex, newBee, newTranslation);
                //ecb.SetComponent(newBee, new Bee { seed = seed });
                ecb.AddComponent(entityIndex, newBee, newScale);
                ecb.RemoveComponent<SpawnBeeTag>(entityIndex, resourceEntity);

            }
            ecb.DestroyEntity(entityIndex, resourceEntity); //Should be cached
        }
    }
}
