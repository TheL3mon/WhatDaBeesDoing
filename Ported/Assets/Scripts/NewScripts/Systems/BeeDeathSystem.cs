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
    private FieldData _fieldData;

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
    }


    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;


        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(Allocator.Persistent);

        if (deadArr.Length > 0)
        {
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

            var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
            var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

            var beeStatus = GetComponentDataFromEntity<Bee>(true);
            var resourceStatus = GetComponentDataFromEntity<Resource>(true);
            var positions = GetComponentDataFromEntity<Translation>(true);

            var deadBeeJob = new DeadBeeJob
            {
                ecb = ecb.AsParallelWriter(),
                deadBees = deadArr,
                resources = resourceArr,
                resourceStatus = resourceStatus,
                positions = positions,
            }.ScheduleParallel();

            var clearReferencesJob = new ClearReferencesJob
            {
                beeStatuses = beeStatus

            }.ScheduleParallel(deadBeeJob);

            var deleteBeeJob = new DeleteDeadBee
            {
                ecb = ecb.AsParallelWriter(),
                dt = deltaTime,
                positions = positions
            }.ScheduleParallel(clearReferencesJob);

            deleteBeeJob.Complete();
            ecb.Playback(EntityManager);

            resourceArr.Dispose();
            ecb.Dispose();
        }
        deadArr.Dispose();
    }
}


[BurstCompile]
public partial struct DeadBeeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public NativeArray<Entity> deadBees;
    [ReadOnly] public NativeArray<Entity> resources;
    [ReadOnly] public ComponentDataFromEntity<Resource> resourceStatus;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;

    void Execute(Entity e, ref Bee bee, in DeadTag tag)
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
                    //Debug.Log("Resource holder dead.");
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = -1;
                    r.holderTeam = -1;
                    r.holder = Entity.Null;

                    var fallingResourceTag = new FallingResourceTag();

                    ecb.SetComponent(resource.Index, resource, r);
                    ecb.AddComponent(resource.Index, resource, fallingResourceTag);
                }
            }
        }
    }
}

[BurstCompile]
public partial struct ClearReferencesJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Bee> beeStatuses;

    void Execute(Entity e, ref Bee bee, in AliveTag tag)
    {

        if (bee.enemyTarget != Entity.Null && beeStatuses[bee.enemyTarget].dead)
        {
            bee.enemyTarget = Entity.Null;
        }
    }

}


[BurstCompile]
public partial struct DeleteDeadBee : IJobEntity
{

    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;
    public float dt;

    void Execute(Entity e, ref Bee bee, in DeadTag tag)
    {
        bee.deathTimer -= dt / 10f;
        float3 scale = bee.beeScale;
        scale *= Mathf.Sqrt(bee.deathTimer);

        var newScale = new NonUniformScale
        {
            Value = scale
        };

        //ecb.SetComponent(e, newScale);
        ecb.SetComponent(e.Index, e, newScale);


        if (bee.deathTimer < 0)
        {
            //ecb.DestroyEntity(e);
            ecb.DestroyEntity(e.Index, e);
        }
    }

}
