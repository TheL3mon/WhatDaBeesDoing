using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class BeeBehaviorSystem : SystemBase
{
    private static ResourceData _resourceData;
    private Random _random;
    private Entity _particlePrefab;

    protected override void OnCreate()
    {
        this.Enabled = true;
        base.OnCreate();
    }

    protected override void OnUpdate()
    {
        var dt = UnityEngine.Time.deltaTime;
        _random = new Random((uint)UnityEngine.Random.Range(1, 500000));
        _particlePrefab = GetSingleton<ParticleData>().particlePrefab;
        var fieldData = GetSingleton<FieldData>();
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        _resourceData = ResourceSystem._resourceData;
        var stackHeights = ResourceSystem._stackHeights;

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var positions = GetComponentDataFromEntity<Translation>(true);
        var resourceStatus = GetComponentDataFromEntity<Resource>(true);
        var beeStatus = GetComponentDataFromEntity<Bee>(true);


        var targetingJob = new TargetingJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            positions = positions,
            resourceData = _resourceData,
            resourceStatus = resourceStatus,
            stackHeights = stackHeights,
            particlePrefab = _particlePrefab,
            fd = fieldData,
            dt = dt,
            ecb = ecb,
            random = _random
        }.Schedule();

        Dependency = targetingJob;

        Dependency.Complete();

        ecb.Playback(World.EntityManager);

        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();
        ecb.Dispose();
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
    public NativeList<int> stackHeights;
    [ReadOnly] public FieldData fd;
    public Entity particlePrefab;
    public float dt;
    //public EntityCommandBuffer ecb;
    public EntityCommandBuffer ecb;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, [EntityInQueryIndex] int entityIndex, ref Bee bee, in Translation position, in BeeData beeData, in AliveTag alive)
    {
        //Debug.Log("How often?");

        bee.isAttacking = false;
        bee.isHoldingResource = false;



        //if (!entityManager.Exists(bee.resourceTarget))
        //    bee.resourceTarget = Entity.Null;

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
                        //Debug.Log("Enemy target pos: " + positions[bee.enemyTarget].Value);
                    }
                }
                else if (bee.team == 1)
                {
                    if (blueTeam.Length > 0)
                    {
                        var randomblue = blueTeam[random.NextInt(blueTeam.Length)];
                        bee.enemyTarget = randomblue;
                        //Debug.Log("Enemy target pos: " + positions[bee.enemyTarget].Value);
                    }
                }
            }
            else
            {
                //Try to taget a random resource
                if (resources.Length > 0)
                {
                    var resource = resources[random.NextInt(resources.Length)];
                    var status = resourceStatus[resource];

                    int index = status.gridX + status.gridY * resourceData.gridCounts.x;

                    if (status.height == stackHeights[index])
                    {
                        bee.resourceTarget = resource;
                        bee.collectingResource = true;
                        //CollectResource(e, entityIndex, bee, beeData);
                    }
                    else
                    {
                        bee.resourceTarget = Entity.Null;
                    }
                }
            }
        }
        else if (bee.enemyTarget != Entity.Null)
        {

            var enemyPos = positions[bee.enemyTarget].Value;

            if (float.IsNaN(enemyPos.x) || float.IsNaN(enemyPos.y) || float.IsNaN(enemyPos.z))
            {
                bee.enemyTarget = Entity.Null;
                return;
            }

            if (float.IsNaN(positions[e].Value.x) || float.IsNaN(positions[e].Value.y) || float.IsNaN(positions[e].Value.z))
            {
                return;
            }

            var delta = positions[bee.enemyTarget].Value - positions[e].Value;
            float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
            if (sqrDist > (beeData.attackDistance * beeData.attackDistance))
            {
                bee.velocity += delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist));
            }
            //else if(sqrDist <= (beeData.attackDistance * beeData.attackDistance))
            else
            {
                //Debug.Log("Delta: " + delta + " positions: " + position.Value + ", enemy pos: " + positions[bee.enemyTarget].Value);
                bee.isAttacking = true;



                bee.velocity += delta * (beeData.attackForce * dt / Mathf.Sqrt(sqrDist));
                if (sqrDist < beeData.hitDistance * beeData.hitDistance)
                {
                    //Spawn particles
                    //velocity change

                    // ParticleSystem._instance.InstantiateBloodParticle(ecb, positions[e].Value, new float3(1, -10, 1));

                    //ParticleSystem.InstantiateBloodParticle(ecb, particlePrefab,positions[e].Value, new float3(1, -10, 1));

                    for (int i = 0; i < 5; i++)
                    {
                        ParticleSystem.InstantiateBloodParticle(entityIndex, ref ecb, particlePrefab, positions[e].Value, bee.velocity, ref random, 2f);
                    }



                    ecb.RemoveComponent<AliveTag>(bee.enemyTarget);
                    ecb.AddComponent(bee.enemyTarget, new DeadTag());
                    bee.enemyTarget = Entity.Null;
                }
            }
        }
        else if (bee.resourceTarget != Entity.Null)
        {
            //CollectResource(e, entityIndex, bee, beeData);
            bee.collectingResource = true;
        }

        if (bee.collectingResource)
        {
            CollectResource(e, ref bee, in beeData);
        }
    }


    void CollectResource(Entity e, ref Bee bee, in BeeData beeData)
    {
        if (resources.Length == 0)
        {
            bee.collectingResource = false;
            bee.resourceTarget = Entity.Null;
            return;
        }

        if (bee.resourceTarget == Entity.Null)
        {
            bee.collectingResource = false;
            return;
        }
;
        var status = resourceStatus[bee.resourceTarget];

        int beeTeam = bee.team;
        int resourceHolderTeam = status.holderTeam;


        if (status.holder == Entity.Null)
        {
            if (status.dead == true)
            {
                bee.resourceTarget = Entity.Null;
                return;
            }
            int resourceIndex = status.gridX + status.gridY * resourceData.gridCounts.x;

            var stackHeight = stackHeights[resourceIndex];
            var resourceHeight = status.height;

            if (stackHeight != resourceHeight)
            {
                {
                    bee.collectingResource = false;
                    bee.resourceTarget = Entity.Null;
                    return;
                }
            }
            else
            {
                var resourcePos = positions[bee.resourceTarget].Value;

                if (float.IsNaN(resourcePos.x) || float.IsNaN(resourcePos.y) || float.IsNaN(resourcePos.z))
                {
                    bee.resourceTarget = Entity.Null;
                    return;
                }

                if (float.IsNaN(positions[e].Value.x) || float.IsNaN(positions[e].Value.y) || float.IsNaN(positions[e].Value.z))
                {
                    return;
                }

                var delta = positions[bee.resourceTarget].Value - positions[e].Value;
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > beeData.grabDistance * beeData.grabDistance)
                {
                    bee.velocity += (delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist)));
                }
                else
                {
                    //Set component
                    var r = new Resource();
                    r.position = status.position;
                    r.height = -1;
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
                    targetPos = new float3(-fd.size.x * .45f + fd.size.x * .9f * 1, 0f, positions[e].Value.z);
                }
                else
                {
                    targetPos = new float3(-fd.size.x * .45f + fd.size.x * .9f * 0, 0f, positions[e].Value.z);
                }

                //var beePos = new Vector3(positions[e].Value.x, positions[e].Value.y, positions[e].Value.z);
                var delta = targetPos - positions[e].Value;
                var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                bee.velocity += (targetPos - positions[e].Value) * (beeData.carryForce * dt / dist);

                if (Mathf.Abs(delta.x) < 10.0f)
                {
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = -1;
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
                    r.position.y -= 0.5f;
                    r.height = -1;
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