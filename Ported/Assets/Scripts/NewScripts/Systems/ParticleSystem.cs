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
            SpawnParticles();
        }
	}

    public void SpawnParticles()
    {
        // TEST VALUES
        ParticleType _particleType = ParticleType.SpawnFlash;
        UnityEngine.Color _bloodColor = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
        float _size = 0;
        if (_particleType == ParticleType.Blood)
        {
            Debug.Log("BLOOD");
            _size = UnityEngine.Random.Range(0.1f, 0.2f);
        }
        else if (_particleType == ParticleType.SpawnFlash)
        {
            _size = UnityEngine.Random.Range(1f, 2f);
        }
        float3 _position = new float3(5, 0, 0);
        //

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        var spawnFlashSpawnJob = new ParticleSpawnJob
        {
            ecb = ecb,
            particle = _particlePrefab,
            position = _position,
            size = _size,
            particleType = _particleType,
            color = _bloodColor,
            count = 1
        }.Schedule();

        spawnFlashSpawnJob.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct ParticleSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity particle;
        public float3 position;
        public float size;
        public ParticleType particleType;
        public UnityEngine.Color color;
        public int count;


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

                var newScale = new NonUniformScale
                {
                    Value = new float3(size, size, size)
                };

                if (particleType == ParticleType.SpawnFlash)
                {
                    color = UnityEngine.Color.white;

                    ParticleSpawnTag spawnTag = new ParticleSpawnTag();
                    ecb.AddComponent(newParticle, spawnTag);
                }
                else if (particleType == ParticleType.Blood)
                {
                    ParticleBloodTag bloodTag = new ParticleBloodTag();
                    ecb.AddComponent(newParticle, bloodTag);
                }

                var newColor = new ParticleColorComponent
                {
                    Value = color
                };

                ecb.SetComponent(newParticle, newTranslation);
                ecb.AddComponent(newParticle, newScale);
                ecb.SetComponent(newParticle, newColor);
            }
        }
    }
}
