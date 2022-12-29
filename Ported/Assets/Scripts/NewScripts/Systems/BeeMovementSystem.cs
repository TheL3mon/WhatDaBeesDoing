using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using Unity.Collections.LowLevel.Unsafe;
using System;

public partial class BeeMovementSystem : SystemBase
{
    public static bool testing_InvincibleBees = true;
    private EntityQuery _blueTeamQuery;
    private EntityQuery _yellowTeamQuery;
    private Random _random;
    private FieldData _fieldData;

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
    }


    protected override void OnUpdate()
    {
        _random.InitState((uint)UnityEngine.Random.Range(0, 100000));
        var deltaTime = Time.DeltaTime;

        //var allBlueBees = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        //var nativearr = allBlueBees.ToEntityArray(Allocator.TempJob);

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(Allocator.Persistent);
        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(Allocator.Persistent);
        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);
        var positions = GetComponentDataFromEntity<Translation>(false);
        var resourceStatus = GetComponentDataFromEntity<Resource>(false);



        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var testingJob = new targetingJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            resourceStatus = resourceStatus,
            status = beeStatus,
            positions = positions,
            dt = Time.DeltaTime,
            manager = EntityManager,
            ecb = ecb,
            random = _random
        }.Schedule();
        testingJob.Complete();

        var containJob = new containmentJob
        {
            field = _fieldData
        }.Schedule();

        var movementJob = new MoveBeeJob
        {
            ecb = ecb,
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            positions = positions,
            dt = Time.DeltaTime,
            random = _random,
            us = (float3)UnityEngine.Random.insideUnitSphere
        }.Schedule();


        //Dynamic buffers is an option
        containJob.Complete();
        movementJob.Complete();
        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();

        ecb.Playback(World.EntityManager);

        //Random rand = new Random();
        //rand.InitState((uint)UnityEngine.Random.Range(0, 100000));


        //var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);

        //Entities
        //    .WithStoreEntityQueryInField(ref _blueTeamQuery)
        //    .WithAll<BlueTeamTag>()
        //    .WithNone<DeadTag>()
        //    .ForEach((Entity e, ref PhysicsVelocity velocity, in BeeData beeData)
        //=>
        //    {


        //        var dir = rand.NextFloat3();
        //        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        //        dir /= len;
        //        dir -= 0.5f;
        //        dir *= rand.NextFloat(0, 1);

        //        velocity.Linear += dir * (beeData.flightJitter * deltaTime);
        //        velocity.Linear *= (1f - beeData.damping);

        //        var beePos = GetComponent<Translation>(e);

        //        var BlueRandomAttractorBee = blueTeamEntities[UnityEngine.Random.Range(0, blueTeamEntities.Length)];
        //        var blueAttractorBeePos = GetComponent<Translation>(BlueRandomAttractorBee);

        //        var deltaAttract = blueAttractorBeePos.Value - beePos.Value;
        //        var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);

        //        if (distAttract > 0f)
        //        {
        //            velocity.Linear += deltaAttract * (beeData.teamAttraction * deltaTime / distAttract);
        //        }

        //        var BlueRandomRepellerBee = blueTeamEntities[UnityEngine.Random.Range(0, blueTeamEntities.Length)];
        //        var blueRepellerBeePos = GetComponent<Translation>(BlueRandomAttractorBee);

        //        var delta = blueRepellerBeePos.Value - beePos.Value;
        //        var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        //        if (dist > 0f)
        //        {
        //            velocity.Linear -= delta * (beeData.teamRepulsion * deltaTime / dist);
        //        }

        //        var bee = GetComponent<Bee>(e);

        //        // only used for smooth rotation:
        //        float3 oldSmoothPos = bee.smoothPosition;
        //        if (bee.isAttacking == false)
        //        {
        //            bee.smoothPosition = Vector3.Lerp(bee.smoothPosition, beePos.Value, deltaTime * beeData.rotationStiffness);
        //        }
        //        else
        //        {
        //            bee.smoothPosition = beePos.Value;
        //        }
        //        bee.smoothDirection = bee.smoothPosition - oldSmoothPos;

        //        //Bee stretching
        //        var size = bee.size;
        //        var scale = new float3(size, size, size);

        //        if (!bee.dead)
        //        {
        //            var velPow = (velocity.Linear * velocity.Linear);
        //            var magnitude = Mathf.Sqrt(velPow.x + velPow.y + velPow.z);

        //            var stretch = Mathf.Max(1f, magnitude * beeData.speedStretch);
        //            scale.z *= stretch;
        //            scale.x /= (stretch - 1f) / 5f + 1f;
        //            scale.y /= (stretch - 1f) / 5f + 1f;
        //        }
        //        var rotation = quaternion.identity;

        //        var q = (Vector3.zero == Vector3.left);

        //        var temp = (bee.smoothDirection == float3.zero);

        //        var isZeroVector = temp.x && temp.y && temp.z;

        //        if (!isZeroVector)
        //        {
        //            rotation = quaternion.LookRotation(bee.smoothDirection, new float3(0, 1, 0));
        //        }

        //        if (bee.dead)
        //        {
        //            bee.beeColor *= .75f;
        //            bee.beeScale *= Mathf.Sqrt(bee.deathTimer);
        //        }
        //    }).Run();


        //var yellowTeamEntities = _yellowTeamQuery.ToEntityArray(Allocator.Temp);


        //Entities
        //.WithStoreEntityQueryInField(ref _yellowTeamQuery)
        //    .WithAll<YellowTeamTag>()
        //    .WithNone<DeadTag>()
        //    .ForEach((Entity e, ref PhysicsVelocity velocity, ref BeeData beeData)
        //=>
        //    {
        //        //var t = (float3)UnityEngine.Random.insideUnitSphere;

        //        var dir = rand.NextFloat3();
        //        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        //        dir /= len;
        //        dir -= 0.5f;
        //        dir *= rand.NextFloat(0, 1);


        //        velocity.Linear += dir * (beeData.flightJitter * deltaTime);
        //        velocity.Linear *= (1f - beeData.damping);

        //        var beePos = GetComponent<Translation>(e);

        //        var yelloeRandomAttractor = yellowTeamEntities[UnityEngine.Random.Range(0, yellowTeamEntities.Length)];
        //        var yellowRandomAttractorPos = GetComponent<Translation>(yelloeRandomAttractor);

        //        var deltaAttract = yellowRandomAttractorPos.Value - beePos.Value;
        //        var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);
        //        if (distAttract > 0f)
        //        {
        //            velocity.Linear += deltaAttract * (beeData.teamAttraction * deltaTime / distAttract);
        //        }


        //        var yellowRandomRepeller = yellowTeamEntities[UnityEngine.Random.Range(0, yellowTeamEntities.Length)];
        //        var yellowRandomRepellerPos = GetComponent<Translation>(yellowRandomRepeller);

        //        var delta = yellowRandomRepellerPos.Value - beePos.Value;
        //        var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        //        if (dist > 0f)
        //        {
        //            velocity.Linear -= delta * (beeData.teamRepulsion * deltaTime / dist);
        //        }

        //        var bee = GetComponent<Bee>(e);

        //        // only used for smooth rotation:
        //        float3 oldSmoothPos = bee.smoothPosition;
        //        if (bee.isAttacking == false)
        //        {
        //            bee.smoothPosition = Vector3.Lerp(bee.smoothPosition, beePos.Value, deltaTime * beeData.rotationStiffness);
        //        }
        //        else
        //        {
        //            bee.smoothPosition = beePos.Value;
        //        }
        //        bee.smoothDirection = bee.smoothPosition - oldSmoothPos;

        //        //Bee stretching
        //        var size = bee.size;
        //        var scale = new float3(size, size, size);

        //        if (!bee.dead)
        //        {
        //            var velPow = (velocity.Linear * velocity.Linear);
        //            var magnitude = Mathf.Sqrt(velPow.x + velPow.y + velPow.z);

        //            var stretch = Mathf.Max(1f, magnitude * beeData.speedStretch);
        //            scale.z *= stretch;
        //            scale.x /= (stretch - 1f) / 5f + 1f;
        //            scale.y /= (stretch - 1f) / 5f + 1f;
        //        }
        //        var rotation = quaternion.identity;

        //        var q = (Vector3.zero == Vector3.left);

        //        var temp = (bee.smoothDirection == float3.zero);

        //        var isZeroVector = temp.x && temp.y && temp.z;

        //        if (!isZeroVector)
        //        {
        //            rotation = quaternion.LookRotation(bee.smoothDirection, new float3(0, 1, 0));
        //        }

        //        if (bee.dead)
        //        {
        //            bee.beeColor *= .75f;
        //            bee.beeScale *= Mathf.Sqrt(bee.deathTimer);
        //        }


        //    }).Run();

        ecb.Dispose();
    }
}


