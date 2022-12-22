using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public partial class BeeDeathSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        _endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);


        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(Allocator.Persistent);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);

        if(deadArr.Length > 0)
        {

            var deadBeeJob = new deadBeeJob
            {
                ecb = ecb,
                deadBees = deadArr
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

    }
}


[BurstCompile]

public partial struct deadBeeJob : IJobEntity
{

    public EntityCommandBuffer ecb;
    public NativeArray<Entity> deadBees;

    void Execute(Entity e, ref Bee bee)
    {
        if(deadBees.Contains(e))
        {
            bee.dead = true;
        }
        if(deadBees.Contains(bee.enemyTarget))
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
