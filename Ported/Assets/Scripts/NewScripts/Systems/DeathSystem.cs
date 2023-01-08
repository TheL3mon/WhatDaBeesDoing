using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;

public partial class DeathSystem : SystemBase
{
    private FieldData _fieldData;
    private int cleanUpLimit;

    protected override void OnCreate()
    {
        this.Enabled = true;
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
        cleanUpLimit = 1000;
    }


    protected override void OnUpdate()
    {

        var deltaTime = UnityEngine.Time.deltaTime;

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);


        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(Allocator.Persistent);

        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var deleteQuery = GetEntityQuery(ComponentType.ReadOnly<DeleteTag>());
        var deleteArr = deleteQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);
        var resourceStatus = GetComponentDataFromEntity<Resource>(true);

        var positions = GetComponentDataFromEntity<Translation>(true);

        var clearReferencesJob = new ClearReferencesJob
        {
            beeStatuses = beeStatus
        };

        var deadBeeJob = new DeadBeeJob
        {
            ecb = ecb.AsParallelWriter(),
            deadBees = deadArr,
            resources = resourceArr,
            resourceStatus = resourceStatus,
            positions = positions,
            dt = deltaTime
        };

        var clearResourceReferencesJob = new ClearResourceReferencesJob
        {
            resourceStatuses = resourceStatus,
            beeStatuses = beeStatus,
            ecb = ecb.AsParallelWriter()
        };

        var deadBeeJobHandle = deadBeeJob.Schedule();
        deadBeeJobHandle.Complete();
        var clearReferencesJobHandle = clearReferencesJob.Schedule();
        clearReferencesJobHandle.Complete();
        var clearResourceReferencesJobHandle = clearResourceReferencesJob.Schedule();
        clearResourceReferencesJobHandle.Complete();

        Dependency = clearResourceReferencesJobHandle;

        if (deleteArr.Length > cleanUpLimit)
        {
            var deleteBeeJob = new DeleteDeadBee
            {
                ecb = ecb.AsParallelWriter()
            };
            var deleteBeeJobHandle = deleteBeeJob.ScheduleParallel();
            deleteBeeJobHandle.Complete();
            Dependency = deleteBeeJobHandle;
        }

        Dependency.Complete();

        ecb.Playback(EntityManager);

        deadArr.Dispose();
        resourceArr.Dispose();
        ecb.Dispose();
    }
}


[BurstCompile]
public partial struct DeadBeeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    public float dt;
    [ReadOnly] public NativeArray<Entity> deadBees;
    [ReadOnly] public NativeArray<Entity> resources;
    [ReadOnly] public ComponentDataFromEntity<Resource> resourceStatus;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;

    void Execute(Entity e, [EntityInQueryIndex] int entityIndex, ref Bee bee, in DeadTag tag)
    {
        if (!bee.dead && deadBees.Contains(e))
        {
            bee.dead = true;

            var targetResourceIndex = resources.IndexOf(bee.resourceTarget);

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

            bee.resourceTarget = Entity.Null;
            bee.enemyTarget = Entity.Null;
            ecb.RemoveComponent<YellowTeamTag>(entityIndex, e);
            ecb.RemoveComponent<BlueTeamTag>(entityIndex, e);
        }

        if (bee.dead)
        {
            bee.deathTimer -= dt / 10f;

            if (bee.deathTimer > 0)
            {

                float3 scale = bee.beeScale;
                scale *= Mathf.Sqrt(bee.deathTimer);

                var newScale = new NonUniformScale
                {
                    Value = scale
                };

                //ecb.SetComponent(e, newScale);
                ecb.SetComponent(entityIndex, e, newScale);
            }
            else
            {
                bee.deathTimer = 0f;
                ecb.AddComponent(entityIndex, e, new DeleteTag());
                ecb.RemoveComponent<DeadTag>(entityIndex, e);
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
public partial struct ClearResourceReferencesJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Resource> resourceStatuses;
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Bee> beeStatuses;
    public EntityCommandBuffer.ParallelWriter ecb;

    void Execute(Entity e, [EntityInQueryIndex] int entityIndex, ref Resource resource, in Translation position)
    {

        if (resource.holder != Entity.Null && beeStatuses[resource.holder].dead)
        {
            resource.holder = Entity.Null;

            var r = new Resource();
            r.position = position.Value;
            r.height = resource.height;
            r.holder = Entity.Null;
            r.holderTeam = -1;

            var fallingResourceTag = new FallingResourceTag();
            ecb.SetComponent(entityIndex, e, r);
            ecb.AddComponent(entityIndex, e, fallingResourceTag);
        }
    }
}


[BurstCompile]
public partial struct DeleteDeadBee : IJobEntity
{

    public EntityCommandBuffer.ParallelWriter ecb;

    void Execute(Entity e, [EntityInQueryIndex] int entityIndex, ref Bee bee, in DeleteTag tag)
    {
        ecb.DestroyEntity(entityIndex, e);
    }

}