public partial struct containmentJob : IJobEntity
{
    public FieldData field;

    void Execute(Entity e, ref Translation trans, ref PhysicsVelocity velocity, in BeeData data)
    {
        if (System.Math.Abs(trans.Value.x) > field.size.x * .48f)
        {
            trans.Value.x = (field.size.x * .48f) * Mathf.Sign(trans.Value.x);
            velocity.Linear.x *= -.5f;
            velocity.Linear.y *= .8f;
            velocity.Linear.z *= .8f;
        }
        if (System.Math.Abs(trans.Value.z) > field.size.z * .48f)
        {
            trans.Value.z = (field.size.z * .48f) * Mathf.Sign(trans.Value.z);
            velocity.Linear.z *= -.5f;
            velocity.Linear.x *= .8f;
            velocity.Linear.y *= .8f;
        }
        if (System.Math.Abs(trans.Value.y) > field.size.y * .48f)
        {
            trans.Value.y = (field.size.y * .48f) * Mathf.Sign(trans.Value.y);
            velocity.Linear.y *= -.5f;
            velocity.Linear.z *= .8f;
            velocity.Linear.x *= .8f;
        }
    }
}
public partial struct targetingJob : IJobEntity
{
    public NativeArray<Entity> blueTeam;
    public NativeArray<Entity> yellowTeam;
    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;
    public ComponentDataFromEntity<Translation> positions;
    public EntityManager manager;
    public float dt;
    public EntityCommandBuffer ecb;

