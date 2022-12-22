using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
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
    private static ResourceData _resourceData;
    private Unity.Mathematics.Random _random;
    protected override void OnUpdate()
    {
        _random = new Unity.Mathematics.Random((uint)Random.Range(1, 500000));
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var deltaTime = Time.DeltaTime;

        // var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);
        // var allBlueBees = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        //var nativearr = allBlueBees.ToEntityArray(Allocator.TempJob);
        //_resourceData = GetSingleton<ResourceData>();
        _resourceData = ResourceSystem._resourceData;

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(Allocator.Persistent);
        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(Allocator.Persistent);
        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(Allocator.Persistent);

        var beeStatus = GetComponentDataFromEntity<Bee>(true);
        var positions = GetComponentDataFromEntity<Translation>(false);
        var resourceStatus = GetComponentDataFromEntity<Resource>(false);

        var stackHeights = ResourceSystem._stackHeights;

        var TGRRJ = new tryGetRandomResourceJob
        {
            resources = resourceArr,
            resourceStatus = resourceStatus,
            ecb = ecb,
            stackHeights = stackHeights,
            resourceData = _resourceData,
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
            stackHeights = stackHeights,
            resourceData = _resourceData,
            dt = Time.DeltaTime,
            ecb = ecb,
            random = _random
        }.Schedule();

        collectingJob.Complete();
        ecb.Playback(World.EntityManager);


        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();
        ecb.Dispose();
    }
}


public partial struct tryGetRandomResourceJob : IJobEntity
{

    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;
    public NativeList<int> stackHeights;
    public ResourceData resourceData;


    public EntityCommandBuffer ecb;

    public JobRandom random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in TryGetRandomResourceTag tag, in BeeData beeData)
    {
        if (resources.Length == 0) return;

        var resource = resources[random.NextInt(resources.Length)];
        var status = resourceStatus[resource];

        int index = status.gridX + status.gridY * resourceData.gridCounts.x;

        var stackHeight = stackHeights[index];

        //int max = 0;

        /*
        for (int i = 0; i < stackHeights.Length; i++) 
        {
            var v = stackHeights[i];
            if (v > max)
            {
                max = v;
            }
        }
        */

        //Debug.Log("max height: " + max);

        var resourceHeight = status.height;

        //Debug.Log("gridPos2: (" + status.gridX + ", " + status.gridY + "). index: (" + index + "). heights: (" + resourceHeight + ", " + stackHeight+")");

        if (resourceHeight == stackHeight)
        {
            bee.resourceTarget = resource;
            ecb.AddComponent(e, new CollectingTag());
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        }
        else
        {
            bee.resourceTarget = Entity.Null;
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        }

        /*
        if (status.stacked == true && status.topOfStack == true)
        {
            bee.resourceTarget = resource;
            ecb.AddComponent(e, new CollectingTag());
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        } else if (status.stacked == false ){
            bee.resourceTarget = resource;
            ecb.AddComponent(e, new CollectingTag());
            ecb.RemoveComponent<TryGetRandomResourceTag>(e);
        } else*/

        {
        }
    }
}



public partial struct collectResourceJob : IJobEntity
{

    public NativeArray<Entity> resources;
    public ComponentDataFromEntity<Resource> resourceStatus;
    public ComponentDataFromEntity<Translation> positions;
    public ResourceData resourceData;
    public NativeArray<Entity> blueTeam;
    public NativeArray<Entity> yellowTeam;
    public NativeList<int> stackHeights;
    public float dt;

    public EntityCommandBuffer ecb;


    public JobRandom random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, in CollectingTag tag, in BeeData beeData)
    {
        var target = bee.resourceTarget;
        var index = resources.IndexOf(target);
        if (index == -1)
            return;
        var resource = resources[index];
        var status = resourceStatus[resource];
        bee.resourceTarget = resource;
        bool beePartOfBlueTeam = blueTeam.Contains(e);
        bool beePartOfYellowTeam = yellowTeam.Contains(e);
        bool holderPartOfBlueTeam = false;
        bool holderPartOfYellowTeam = false;

        //Debug.Log("In collcting job");

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
            int resourceIndex = status.gridX + status.gridY * resourceData.gridCounts.x;
            var stackHeight = stackHeights[resourceIndex];
            var resourceHeight = status.height;

            Debug.Log("gridPos2: (" + status.gridX + ", " + status.gridY + "). index: (" + index + "). heights: (" + resourceHeight + ", " + stackHeight + ")");

            if (stackHeight != resourceHeight) 
                bee.resourceTarget = Entity.Null;
            else
            {
                var delta = positions[resource].Value - positions[e].Value;
                float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
                if (sqrDist > beeData.grabDistance * beeData.grabDistance)
                {
                    Debug.Log("Moving towards resource");
                    velocity.Linear += (delta * (beeData.chaseForce * dt / Mathf.Sqrt(sqrDist)));
                }
                else
                {
                    Debug.Log("Implement logic to grab resource");
                    status.holder = e;
                    status.stacked = false;

                    //Set component

                    var r = new Resource();
                    r.position = status.position;
                    r.height = status.height;
                    r.holder = e;

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
                Debug.Log("Bee should be returning resource");
                //float3 targetPos = new float3(100, 20, 100); //TODO should find its own field
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
                    Debug.Log("Bee arrived at destination");
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = status.height;
                    r.holder = Entity.Null;


                    var fallingResourceTag = new FallingResourceTag();
                    ecb.SetComponent(bee.resourceTarget, r);
                    ecb.AddComponent(bee.resourceTarget, fallingResourceTag);

                    //ecb.AddComponent(bee.enemyTarget, new FallingResourceTag());
                }
                else
                {
                    var r = new Resource();
                    r.position = positions[e].Value;
                    r.height = status.height;
                    r.holder = e;

                    ecb.SetComponent(bee.resourceTarget, r);

                    ecb.SetComponent(bee.resourceTarget, new Translation
                    {
                        Value = r.position
                    });
                }


                /*
                if (dist < 1f)
                {
                    Debug.Log("Bee arrived at destination");
                    //var beeResourceStatus = resourceStatus[bee.resourceTarget];
                    //status.holder = Entity.Null;
                }*/
            }
            else if (beePartOfBlueTeam && holderPartOfYellowTeam) bee.enemyTarget = status.holder;
            else if (beePartOfYellowTeam && holderPartOfBlueTeam) bee.enemyTarget = status.holder;
            else if (beePartOfBlueTeam && holderPartOfBlueTeam) bee.resourceTarget = Entity.Null;
            else if (beePartOfYellowTeam && holderPartOfYellowTeam) bee.resourceTarget = Entity.Null;
        }

    }
}