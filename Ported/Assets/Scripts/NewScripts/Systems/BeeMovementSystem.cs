using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TreeEditor;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public partial class BeeMovementSystem : SystemBase
{

    private EntityQuery _blueTeamQuery;
    private EntityQuery _yellowTeamQuery;


    protected override void OnStartRunning()
    {
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;

        var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);
        var randBlueTeamMember = blueTeamEntities[Random.Range(0, blueTeamEntities.Length)];
        var randBlueTeamMemeberPos = GetComponent<Translation>(randBlueTeamMember);

        var yellowTeamEntities = _yellowTeamQuery.ToEntityArray(Allocator.Temp);
        var randYellowTeamMember = yellowTeamEntities[Random.Range(0, yellowTeamEntities.Length)];
        var randYellowTeamMemeberPos = GetComponent<Translation>(randYellowTeamMember);

        Entities
            .WithStoreEntityQueryInField(ref _blueTeamQuery)
            .WithAll<BlueTeamTag>().ForEach((ref Translation translation, ref PhysicsVelocity velocity, in BeeData beeData)
        =>{
            velocity.Linear += (float3) Random.insideUnitSphere * (beeData.flightJitter * deltaTime);

            var delta = randBlueTeamMemeberPos.Value - translation.Value;
            var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if(dist > 0f)
            {
                velocity.Linear += delta * (beeData.teamAttraction * deltaTime / dist);
            }
        }).Run();

        Entities
            .WithStoreEntityQueryInField(ref _yellowTeamQuery)
            .WithAll<YellowTeamTag>().ForEach((ref Translation translation, ref PhysicsVelocity velocity, ref BeeData beeData)
        => {
            velocity.Linear += (float3)Random.insideUnitSphere * (beeData.flightJitter * deltaTime);

            var delta = randYellowTeamMemeberPos.Value - translation.Value;
            var dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            if (dist > 0f)
            {
                velocity.Linear += delta * (beeData.teamAttraction * deltaTime / dist);
            }
        }).Run();
    }
}


public partial struct beeMoveJob : IJobEntity
{
    public float deltaTime;
    public float3 unitSphere;
    public float3 targetPos;
    void Execute(Entity e, ref PhysicsVelocity velocity, in BeeData beeData)
    {
        
    }
}