    //Race conditions????+ only reading from this data
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Bee> status;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in BeeData beeData, in AliveTag alive)
    {
        //if (bee.dead == false)
        //{
        //if(blueTeam.Contains(e))
        {
            if (bee.enemyTarget == Entity.Null && bee.resourceTarget == Entity.Null)
            {
                if (random.NextFloat(1.0f) < beeData.aggression)
                {
                    if (blueTeam.Contains(e))
                    {
                        if (yellowTeam.Length > 0)
                        {
                            var randomyellow = yellowTeam[random.NextInt(yellowTeam.Length)];
                            bee.enemyTarget = randomyellow;
                        }

                    }
                    else if (yellowTeam.Contains(e))
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

                    ecb.AddComponent(e, new TryGetRandomResourceTag());
                    //Debug.Log("Missing implementation for getting a resource");
                }
            }
            else if (bee.enemyTarget != Entity.Null)
            {
                if (blueTeam.IndexOf(bee.enemyTarget) == -1 && yellowTeam.IndexOf(bee.enemyTarget) == -1)
                {
                    Debug.Log("Bee target removed");
                    bee.enemyTarget = Entity.Null;
                }
                else if (status[bee.enemyTarget].dead)
                {
                    //Debug.Log("dead bee");
                    bee.enemyTarget = Entity.Null;
                }
                else
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

                            ParticleSystem._instance.InstantiateBloodParticle(ecb, positions[e].Value, new float3(1, -10, 1));

                            ecb.AddComponent(bee.enemyTarget, new DeadTag());
                            ecb.RemoveComponent<AliveTag>(bee.enemyTarget);
                            bee.enemyTarget = Entity.Null;
                        }
                    }
                }
            }
            else if (bee.resourceTarget != Entity.Null)
            {
                //Debug.Log("Bee has a resource target");    
                ecb.AddComponent(e, new CollectingTag());
            }
        }
        //}
    }
}
public partial struct MoveBeeJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public NativeArray<Entity> blueTeam;
    public NativeArray<Entity> yellowTeam;
    public ComponentDataFromEntity<Translation> positions;
    public float dt;
    public float3 us;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in BeeData beeData, in AliveTag alive)
    {
        //Debug.Log("Does this run?");

        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 1);


        velocity.Linear += dir * (beeData.flightJitter * dt);
        velocity.Linear *= (1f - beeData.damping);

        var beePos = positions[e];

        Entity randomAttractor;
        Entity randomRepeller;
        Translation randomAttractorPos = new Translation();
        Translation randomRepellerPos = new Translation();

        if (bee.team == 0)
        {
            randomAttractor = blueTeam[random.NextInt(0, blueTeam.Length - 1)];
            randomAttractorPos = positions[randomAttractor];
        }
        else
        {
            randomRepeller = yellowTeam[random.NextInt(0, yellowTeam.Length - 1)];
            randomRepellerPos = positions[randomRepeller];
        }


        var deltaAttract = randomAttractorPos.Value - beePos.Value;
        var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);
        if (distAttract > 0f)
        {
            velocity.Linear += deltaAttract * (beeData.teamAttraction * dt / distAttract);
        }

        var delta = randomRepellerPos.Value - beePos.Value;
        var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        if (dist > 0f)
        {
            velocity.Linear -= delta * (beeData.teamRepulsion * dt / dist);
        }

    }
}