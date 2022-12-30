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
using UnityEngine.UIElements;
using Plane = UnityEngine.Plane;
using Ray = UnityEngine.Ray;
using RaycastHit = Unity.Physics.RaycastHit;

[AlwaysUpdateSystem]
public partial class MouseRayCastSystem : SystemBase
{

    private Camera _mainCamera;
    private BuildPhysicsWorld _buildPhysicsWorld;
    private CollisionWorld _collisionWorld;
    private Entity _marker;
    //private Entity _resource;
    private Entity _resourcePrefab;
    private ResourceData _resourceData;
    private FieldData _fieldData;
    private float3 fieldSize;

    protected override void OnStartRunning()
    {
        _marker = GetSingleton<BeePrefabs>().mouseMarker;
        //_resource = GetSingleton<BeePrefabs>().resource;
        _resourcePrefab = GetSingleton<BeePrefabs>().resource;
        _resourceData = GetSingleton<ResourceData>();
        _fieldData = GetSingleton<FieldData>();
        _mainCamera = Camera.main;
        _marker = EntityManager.Instantiate(_marker);
        fieldSize = GetSingleton<FieldData>().size;
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

        /*
        if(raycast(rayStart, rayend, out var raycastHit))
        {
            var hitEntity = _buildPhysicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            var hitPos = raycastHit.Position;

            EntityManager.SetComponentData(_marker, new Translation { Value = hitPos });
        } else
        {
            EntityManager.SetComponentData(_marker, new Translation { Value = { x = -200, y = -200, z = -200 } });
        }*/
        EntityManager.SetComponentData(_marker, new Translation { Value = { x = -200, y = -200, z = -200 } });

        for (int i = 0; i < 3; i++)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                Vector3 wallCenter = new Vector3();
                wallCenter[i] = fieldSize[i] * .5f * j;
                //Debug.Log(wallCenter);
                Plane plane = new Plane(-wallCenter, wallCenter);
                float hitDistance;
                if (Vector3.Dot(plane.normal, ray.direction) < 0f)
                {
                    if (plane.Raycast(ray, out hitDistance))
                    {
                        Vector3 hitPoint = ray.GetPoint(hitDistance);
                        bool insidefield = true;
                        for (int k = 0; k < 3; k++)
                        {
                            if (Mathf.Abs(hitPoint[k]) > fieldSize[k] * .5f + .01f)
                            {
                                insidefield = false;
                                break;
                            }
                        }
                        if (insidefield)
                        {
                            EntityManager.SetComponentData(_marker, new Translation { Value = hitPoint });
                            break;
                        }
                    }
                }
            }
        }

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        if (Input.GetMouseButtonUp(0))
        {
            var markerPos = GetComponent<Translation>(_marker);
            Debug.Log("Spawn position = " + markerPos.Value);
            if (markerPos.Value.z == -15)
            {
                markerPos.Value.z += 1;
            }
            else if (markerPos.Value.z == 15)
            {
                markerPos.Value.z -= 1;
            }
            if (markerPos.Value.y == -10)
            {
                markerPos.Value.y += 1;
            }
            else if (markerPos.Value.y == 10)
            {
                markerPos.Value.y -= 1;
            }

            var resourceSpawn = new SpawnJobResource
            {
                ecb = ecb,
                resourcePrefab = _resourcePrefab,
                fieldData = _fieldData,
                position = new float3 { x = markerPos.Value.x, y = markerPos.Value.y, z = markerPos.Value.z }
            }.Schedule();
            resourceSpawn.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
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