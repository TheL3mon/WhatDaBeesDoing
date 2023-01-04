using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class BeeBehaviorSystem : SystemBase
{
    private static ResourceData _resourceData;
    private Random _random;
    protected override void OnUpdate()
    {
        _random = new Random((uint)UnityEngine.Random.Range(1, 500000));
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        _resourceData = ResourceSystem._resourceData;
        var stackHeights = ResourceSystem._stackHeights;

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(Allocator.TempJob);

        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(Allocator.TempJob);

        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(Allocator.TempJob);

        var positions = GetComponentDataFromEntity<Translation>(true);
        var resourceStatus = GetComponentDataFromEntity<Resource>(true);

        var collectingJob = new CollectResourceJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            resourceStatus = resourceStatus,
            positions = positions,
            stackHeights = stackHeights,
            resourceData = _resourceData,
            dt = Time.DeltaTime,
            ecb = ecb,
            random = _random
        }.Schedule();

        var targetingJob = new TargetingJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            positions = positions,
            resourceData = _resourceData,
            resourceStatus = resourceStatus,
            stackHeights = stackHeights,
            dt = Time.DeltaTime,
            ecb = ecb.AsParallelWriter(),
            random = _random
        }.ScheduleParallel(collectingJob);

        targetingJob.Complete();

        ecb.Playback(World.EntityManager);

        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();
        ecb.Dispose();
    }
}


[BurstCompile]
public partial struct CollectResourceJob : IJobEntity
{

    [ReadOnly] public NativeArray<Entity> resources;
    [ReadOnly] public ComponentDataFromEntity<Resource> resourceStatus;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;
    [ReadOnly] public ResourceData resourceData;
    [ReadOnly] public NativeArray<Entity> blueTeam;
    [ReadOnly] public NativeArray<Entity> yellowTeam;
    public NativeList<int> stackHeights;
    public float dt;

    public EntityCommandBuffer ecb;


    public Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in CollectingTag tag, in BeeData beeData)
    {
        if (resources.Length == 0) 
            return; 
        var target = bee.resourceTarget;
        var index = resources.IndexOf(target);

        if (index == -1)
            return;
        var resource = resources[index];
        var status = resourceStatus[resource];
        bee.resourceTarget = resource;

        int beeTeam = bee.team;
        int resourceHolderTeam = status.holderTeam;


        if (status.holder == Entity.Null)
        {
            if (status.dead == true)
            {
                Debug.Log("resource is dead");
                bee.resourceTarget = Entity.Null;
                return;
            }
            int resourceIndex = status.gridX + status.gridY * resourceData.gridCounts.x;
            var stackHeight = stackHeights[resourceIndex];
            var resourceHeight = status.height;

            if (stackHeight != resourceHeight)
                bee.resourceTarget = Entity.Null;
            else
            {
                var delta = positions[resource].Value - positions[e].Value;
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > beeData.grabDistance * beeData.grabDistance)
                {
                    //Debug.Log("Moving towards resource");
                    velocity.Linear += (delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist)));
                }
                else
                {
                    //Set component
                    var r = new Resource();
                    r.position = status.position;
                    r.height = status.height;
                    r.holder = e;
                    r.holderTeam = bee.team;

                    ecb.SetComponent(bee.resourceTarget, r);

                    //reduce stack of resource
                    stackHeights[resourceIndex] -= 1;
                }
            }
        }
        else
        {
            if (status.holder == e)
            {
                float3 targetPos;

                if (bee.team == 0)
                {
                    targetPos = new float3(50, 0, 0);
                }
                else
                {
                    targetPos = new float3(-50, 0, 0);
                }

                //var beePos = new Vector3(positions[e].Value.x, positions[e].Value.y, positions[e].Value.z);
                var delta = targetPos - positions[e].Value;
                var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                velocity.Linear += (targetPos - positions[e].Value) * (beeData.carryForce * dt / dist);

                if (Mathf.Abs(delta.x) < 10.0f)
                {
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = status.height;
                    r.holder = Entity.Null;
                    r.holderTeam = -1;

                    var fallingResourceTag = new FallingResourceTag();
                    ecb.SetComponent(bee.resourceTarget, r);
                    ecb.AddComponent(bee.resourceTarget, fallingResourceTag);
                }
                else
                {
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = status.height;
                    r.holder = e;
                    r.holderTeam = -1;

                    ecb.SetComponent(bee.resourceTarget, r);

                    ecb.SetComponent(bee.resourceTarget, new Translation
                    {
                        Value = r.position
                    });
                }
            }
            else if (resourceHolderTeam != -1 && beeTeam != resourceHolderTeam) bee.enemyTarget = status.holder;
            else if (beeTeam == resourceHolderTeam) bee.resourceTarget = Entity.Null;
        }
    }
}

