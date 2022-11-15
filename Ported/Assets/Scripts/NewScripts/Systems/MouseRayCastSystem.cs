using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Timeline;
using RaycastHit = Unity.Physics.RaycastHit;

[AlwaysUpdateSystem]
public partial class MouseRayCastSystem : SystemBase
{

    private Camera _mainCamera;
    private BuildPhysicsWorld _buildPhysicsWorld;
    private CollisionWorld _collisionWorld;
    private Entity _marker;
    private Entity _resource;

    protected override void OnStartRunning()
    {
        _marker = GetSingleton<BeePrefabs>().mouseMarker;
        _resource = GetSingleton<BeePrefabs>().resource;
        _mainCamera = Camera.main;
        _marker = EntityManager.Instantiate(_marker);
        EntityManager.SetComponentData(_marker, new Translation { Value = { x = -200, y = -200, z = -200 } });
    }

    protected override void OnCreate()
    {
        _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        _collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        var rayStart = ray.origin;
        var rayend = ray.GetPoint(200f);

        
        if(raycast(rayStart, rayend, out var raycastHit))
        {
            var hitEntity = _buildPhysicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            var hitPos = raycastHit.Position;

            EntityManager.SetComponentData(_marker, new Translation { Value = hitPos });
        } else
        {
            EntityManager.SetComponentData(_marker, new Translation { Value = { x = -200, y = -200, z = -200 } });
        }

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        if(Input.GetMouseButtonUp(0))
        {
            var markerPos = GetComponent<Translation>(_marker);

            var resourceSpawnJob = new SpawnResourceJob
            {
                ecb = ecb,
                resource = _resource,
                position = new float3 { x = markerPos.Value.x, y = markerPos.Value.y + 2, z = markerPos.Value.z }
            }.Schedule();

            resourceSpawnJob.Complete();
            ecb.Playback(EntityManager);
        }
    }


    private bool raycast(float3 rayStart, float3 rayEnd, out RaycastHit raycastHit)
    {
        var raycastInput = new RaycastInput
        {
            Start = rayStart,
            End = rayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = (uint)CollisionLayers.Selection,
                CollidesWith = (uint)CollisionLayers.Ground
            }
        };

        return _collisionWorld.CastRay(raycastInput, out raycastHit);
    }
}


[BurstCompile]
public partial struct SpawnResourceJob1 : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resource;
    public float3 position;

    void Execute(in ResourceData resourseData)
    {
            Debug.Log("in resource job");
            var newResource = ecb.Instantiate(resource);
            var newTranslation = new Translation
            {
                Value = position
            };

            ecb.SetComponent(newResource, newTranslation);
    }
}


[BurstCompile]
public partial struct SpawnResourceJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resource;
    public float3 position;
    //public int beesToSpawn;

    void Execute(in ResourceData spawnData)
    {
        //for (int i = 0; i < spawnData.spawnPerAction; i++)
        //{
            var newBee = ecb.Instantiate(resource);

            var newTranslation = new Translation
            {
                Value = position
            };

            ecb.SetComponent(newBee, newTranslation);

        //}
    }
}
