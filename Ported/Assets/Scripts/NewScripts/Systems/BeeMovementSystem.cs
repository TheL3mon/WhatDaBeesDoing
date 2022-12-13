using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TreeEditor;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Random = UnityEngine.Random;
using JobRandom = Unity.Mathematics.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

public partial class BeeMovementSystem : SystemBase
{

    private EntityQuery _blueTeamQuery;
    private EntityQuery _yellowTeamQuery;
    private Unity.Mathematics.Random _random;

    //public BeeData beeData = new BeeData();

    protected override void OnStartRunning()
    {
        //bees = new List<Bee>(50000);
        //teamsOfBees = new List<Bee>[2];
        //pooledBees = new List<Bee>(50000);
        //var bpb = 1;

        int beesPerBatch = 1023;

        //beeData.beeMatrices = new List<List<Matrix4x4>>();
        //beeData.beeMatrices.Add(new List<Matrix4x4>());
        //beeData.beeColors = new List<List<Vector4>>();
        //beeData.beeColors.Add(new List<Vector4>());

        //beeData.matProps = new MaterialPropertyBlock();
        ////beeData.matProps.SetVectorArray("_Color", new float4x4[BeeData.beesPerBatch]);
        ////beeData.matProps.SetMatrix("_Color", new Matrix4x4[BeeData.beesPerBatch]);
        //beeData.matProps.SetMatrixArray("_Color", new Matrix4x4[beesPerBatch]);

        /*
        for (int i=0;i<2;i++) {
			teamsOfBees[i] = new List<Bee>(25000);
		}
		for (int i=0;i<startBeeCount;i++) {
			int team = i%2;
			SpawnBee(team);
		}
        */

    }

    protected override void OnUpdate()
    {
        _random = new Unity.Mathematics.Random((uint)Random.Range(1, 500000));
        var deltaTime = Time.DeltaTime;

        var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);
        var allBlueBees = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        //var nativearr = allBlueBees.ToEntityArray(Allocator.TempJob);

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(Allocator.Persistent);
        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(Allocator.Persistent);
        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);
        var positions = GetComponentDataFromEntity<Translation>(false);

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var testingJob = new targetingJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            status = beeStatus,
            positions = positions,
            dt = Time.DeltaTime,
            manager = EntityManager,
            ecb = ecb,
            random = _random
        }.Schedule();
        //Dynamic buffers is an option

        testingJob.Complete();
        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();
        
        ecb.Playback(World.EntityManager);

        //var position = positions[nativearr[0]];
        //.WithReadOnly(positions)

        Entities
            .WithStoreEntityQueryInField(ref _blueTeamQuery)
            .WithAll<BlueTeamTag>().ForEach((Entity e, ref PhysicsVelocity velocity, in BeeData beeData)
        =>{
            velocity.Linear += (float3) Random.insideUnitSphere * (beeData.flightJitter * deltaTime);
            velocity.Linear *= (1f - beeData.damping);

            var beePos = GetComponent<Translation>(e);

            var BlueRandomAttractorBee = blueTeamEntities[Random.Range(0, blueTeamEntities.Length)];
            var blueAttractorBeePos = GetComponent<Translation>(BlueRandomAttractorBee);

            var deltaAttract = blueAttractorBeePos.Value - beePos.Value;
            var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);
            
            if(distAttract > 0f)
            {
                velocity.Linear += deltaAttract * (beeData.teamAttraction * deltaTime / distAttract);
            }

            var BlueRandomRepellerBee = blueTeamEntities[Random.Range(0, blueTeamEntities.Length)];
            var blueRepellerBeePos = GetComponent<Translation>(BlueRandomAttractorBee);
            
            var delta = blueRepellerBeePos.Value - beePos.Value;
            var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if (dist > 0f)
            {
                velocity.Linear -= delta * (beeData.teamRepulsion * deltaTime / dist);
            }

        }).Run();


        var yellowTeamEntities = _yellowTeamQuery.ToEntityArray(Allocator.Temp);


        Entities
        .WithStoreEntityQueryInField(ref _yellowTeamQuery)
            .WithAll<YellowTeamTag>().ForEach((Entity e, ref PhysicsVelocity velocity, ref BeeData beeData)
        => {
            velocity.Linear += (float3)Random.insideUnitSphere * (beeData.flightJitter * deltaTime);
            velocity.Linear *= (1f - beeData.damping);

            var beePos = GetComponent<Translation>(e);

            var yelloeRandomAttractor = yellowTeamEntities[Random.Range(0, yellowTeamEntities.Length)];
            var yellowRandomAttractorPos = GetComponent<Translation>(yelloeRandomAttractor);

            var deltaAttract = yellowRandomAttractorPos.Value - beePos.Value;
            var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);
            if (distAttract > 0f)
            {
                velocity.Linear += deltaAttract * (beeData.teamAttraction * deltaTime / distAttract);
            }


            var yellowRandomRepeller = yellowTeamEntities[Random.Range(0, yellowTeamEntities.Length)];
            var yellowRandomRepellerPos = GetComponent<Translation>(yellowRandomRepeller);

            var delta = yellowRandomRepellerPos.Value - beePos.Value;
            var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if (dist > 0f)
            {
                velocity.Linear -= delta * (beeData.teamRepulsion * deltaTime / dist);
            }
        }).Run();
    }
}


public partial struct beeMoveJob : IJobEntity
{
    public float deltaTime;
    public float3 unitSphere;
    public float3 targetPos;
    void Execute(ref PhysicsVelocity velocity, in BeeData beeData)
    {
        
        
    }
}
public partial struct targetingJob :IJobEntity
{
    public NativeArray<Entity> blueTeam;
    public NativeArray<Entity> yellowTeam;
    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Translation> positions;
    public EntityManager manager;
    public float dt;
    public EntityCommandBuffer ecb;

    //Race conditions????+ only reading from this data
    [NativeDisableContainerSafetyRestriction][ReadOnly] public ComponentDataFromEntity<Bee> status;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in BeeData beeData)
    {
        if (bee.dead == false)
        {
            if(blueTeam.Contains(e))
            {
                if (bee.enemyTarget == Entity.Null && bee.resourceTarget == Entity.Null)
                {
                    if(random.NextFloat(1.0f) < beeData.aggression)
                    {
                        if(yellowTeam.Length > 0)
                        {
                            var randomyellow = yellowTeam[random.NextInt(yellowTeam.Length)];
                            bee.enemyTarget = randomyellow;
                        }
                    } else
                    {
                        Debug.Log("Missing implementation for getting a resource");
                    }
                } else if (bee.enemyTarget != Entity.Null)
                {
                    if (status[bee.enemyTarget].dead)
                    {
                        Debug.Log("dead bee");
                        bee.enemyTarget = Entity.Null;
                    } else
                    {
                        var delta = positions[bee.enemyTarget].Value - positions[e].Value;
                        float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                        if(sqrDist > beeData.attackDistance * beeData.attackDistance)
                        {
                            velocity.Linear += delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist));
                        } else
                        {
                            bee.isAttacking = true;
                            velocity.Linear += delta * (beeData.attackForce * dt / Mathf.Sqrt(sqrDist));
                            if (sqrDist < beeData.hitDistance * beeData.hitDistance)
                            {
                                                                
                                //Spawn particles
                                //velocity change

                                ecb.AddComponent(bee.enemyTarget, new DeadTag());
                                bee.enemyTarget = Entity.Null;
                            }
                        }
                    }
                }
            }
        }
    }
}
