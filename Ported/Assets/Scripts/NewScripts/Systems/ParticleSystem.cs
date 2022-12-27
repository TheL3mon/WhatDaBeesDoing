using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Unity.Entities.EntityQueryBuilder;
using static UnityEditor.PlayerSettings;
using static UnityEngine.ParticleSystem;

public partial class ParticleSystem : SystemBase
{
    float timer;
    public Unity.Mathematics.Random random;

    public ParticleData _particleData;
    public Entity _particlePrefab;

    //protected override void OnCreate()
    //{
    //    base.OnCreate();
    //}

    protected override void OnStartRunning()
    {
        _particleData = GetSingleton<ParticleData>();
        _particlePrefab = GetSingleton<ParticleData>().particlePrefab;

        random.InitState(6969);
    }

    protected override void OnUpdate()
    {
        timer += Time.DeltaTime;
        
        // SPAWNFLASH test
        if (Input.GetKeyDown(KeyCode.F))
		{
            // this random can only be called outside of Jobs apparently
            UnityEngine.Color bloodColor = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);

            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            var spawnFlashSpawnJob = new ParticleSpawnJob
            {
                ecb = ecb,
                particle = _particlePrefab,
                position = new float3(0, 0, 0), // TEST VALUE
                particleType = ParticleType.Blood, // TEST VALUE
                color = bloodColor,
                count = 1
            }.Schedule();

            spawnFlashSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
	}

    [BurstCompile]
    public partial struct ParticleSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity particle;
        public float3 position;
        public ParticleType particleType;
		public int count;

        public UnityEngine.Color color;

        void Execute(in ParticleData particleData)
        {
            for (int i = 0; i < count; i++)
            {
                Debug.Log("BOFA DEEZ PARTICLE NUTS");
                var newParticle = ecb.Instantiate(particle);

                var newTranslation = new Translation
                {
                    Value = position
                };

                if (particleType == ParticleType.SpawnFlash)
                {
                    color = UnityEngine.Color.white;
                }

                var newColor = new ParticleColorComponent
                {
                    Value = color
                };

                ecb.SetComponent(newParticle, newTranslation);
                ecb.SetComponent(newParticle, newColor);
            }
        }
    }
}
