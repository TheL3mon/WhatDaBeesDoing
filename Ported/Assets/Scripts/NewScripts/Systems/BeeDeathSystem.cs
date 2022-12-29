using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

public partial class BeeDeathSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {        
        //var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        //var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);
        _endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);


        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(Allocator.Persistent);

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
                resources = resourceArr,
                resourceStatus = resourceStatus,
                positions = positions
            }.Schedule();

            deadBeeJob.Complete();


            var deleteBeeJob = new deleteDeadBee
            {
                ecb = ecb
            }.Schedule();

            deleteBeeJob.Complete();
            ecb.Playback(EntityManager);
        }

        ecb.Dispose();
        deadArr.Dispose();
        resourceArr.Dispose();

    }
}


[BurstCompile]

public partial struct deadBeeJob : IJobEntity
{

    public EntityCommandBuffer ecb;
    public NativeArray<Entity> deadBees;
    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;
    public ComponentDataFromEntity<Translation> positions;

    void Execute(Entity e, ref Bee bee)
    {

        var targetResourceIndex = resources.IndexOf(bee.resourceTarget);

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

                var fallingResourceTag = new FallingResourceTag();

                ecb.SetComponent(resource, r);
                ecb.AddComponent(resource, fallingResourceTag);
            }

        }

        if (deadBees.Contains(e))
        {
            bee.dead = true;
        }

        if (deadBees.Contains(bee.enemyTarget))
        {
            bee.enemyTarget = Entity.Null;
        }

    }

}



[BurstCompile]

public partial struct deleteDeadBee : IJobEntity
{

    public EntityCommandBuffer ecb;

    void Execute(Entity e, ref Bee bee, in DeadTag tag)
    {
        //bee.dead = true;
        // Debug.Log("Deleted bee");

        ecb.DestroyEntity(e);
    }

}
