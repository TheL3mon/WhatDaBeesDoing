using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities.UniversalDelegates;
using Unity.Physics;

public partial class BeeDeathSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;
    public float deltaTime;
    private FieldData _fieldData;

    protected override void OnCreate()
    {
        //var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        //var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);
        _endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
    }


    protected override void OnUpdate()
    {
        deltaTime = Time.DeltaTime;

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);


        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(Allocator.Persistent);


        var beesQuery = GetEntityQuery(ComponentType.ReadWrite<Bee>());
        var beesArr = beesQuery.ToEntityArray(Allocator.Persistent);

        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);
        var resourceStatus = GetComponentDataFromEntity<Resource>(false);

        var positions = GetComponentDataFromEntity<Translation>(false);

        if (deadArr.Length > 0)
        {

            var deadBeeJob = new deadBeeJob
            {
                ecb = ecb,
                deadBees = deadArr,
                bees = beesArr,
                resources = resourceArr,
                resourceStatus = resourceStatus,
                beeStatuses = beeStatus,
                positions = positions,
                fd = _fieldData
            }.Schedule();

            deadBeeJob.Complete();

            var deleteBeeJob = new deleteDeadBee
            {
                ecb = ecb,
                dt = deltaTime,
                positions = positions
            }.Schedule();

            deleteBeeJob.Complete();
            ecb.Playback(EntityManager);
        }

        ecb.Dispose();
        deadArr.Dispose();
        beesArr.Dispose();
        resourceArr.Dispose();

    }
}


[BurstCompile]

public partial struct deadBeeJob : IJobEntity
{

    public EntityCommandBuffer ecb;
    public NativeArray<Entity> deadBees;
    public NativeArray<Entity> resources;
    public NativeArray<Entity> bees;
    public ComponentDataFromEntity<Resource> resourceStatus;
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Bee> beeStatuses;
    public ComponentDataFromEntity<Translation> positions;
    public FieldData fd;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity)
    {
        if (!bee.dead && deadBees.Contains(e))
        {
            bee.dead = true;

            var targetResourceIndex = resources.IndexOf(bee.resourceTarget);
            //Debug.Log("Bee dead.");

            if (targetResourceIndex != -1)
            {
                var resource = resources[targetResourceIndex];
                var status = resourceStatus[resource];

                var holder = status.holder;

                if (holder == e)
                {
                    Debug.Log("Resource holder dead.");
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = -1;
                    r.holderTeam = -1;
                    r.holder = Entity.Null;

                    var fallingResourceTag = new FallingResourceTag();

                    ecb.SetComponent(resource, r);
                    ecb.AddComponent(resource, fallingResourceTag);
                }
            }
        }

        if (bee.dead)
        {
            velocity.Linear = fd.gravity * new float3(0, -9.8f, 0);
        }

        if (bee.enemyTarget != Entity.Null && beeStatuses[bee.enemyTarget].dead)
        {
            bee.enemyTarget = Entity.Null;
        }

        //if (deadBees.Contains(bee.enemyTarget))
        //{
        //    bee.enemyTarget = Entity.Null;
        //}

    }

}



[BurstCompile]

public partial struct deleteDeadBee : IJobEntity
{

    public EntityCommandBuffer ecb;
    public ComponentDataFromEntity<Translation> positions;
    public float dt;

    void Execute(Entity e, ref Bee bee, in DeadTag tag)
    {
        //bee.dead = true;
        // Debug.Log("Deleted bee");
        bee.deathTimer -= dt / 10f;
        float3 scale = bee.beeScale;
        scale *= Mathf.Sqrt(bee.deathTimer);

        var newScale = new NonUniformScale
        {
            Value = scale
        };

        ecb.SetComponent(e, newScale);


        if (bee.deathTimer < 0)
        {
            ecb.DestroyEntity(e);
        }
    }

}
