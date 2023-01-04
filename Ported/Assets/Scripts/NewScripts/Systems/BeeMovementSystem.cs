using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using Unity.Burst;
using static UnityEditor.PlayerSettings;

[UpdateBefore(typeof(BeeBehaviorSystem))]
public partial class BeeMovementSystem : SystemBase
{
    public static bool testing_InvincibleBees = true;
    private Random _random;
    private FieldData _fieldData;

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
    }


    protected override void OnUpdate()
    {
        _random.InitState((uint)UnityEngine.Random.Range(0, 100000));

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var positions = GetComponentDataFromEntity<Translation>(true);
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var containJob = new ContainmentJob
        {
            field = _fieldData
            //random = _random
        }.ScheduleParallel();

        containJob.Complete();

        Dependency = containJob;

        var movementJob = new MoveBeeJob
        {
            ecb = ecb.AsParallelWriter(),
            blueTeam = blueArr,
            yellowTeam = yellowArr,
            positions = positions,
            dt = Time.DeltaTime,
            random = _random
        }.ScheduleParallel(Dependency);

        movementJob.Complete();

        Debug.Log("Number of bees: " + (blueArr.Length + yellowArr.Length));
        //Debug.Log("Number of alive bees: " + (aliveArr.Length));

        //Dynamic buffers is an option
        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();

        ecb.Playback(World.EntityManager);

        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct ContainmentJob : IJobEntity
{
    [ReadOnly] public FieldData field;

    //public Unity.Mathematics.Random random;
    void Execute(Entity e, ref Translation pos, ref PhysicsVelocity velocity)
    {
        // bool printValue = false;
        //if (random.NextFloat(0, 1) < -1f)
        //{
        //    printValue = true;
        //}

        //var percentage = 0.0f;

        if (System.Math.Abs(pos.Value.x) > field.size.x * .48f)
        {
            //var oldVel = velocity.Linear;
            pos.Value.x = (field.size.x * .48f) * Mathf.Sign(pos.Value.x);
            velocity.Linear.x *= -.5f;
            velocity.Linear.y *= .8f;
            velocity.Linear.z *= .8f;
            //if (random.NextFloat(0, 1) < percentage)
            //    Debug.Log(e.Index + " X: position" + pos.Value + ", old vel: " + oldVel + ", new vel:" + velocity.Linear + ", field size: " + field.size);
            //Debug.Log("X: position" + pos.Value +", vel: " + velocity.Linear+ ", field size: " + field.size);
        }
        if (System.Math.Abs(pos.Value.z) > field.size.z * .48f)
        {
            var oldVel = velocity.Linear;
            pos.Value.z = (field.size.z * .48f) * Mathf.Sign(pos.Value.z);
            velocity.Linear.z *= -.5f;
            velocity.Linear.x *= .8f;
            velocity.Linear.y *= .8f;
            //if (random.NextFloat(0, 1) < percentage)
            //    Debug.Log(e.Index + " Z: position" + pos.Value + ", old vel: " + oldVel + ", new vel:" + velocity.Linear + ", field size: " + field.size);
            //Debug.Log("Z: position" + pos.Value + ", vel: " + velocity.Linear);
        }
        if (System.Math.Abs(pos.Value.y) > field.size.y * .48f)
        {
            //var oldVel = velocity.Linear;
            pos.Value.y = (field.size.y * .48f) * Mathf.Sign(pos.Value.y);
            velocity.Linear.y *= -.5f;
            velocity.Linear.z *= .8f;
            velocity.Linear.x *= .8f;
            //if (random.NextFloat(0, 1) < percentage)
            //    Debug.Log(e.Index + " Y: position" + pos.Value + ", old vel: " + oldVel + ", new vel:" + velocity.Linear + ", field size: " + field.size);
            //Debug.Log("Y: position" + pos.Value + ", vel: " + velocity.Linear + ", field size: " + field.size);
            //Debug.Log("Y: position" + pos.Value + ", vel: " + velocity.Linear);
        }
    }
}

[BurstCompile]
public partial struct MoveBeeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public NativeArray<Entity> blueTeam;
    [ReadOnly] public NativeArray<Entity> yellowTeam;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;
    public float dt;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, ref Rotation rotation, ref NonUniformScale nus, in BeeData beeData, in AliveTag alive)
    {
        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 1);

        var old_velocity = velocity.Linear;
        velocity.Linear += dir * (beeData.flightJitter * dt);
        velocity.Linear *= (1f - beeData.damping);
        //if (random.NextFloat(0, 1) < 0.05f)
        //    Debug.Log(e.Index + " before: " + old_velocity + " after:" + velocity.Linear);

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

        // only used for smooth rotation:
        float3 oldSmoothPos = bee.smoothPosition;
        if (bee.isAttacking == false)
        {
            bee.smoothPosition = Vector3.Lerp(bee.smoothPosition, beePos.Value, dt * beeData.rotationStiffness);
        }
        else
        {
            bee.smoothPosition = beePos.Value;
        }
        bee.smoothDirection = bee.smoothPosition - oldSmoothPos;

        //Bee stretching
        var size = bee.size;
        var scale = new float3(size, size, size);

        if (!bee.dead)
        {
            var velPow = (velocity.Linear * velocity.Linear);
            var magnitude = Mathf.Sqrt(velPow.x + velPow.y + velPow.z);

            var stretch = Mathf.Max(1f, magnitude * beeData.speedStretch);
            scale.z *= stretch;
            scale.x /= (stretch - 1f) / 5f + 1f;
            scale.y /= (stretch - 1f) / 5f + 1f;
        }


        var rot = quaternion.identity;

        var temp = (bee.smoothDirection == float3.zero);

        var isZeroVector = temp.x && temp.y && temp.z;

        if (!isZeroVector)
        {
            rot = quaternion.LookRotation(bee.smoothDirection, new float3(0, 1, 0));
        }

        bee.beeScale = scale;

        rotation.Value = rot;

        nus.Value = bee.beeScale;
    }
}