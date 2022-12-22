using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ResourceSystem : SystemBase
{
    private Entity _blueTeamPrefab;
    private Entity _yellowTeamPrefab;

    private EntityCommandBuffer _ecb;
    private Entity _resourcePrefab;
    private FieldData _fieldData;
    private ResourceData _resourceData;
    private NativeArray<int> _stackHeights;
    //public int[,] stackHeights;

    private void SetupResource()
    {
        _fieldData = GetSingleton<FieldData>();

        _resourceData = GetSingleton<ResourceData>();

        var rd = _resourceData;

        var gridCounts = new int2((int)(_fieldData.size.x/rd.resourceSize), (int)(_fieldData.size.y/rd.resourceSize));
        var gridSize = new Vector2(_fieldData.size.x/gridCounts.x, _fieldData.size.z/gridCounts.y);
        var minGridPos = new Vector2((gridCounts.x-1f)*-.5f*gridSize.x,(gridCounts.y-1f)*-.5f*gridSize.y);
        //var stackHeights = new int[gridCounts.x,gridCounts.y];

        _resourceData.gridCounts = gridCounts;
        _resourceData.gridSize = gridSize;
        _resourceData.minGridPos = minGridPos;

        int size_x = _resourceData.gridCounts[0];
        int size_y = _resourceData.gridCounts[1];

        _stackHeights = new NativeArray<int>(size_x * size_y, Allocator.Persistent);
        //_resourceData.stackHeights = new NativeArray<int>(_fieldData.size.x * _fieldData.size., Allocator.Persistent); ;

        Debug.Log("stackHeights: " + (size_x * size_y));
    }

    protected override void OnStartRunning()
    {
        SetupResource();
        _blueTeamPrefab = GetSingleton<BeePrefabs>().blueBee;
        _yellowTeamPrefab = GetSingleton<BeePrefabs>().yellowBee;
        _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _stackHeights.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb2 = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var dt = Time.DeltaTime;

        var fallingResourceJob = new FallingResourceJob
        {
            dt = dt,
            ecb = ecb2,
            rd = _resourceData,
            fd = _fieldData,
            stackHeights = _stackHeights
        }.Schedule();
        fallingResourceJob.Complete();

        var spawnBeeFromResourceJob = new SpawnBeeFromResourceJob
        {
            ecb = ecb2,
            rd = _resourceData,
            blueBee = _blueTeamPrefab,
            yellowBee = _yellowTeamPrefab

        }.Schedule();
        spawnBeeFromResourceJob.Complete();



        ecb2.Playback(EntityManager);
        ecb2.Dispose();
    }

    //[BurstCompile]
    public partial struct FallingResourceJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity resourcePrefab;
        public ResourceData rd;
        public FieldData fd;
        public float dt;
        public NativeArray<int> stackHeights;

        void Execute(Entity resourceEntity, ref Resource resource, in FallingResourceTag frt)
        {
            //Apply gravity on resource
            var g = fd.gravity * new float3(0.0f, -1.0f, 0.0f);
            resource.velocity += g * dt;
            resource.position += resource.velocity;

            //Debug.Log("dt: "+ dt);
            //Debug.Log("resource position: " + resource.position);

            //Snap to grid
            var gridIndex = GetGridIndex(resource);
            resource.gridX = gridIndex[0];
            resource.gridY = gridIndex[1];

            int index = resource.gridX + resource.gridY * rd.gridCounts.x;

            //Debug.Log("index: " + index);

            //Debug.Log("gridPos: (" + resource.gridX + ", " + resource.gridY + ") index: " + index);

            //int height = stackHeights[index];
            int height = stackHeights[index];
            //Debug.Log("height: " + height);

            var pos = GetStackPos(resource.gridX, resource.gridY, height);
            var floorY = pos.y;

            if (resource.position.y < floorY)
            {
                stackHeights[index]++;
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
                    //ecb.AddComponent(resourceEntity, spawnBeeTag);
                    //Debug.Log("spawnBeeTag added");
                }
                //Debug.Log("Final pos: (" + resource.position + ") Grid coords: (" + resource.gridX + ", " + resource.gridY + ")");
                //Debug.Log("Grid coords: " + resource.position);

                ecb.RemoveComponent(resourceEntity, typeof(FallingResourceTag));
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


    public partial struct SpawnBeeFromResourceJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public ResourceData rd;
        public Entity blueBee;
        public Entity yellowBee;

        void Execute(Entity resourceEntity, ref Resource resource, in SpawnBeeTag sbt)
        {
            //Debug.Log("Bee spawning should happen here");

            for (int i = 0; i < rd.beesPerResource; i++)
            {
                Entity newBee;

                if (sbt.team == 0)
                    newBee = ecb.Instantiate(yellowBee);
                else
                    newBee = ecb.Instantiate(blueBee);
                   
                var newTranslation = new Translation
                {
                    Value = resource.position
                };

                ecb.SetComponent(newBee, newTranslation);
                ecb.RemoveComponent(resourceEntity, typeof(SpawnBeeTag));

            }
            ecb.DestroyEntity(resourceEntity); //Should be cached
        }
    }
}
