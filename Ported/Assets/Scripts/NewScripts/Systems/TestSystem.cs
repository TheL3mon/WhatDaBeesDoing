using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using JobRandom = Unity.Mathematics.Random;
using Random = UnityEngine.Random;

public partial class TestSystem : SystemBase
{
    private EntityQuery _blueTeamQuery;
    private EntityQuery _yellowTeamQuery;
    private Unity.Mathematics.Random _random;
    protected override void OnUpdate()
    {
        _random = new Unity.Mathematics.Random((uint)Random.Range(1, 500000));
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var deltaTime = Time.DeltaTime;

        // var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);
        // var allBlueBees = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
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

        var TGRRJ = new tryGetRandomResourceJob
        {
            resources = resourceArr,
            resourceStatus = resourceStatus,
            ecb = ecb,
            random = _random
        }.Schedule();
        TGRRJ.Complete();

        var collectingJob = new collectResourceJob
        {
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            resources = resourceArr,
            resourceStatus = resourceStatus,
            positions = positions,
            dt = Time.DeltaTime,
            ecb = ecb,
            random = _random
        }.Schedule();

        collectingJob.Complete();
        ecb.Playback(World.EntityManager);


        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();
    }
}


public partial struct tryGetRandomResourceJob : IJobEntity
{

    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;


    public EntityCommandBuffer ecb;

    public JobRandom random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in TryGetRandomResourceTag tag, in BeeData beeData)
    {
        if (resources.Length == 0) return;

        var resource = resources[random.NextInt(resources.Length)];
        var status = resourceStatus[resource];

        if(status.stacked == true && status.topOfStack == true)
        {
            bee.resourceTarget = resource;
            ecb.AddComponent(e, new CollectingTag());
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        } else if (status.stacked == false ){
            bee.resourceTarget = resource;
            ecb.AddComponent(e, new CollectingTag());
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        } else
        {
            bee.resourceTarget = Entity.Null;
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        }
    }
}



public partial struct collectResourceJob : IJobEntity
{

    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;
    public ComponentDataFromEntity<Translation> positions;
    public NativeArray<Entity> blueTeam;
    public NativeArray<Entity> yellowTeam;
    public float dt;

    public EntityCommandBuffer ecb;


    public JobRandom random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in CollectingTag tag, in BeeData beeData)
    {
        var target = bee.resourceTarget;
        var index = resources.IndexOf(target);
        var resource = resources[index];
        var status = resourceStatus[resource];
        bee.resourceTarget = resource;
        bool beePartOfBlueTeam = blueTeam.Contains(e);
        bool beePartOfYellowTeam = yellowTeam.Contains(e);
        bool holderPartOfBlueTeam = false;
        bool holderPartOfYellowTeam = false;

        Debug.Log("In collcting job");

        if (resources.Length == 0)
        { return; }
        
        if (status.holder != Entity.Null)
        {
            holderPartOfBlueTeam = blueTeam.Contains(status.holder);
            holderPartOfYellowTeam = yellowTeam.Contains(status.holder);
        }

        if (status.holder == Entity.Null)
        {
            if (status.dead == true)
            {
                Debug.Log("resource is dead");
                bee.resourceTarget = Entity.Null;
                return;
            }
            if (status.stacked == true && status.topOfStack == false) bee.resourceTarget = Entity.Null;
            else if (status.stacked == true && status.topOfStack == false)
            {
                var delta = positions[resource].Value - positions[e].Value;
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > beeData.grabDistance * beeData.grabDistance)
                {
                    Debug.Log("Moving towards resource");
                    velocity.Linear += (delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist)) * 300);
                }
                else
                {
                    Debug.Log("Implement logic to grab resource");
                    status.holder = e;
                    status.stacked = false;
                    //reduce stack of resource
                }
            }
        }
        else
        {
            if (status.holder == e)
            {
                float3 targetPos = new float3(100, 20, 100);
                //var beePos = new Vector3(positions[e].Value.x, positions[e].Value.y, positions[e].Value.z);
                var delta = targetPos - positions[e].Value;
                var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                velocity.Linear += (targetPos - positions[e].Value) * (beeData.carryForce * dt / dist);
                if (dist < 1f)
                {
                    Debug.Log("Bee arrived at destination");
                    //var beeResourceStatus = resourceStatus[bee.resourceTarget];
                    //status.holder = Entity.Null;
                }
                //do stuff
            }
            else if (beePartOfBlueTeam && holderPartOfYellowTeam) bee.enemyTarget = status.holder;
            else if (beePartOfYellowTeam && holderPartOfBlueTeam) bee.enemyTarget = status.holder;
            else if (beePartOfBlueTeam && holderPartOfBlueTeam) bee.resourceTarget = Entity.Null;
            else if (beePartOfYellowTeam && holderPartOfYellowTeam) bee.resourceTarget = Entity.Null;
        }

    }
}