[BurstCompile]
public partial struct TargetingJob : IJobEntity
{
    [ReadOnly] public NativeArray<Entity> blueTeam;
    [ReadOnly] public NativeArray<Entity> yellowTeam;
    [ReadOnly] public NativeArray<Entity> resources;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;
    [ReadOnly] public ComponentDataFromEntity<Resource> resourceStatus;
    [ReadOnly] public ResourceData resourceData;
    [ReadOnly] public NativeList<int> stackHeights;
    public float dt;
    public EntityCommandBuffer.ParallelWriter ecb;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in BeeData beeData, in AliveTag alive)
    {
        if (bee.enemyTarget == Entity.Null && bee.resourceTarget == Entity.Null)
        {
            if (random.NextFloat(1.0f) < beeData.aggression)
            {
                if (bee.team == 0)
                {
                    if (yellowTeam.Length > 0)
                    {
                        var randomyellow = yellowTeam[random.NextInt(yellowTeam.Length)];
                        bee.enemyTarget = randomyellow;
                    }
                }
                else if (bee.team == 1)
                {
                    if (blueTeam.Length > 0)
                    {
                        var randomblue = blueTeam[random.NextInt(blueTeam.Length)];
                        bee.enemyTarget = randomblue;
                    }
                }
            }
            else
            {
                //Try to taget a random resource

                var resource = resources[random.NextInt(resources.Length)];
                var status = resourceStatus[resource];

                int index = status.gridX + status.gridY * resourceData.gridCounts.x;

                if (status.height == stackHeights[index])
                {
                    bee.resourceTarget = resource;
                    ecb.AddComponent(e.Index, e, new CollectingTag());
                }
                else
                {
                    bee.resourceTarget = Entity.Null;
                }
            }
        }
        else if (bee.enemyTarget != Entity.Null)
        {
            var delta = positions[bee.enemyTarget].Value - positions[e].Value;
            float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
            if (sqrDist > beeData.attackDistance * beeData.attackDistance)
            {
                velocity.Linear += delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist));
            }
            else
            {
                bee.isAttacking = true;
                velocity.Linear += delta * (beeData.attackForce * dt / Mathf.Sqrt(sqrDist));
                if (sqrDist < beeData.hitDistance * beeData.hitDistance)
                {
                    //Spawn particles
                    //velocity change

                    // ParticleSystem._instance.InstantiateBloodParticle(ecb, positions[e].Value, new float3(1, -10, 1));

                    ecb.RemoveComponent<AliveTag>(bee.enemyTarget.Index, bee.enemyTarget);
                    ecb.AddComponent(bee.enemyTarget.Index, bee.enemyTarget, new DeadTag());
                    bee.enemyTarget = Entity.Null;
                }
            }
        }
        else if (bee.resourceTarget != Entity.Null)
        {
            //Debug.Log("Bee has a resource target");    
            ecb.AddComponent(e.Index, e, new CollectingTag());
        }
    }


    void CollectResource()
    { 
    
    }

}