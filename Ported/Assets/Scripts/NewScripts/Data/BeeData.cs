using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[GenerateAuthoringComponent]
public struct BeeData : IComponentData
{
    public int team;
    public float flightJitter;
    public float teamAttraction;
    public float teamRepulsion;
    public float damping;


    // NOT TESTED/CONVERTED VVVVV
    public MeshRenderer beeMesh;
    public Material beeMaterial;
    public URPMaterialPropertyBaseColor[] teamColors;
    public float minBeeSize;
    public float maxBeeSize;
    public float speedStretch;
    public float rotationStiffness;
    public float aggression;
    public float chaseForce;
    public float carryForce;
    public float grabDistance;
    public float attackDistance;
    public float attackForce;
    public float hitDistance;
    public float maxSpawnSpeed;
    //[Space(10)]
    public int startBeeCount;

    //List<Bee> bees;
    //List<Bee>[] teamsOfBees;
    //List<Bee> pooledBees;

    //int activeBatch = 0;
    List<List<Matrix4x4>> beeMatrices;
    List<List<Vector4>> beeColors;

    //static BeeManager instance;

    const int beesPerBatch = 1023;
    MaterialPropertyBlock matProps;
    // THIS IS HOW WE'RE GONNA PASS THE COLOR IN BeeManager AND ParticleManager VVVVV
    // OR WE COULD USE URPMaterialPropertyBaseColor
    /*
    [MaterialProperty( "_Fill" , MaterialPropertyFormat.Float )]
    public struct FillMaterialProperty : IComponentData
    {
        public float Value;
    }

    var healthData = GetComponentDataFromEntity<Health>();
    uint systemVersion = LastSystemVersion;
    Entities
    .WithName("update_fill_property_job")
    .WithAll<HealthBar>()
    .WithReadOnly( healthData )
    .ForEach( ( ref FillMaterialProperty fill , in Parent parent ) =>
    {
        if( healthData.DidChange(parent.Value,systemVersion) )
        {
            var hp = healthData[ parent.Value ];
            fill.Value = hp.value / 100f;
        }
    } )
    .WithBurst().ScheduleParallel();
    */
}
