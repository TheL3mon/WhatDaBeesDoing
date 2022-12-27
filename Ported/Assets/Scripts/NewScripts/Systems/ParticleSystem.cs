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
            SpawnParticles(new float3(5, 0, 0), ParticleType.Blood, new float3(3,2,1));
        }
	}

    public void SpawnParticles(float3 _position, ParticleType _type, float3 _vel, float _velocityJitter = 6f, int count = 1)
    {
        // TEST VALUES
        UnityEngine.Color _bloodColor = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
        float _size = 0;
        float3 _velocity = 0;
        float _lifeDuration = 0;
        if (_type == ParticleType.Blood)
        {
            Debug.Log("BLOOD");
            _size = UnityEngine.Random.Range(0.1f, 0.2f);
            _velocity = _vel + (float3)UnityEngine.Random.insideUnitSphere * _velocityJitter;
            _lifeDuration = UnityEngine.Random.Range(3f, 5f);
        }
        else if (_type == ParticleType.SpawnFlash)
        {
            _size = UnityEngine.Random.Range(1f, 2f);
            _velocity = (float3)UnityEngine.Random.insideUnitSphere * 5f;
            _lifeDuration = UnityEngine.Random.Range(.25f, .5f);
        }

        Particle _particle = new Particle
        {
            type = _type,
            position = _position,
            velocity = _velocity,
            size = _size,
            life = 1f,
            lifeDuration = _lifeDuration,
            stuck = false,
            cachedMatrix = Matrix4x4.identity // TEMPORARY
        };
        //

        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        var spawnFlashSpawnJob = new ParticleSpawnJob
        {
            ecb = ecb,
            particle = _particlePrefab,
            particleValues = _particle,
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
        public Particle particleValues;
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
                    Value = particleValues.position
                };

                var newScale = new NonUniformScale
                {
                    Value = particleValues.size
                };

                if (particleValues.type == ParticleType.SpawnFlash)
                {
                    color = UnityEngine.Color.white;

                    ParticleSpawnTag spawnTag = new ParticleSpawnTag();
                    ecb.AddComponent(newParticle, spawnTag);
                }
                else if (particleValues.type == ParticleType.Blood)
                {
                    ParticleBloodTag bloodTag = new ParticleBloodTag();
                    ecb.AddComponent(newParticle, bloodTag);
                }

                var newColor = new ParticleColorComponent
                {
                    Value = color
                };

                ecb.SetComponent(newParticle, particleValues);
                ecb.SetComponent(newParticle, newTranslation);
                ecb.AddComponent(newParticle, newScale);
                ecb.SetComponent(newParticle, newColor);
            }
        }
    }
}
