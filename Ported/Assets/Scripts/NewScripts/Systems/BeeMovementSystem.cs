using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
using Unity.Burst;

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
        this.Enabled = true;

        _random = new Random();
        _random.InitState((uint)UnityEngine.Random.Range(0, 100000));

        var blueTeamQuery = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        var blueArr = blueTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
        var yellowTeamQuery = GetEntityQuery(ComponentType.ReadOnly<YellowTeamTag>());
        var yellowArr = yellowTeamQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);
        var resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceTag>());
        var resourceArr = resourceQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var aliveQuery = GetEntityQuery(ComponentType.ReadOnly<AliveTag>());
        var aliveArr = aliveQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var deadQuery = GetEntityQuery(ComponentType.ReadOnly<DeadTag>());
        var deadArr = deadQuery.ToEntityArray(World.UpdateAllocator.ToAllocator);

        var positions = GetComponentDataFromEntity<Translation>(true);
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var movementJob = new MoveBeeJob
        {
            ecb = ecb.AsParallelWriter(),
            blueTeam = blueArr, 
            yellowTeam = yellowArr,
            positions = positions,
            dt = Time.DeltaTime,
            random = _random,
            field = _fieldData
        }.ScheduleParallel();

        movementJob.Complete();

        Debug.Log("Number of bees: " + (blueArr.Length + yellowArr.Length) + ", Alive: " + aliveArr.Length + ", Dead: " + deadArr.Length);

        //Dynamic buffers is an option
        blueArr.Dispose();
        yellowArr.Dispose();
        resourceArr.Dispose();

        ecb.Playback(World.EntityManager);

        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct MoveBeeJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public NativeArray<Entity> blueTeam;
    [ReadOnly] public NativeArray<Entity> yellowTeam;
    [ReadOnly] public ComponentDataFromEntity<Translation> positions;
    [ReadOnly] public FieldData field;
    public float dt;

    public Unity.Mathematics.Random random;

    void Execute(Entity e, ref Bee bee, ref PhysicsVelocity velocity, ref Rotation rotation, ref NonUniformScale nus, in BeeData beeData)
    {
        var beePos = positions[e];

        if (float.IsNaN(positions[e].Value.x) || float.IsNaN(positions[e].Value.y) || float.IsNaN(positions[e].Value.z))
        {
            return;
        }

        var oldPos = beePos.Value;
        //Debug.Log(e.Index + " beePos before: " + oldPos);

        if (bee.dead)
        {
            if (System.Math.Abs(beePos.Value.y) > field.size.y * .48f)
                velocity.Linear = float3.zero;
            else
                velocity.Linear = field.gravity * new float3(0, -9.8f, 0);
            return;
        }


        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 1);

        var vel = velocity.Linear;
        vel  += dir * (beeData.flightJitter * dt);
        vel *= (1f - beeData.damping);

        //if(float.IsNaN(velocity.Linear.x))
        //    Debug.Log(e.Index + " before: " + old_velocity + " after:" + velocity.Linear);


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
            vel += deltaAttract * (beeData.teamAttraction * dt / distAttract);
        }

        var delta = randomRepellerPos.Value - beePos.Value;
        var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        if (dist > 0f)
        {
            vel -= delta * (beeData.teamRepulsion * dt / dist);
        }

        var newPos = beePos.Value + (dt * vel);

        float arenaAttraction = 10f;

        if (System.Math.Abs(newPos.x) > field.size.x * .48f)
        {
            vel.x = arenaAttraction * -Mathf.Sign(beePos.Value.x);
            //vel.x *= -1.0f;
            vel.y *= .8f;
            vel.z *= .8f;

            //if (System.Math.Abs(vel.x) < 5.0f)
            //    vel.x = arenaAttraction * -Mathf.Sign(beePos.Value.x);

        }

        if (System.Math.Abs(newPos.z) > field.size.z * .48f)
        {
            vel.z = vel.z = arenaAttraction * -Mathf.Sign(beePos.Value.z);
            //vel.z *= -1.0f;
            vel.x *= .8f;
            vel.y *= .8f;
            //if (System.Math.Abs(vel.z) < 5.0f)
            //    vel.z = arenaAttraction * -Mathf.Sign(beePos.Value.z);
        }


        if (System.Math.Abs(newPos.y) > field.size.y * .48f)
        {
            vel.y = arenaAttraction * -Mathf.Sign(beePos.Value.y);
            //vel.y *= -1.0f;
            vel.z *= .8f;
            vel.x *= .8f;
            //if (System.Math.Abs(vel.y) < 5.0f)
            //    vel.y = arenaAttraction * -Mathf.Sign(beePos.Value.y);
        }

        velocity.Linear = vel;

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
            var velPow = (vel * vel);
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

        //if (float.IsNaN(oldPos.x) || float.IsNaN(beePos.Value.x))
        //    Debug.Log(e.Index + " beePos before: " + oldPos + ", after: " + beePos.Value);
    }
}