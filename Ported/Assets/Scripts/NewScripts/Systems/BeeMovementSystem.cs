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
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Random = UnityEngine.Random;

public partial class BeeMovementSystem : SystemBase
{

    private EntityQuery _blueTeamQuery;
    private EntityQuery _yellowTeamQuery;

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
        var deltaTime = Time.DeltaTime;

        var blueTeamEntities = _blueTeamQuery.ToEntityArray(Allocator.Temp);
        var positions = GetComponentDataFromEntity<Translation>(true);
        var allBlueBees = GetEntityQuery(ComponentType.ReadOnly<BlueTeamTag>());
        //var nativearr = allBlueBees.ToEntityArray(Allocator.TempJob);

        //Dynamic buffers is an option


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
    void Execute(Entity e, ref PhysicsVelocity velocity, in BeeData beeData)
    {
        
    }
}
