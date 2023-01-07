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
    private Random _random;
    private FieldData _fieldData;

    protected override void OnCreate()
    {
        this.Enabled = true;
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        _fieldData = GetSingleton<FieldData>();
    }


    protected override void OnUpdate()
    {
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

        var deltaTime = UnityEngine.Time.deltaTime;

        var movementJob = new MoveBeeJob
        {
            ecb = ecb.AsParallelWriter(),
            blueTeam = blueArr, 
            yellowTeam = yellowArr,
            positions = positions,
            dt = deltaTime,
            random = _random,
            field = _fieldData
        }.ScheduleParallel();

        Dependency = movementJob;

        Dependency.Complete();

        Debug.Log("Number of bees: " + (blueArr.Length + yellowArr.Length) + ", Blue: " + blueArr.Length + " Yellow: " + yellowArr.Length + ", Dead: " + deadArr.Length);

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

    void Execute(Entity e, [EntityInQueryIndex] int entityIndex, ref Bee bee, ref Rotation rotation, ref NonUniformScale nus, in Translation position, in BeeData beeData)
    {
        if (float.IsNaN(position.Value.x) || float.IsNaN(position.Value.y) || float.IsNaN(position.Value.z))
        {
            return;
        }

        var oldPos = position.Value;
        //Debug.Log(e.Index + " beePos before: " + oldPos);


        if (bee.dead)
        {
            if (System.Math.Abs(position.Value.y) > field.size.y * .48f)
            {
                bee.velocity = float3.zero;
                var pos = position.Value;
                if (position.Value.y < -(field.size.y/2))
                {
                    pos = -(field.size.y / 2);
                }
                ecb.SetComponent(entityIndex, e, new Translation { Value = pos });
            }
            else
            {
                bee.velocity = field.gravity * new float3(0, -9.8f, 0);
                var pos = position.Value + (dt * bee.velocity);
                ecb.SetComponent(entityIndex, e, new Translation { Value = pos });
            }
            return;
        }


        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 2);

        bee.velocity += dir * (beeData.flightJitter * dt);
        bee.velocity *= (1f - beeData.damping);

        //if(float.IsNaN(velocity.Linear.x))
        //    Debug.Log(e.Index + " before: " + old_velocity + " after:" + velocity.Linear);

        Translation randomAttractorPos = new Translation();
        Translation randomRepellerPos = new Translation();

        if (bee.team == 0)
        {
            randomAttractorPos = positions[blueTeam[random.NextInt(0, blueTeam.Length)]];
            randomRepellerPos = positions[blueTeam[random.NextInt(0, blueTeam.Length)]];
        }
        else
        {
            randomAttractorPos = positions[yellowTeam[random.NextInt(0, yellowTeam.Length)]];
            randomRepellerPos = positions[yellowTeam[random.NextInt(0, yellowTeam.Length)]];
        }

        var deltaAttract = randomAttractorPos.Value - position.Value;
        var distAttract = Mathf.Sqrt(deltaAttract.x * deltaAttract.x + deltaAttract.y * deltaAttract.y + deltaAttract.z * deltaAttract.z);
        if (distAttract > 0f)
        {
            bee.velocity += deltaAttract * (beeData.teamAttraction * dt / distAttract);
        }

        var delta = randomRepellerPos.Value - position.Value;
        var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
        if (dist > 0f)
        {
            bee.velocity -= delta * (beeData.teamRepulsion * dt / dist);
        }

        var newPos = position.Value + (dt * bee.velocity);

        float arenaAttraction = 10f;

        if (System.Math.Abs(newPos.x) > field.size.x * .48f)
        {
            bee.velocity.x = arenaAttraction * -Mathf.Sign(position.Value.x);
            bee.velocity.y *= .8f;
            bee.velocity.z *= .8f;

        }

        if (System.Math.Abs(newPos.z) > field.size.z * .48f)
        {
            bee.velocity.z = bee.velocity.z = arenaAttraction * -Mathf.Sign(position.Value.z);
            bee.velocity.x *= .8f;
            bee.velocity.y *= .8f;
        }


        if (System.Math.Abs(newPos.y) > field.size.y * .48f)
        {
            bee.velocity.y = arenaAttraction * -Mathf.Sign(position.Value.y);
            bee.velocity.z *= .8f;
            bee.velocity.x *= .8f;
        }

        //position.Value += dt * bee.velocity;

        newPos = position.Value + (dt * bee.velocity);

        ecb.SetComponent(entityIndex, e, new Translation { Value = newPos});

        // only used for smooth rotation:
        float3 oldSmoothPos = bee.smoothPosition;
        if (bee.isAttacking == false)
        {
            bee.smoothPosition = Vector3.Lerp(bee.smoothPosition, position.Value, dt * beeData.rotationStiffness);
        }
        else
        {
            bee.smoothPosition = position.Value;
        }
        bee.smoothDirection = bee.smoothPosition - oldSmoothPos;

        //Bee stretching
        var size = bee.size;
        var scale = new float3(size, size, size);

        if (!bee.dead)
        {
            var velPow = (bee.velocity * bee.velocity);
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