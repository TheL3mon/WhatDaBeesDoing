using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class ParticleSystem : SystemBase
{
    float timer;
    public Unity.Mathematics.Random random;

    public ParticleData _particleData;
    public FieldData _fieldData;
    public Entity _particlePrefab;
    public readonly static ParticleSystem _instance;
    public Random rand;

    protected override void OnStartRunning()
    {
        //rand.InitState(25151);
        _particleData = GetSingleton<ParticleData>();
        _fieldData = GetSingleton<FieldData>();
        _particlePrefab = GetSingleton<ParticleData>().particlePrefab;
        //_instance = this;

        random.InitState(6969);
    }

    protected override void OnUpdate()
    {
        this.Enabled = true;
        timer = Time.DeltaTime;

        // TEST SPAWN
        if (Input.GetKeyDown(KeyCode.F))
        {
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
            // last property (velocity) should be set to -> bee.velocity * .35f
            SpawnParticles(ecb, new float3(5, 0, 0), ParticleType.Blood, new float3(1, -10, 1));
        }


        var ecb2 = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var particleBehaviorJob = new ParticleBehaviorJob
        {
            ecb = ecb2.AsParallelWriter(),
            fieldData = _fieldData,
            deltaTime = timer
        }.Schedule();
        particleBehaviorJob.Complete();

        ecb2.Playback(EntityManager);
        ecb2.Dispose();
    }

    public static void InstantiateSpawnFlashParticle(int entityIndex, ref EntityCommandBuffer.ParallelWriter ecb, Entity particlePrefab, float3 position, float3 velocity, ref Random random, float _velocityJitter = 6f)
    {
        //Particle particle = new Particle
        //{
        //    type = ParticleType.SpawnFlash,
        //    position = position,
        //    velocity = velocity + rand.NextFloat3Direction() * 5f,
        //    size = rand.NextFloat(1f, 2f),
        //    life = 1f,
        //    lifeDuration = rand.NextFloat(.25f,.5f),
        //    stuck = false
        //};

        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 2);

        Particle particle = new Particle
        {
            type = ParticleType.SpawnFlash,
            position = position,
            velocity = (velocity + dir * _velocityJitter) * 5,
            size = random.NextFloat(0.3f, 0.5f),
            life = 1f,
            lifeDuration = random.NextFloat(.25f, .5f),
            stuck = false
        };

        UnityEngine.Color color = UnityEngine.Color.yellow;

        InstantiateParticle(entityIndex, ecb, particle, color, particlePrefab);
    }

    public static void InstantiateBloodParticle(int entityIndex, ref EntityCommandBuffer ecb, Entity particlePrefab, float3 position, float3 velocity, ref Random random, float _velocityJitter = 6f)
    {
        //Particle particle = new Particle
        //{
        //    type = ParticleType.Blood,
        //    position = position,
        //    velocity = velocity + rand.NextFloat3Direction() * _velocityJitter,
        //    size = rand.NextFloat(0.1f, 0.2f),
        //    life = 1f,
        //    lifeDuration = rand.NextFloat(3f, 5f),
        //    stuck = false
        //};
        var dir = random.NextFloat3();
        var len = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        dir /= len;
        dir -= 0.5f;
        dir *= random.NextFloat(0, 2);


        Particle particle = new Particle
        {
            type = ParticleType.Blood,
            position = position,
            velocity = (velocity + dir * _velocityJitter)/3,
            size = 0.15f,
            life = 1f,
            lifeDuration = 4f,
            stuck = false
        };

        UnityEngine.Color color = UnityEngine.Color.red;

        InstantiateParticle(entityIndex, ecb, particle, color, particlePrefab);
    }

    public static void InstantiateParticle(int entityIndex, EntityCommandBuffer.ParallelWriter ecb, Particle particle, UnityEngine.Color color, Entity particlePrefab)
    {
        //Debug.Log("BOFA DEEZ PARTICLE NUTS");
        var newParticle = ecb.Instantiate(entityIndex, particlePrefab);
        //var newParticle = ecb.Instantiate(particlePrefab);

        var newTranslation = new Translation
        {
            Value = particle.position
        };

        var newScale = new NonUniformScale
        {
            Value = particle.size
        };

        if (particle.type == ParticleType.SpawnFlash)
        {
            color = UnityEngine.Color.white;

            ParticleSpawnTag spawnTag = new ParticleSpawnTag();
            ecb.AddComponent(entityIndex, newParticle, spawnTag);
        }
        else if (particle.type == ParticleType.Blood)
        {
            ParticleBloodTag bloodTag = new ParticleBloodTag();
            ecb.AddComponent(entityIndex, newParticle, bloodTag);
        }
        ParticleTag particleTag = new ParticleTag();
        ecb.AddComponent(entityIndex, newParticle, particleTag);

        particle.color = color;
        var newColor = new ParticleColorComponent
        {
            Value = color
        };

        ecb.SetComponent(entityIndex, newParticle, particle);
        ecb.SetComponent(entityIndex, newParticle, newTranslation);
        ecb.AddComponent(entityIndex, newParticle, newScale);
        ecb.SetComponent(entityIndex, newParticle, newColor);
    }

    public static void InstantiateParticle(int entityIndex, EntityCommandBuffer ecb, Particle particle, UnityEngine.Color color, Entity particlePrefab)
    {
        //Debug.Log("BOFA DEEZ PARTICLE NUTS");
        var newParticle = ecb.Instantiate(particlePrefab);
        //var newParticle = ecb.Instantiate(particlePrefab);

        var newTranslation = new Translation
        {
            Value = particle.position
        };

        var newScale = new NonUniformScale
        {
            Value = particle.size
        };

        if (particle.type == ParticleType.SpawnFlash)
        {
            color = UnityEngine.Color.white;

            ParticleSpawnTag spawnTag = new ParticleSpawnTag();
            ecb.AddComponent(newParticle, spawnTag);
        }
        else if (particle.type == ParticleType.Blood)
        {
            ParticleBloodTag bloodTag = new ParticleBloodTag();
            ecb.AddComponent(newParticle, bloodTag);
        }
        ParticleTag particleTag = new ParticleTag();
        ecb.AddComponent(newParticle, particleTag);

        particle.color = color;
        var newColor = new ParticleColorComponent
        {
            Value = color
        };

        ecb.SetComponent(newParticle, particle);
        ecb.SetComponent(newParticle, newTranslation);
        ecb.AddComponent(newParticle, newScale);
        ecb.SetComponent(newParticle, newColor);
    }

    public void SpawnParticles(EntityCommandBuffer _ecb, float3 _position, ParticleType _type, float3 _vel, float _velocityJitter = 6f, int count = 1)
    {
        // TEST VALUES

        UnityEngine.Color _bloodColor = UnityEngine.Random.ColorHSV(-.05f, .05f, .75f, 1f, .3f, .8f);
        float _size = 0;
        float3 _velocity = 0;
        float _lifeDuration = 0;
        if (_type == ParticleType.Blood)
        {
            //Debug.Log("BLOOD");
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
            stuck = false
        };
        //

        var spawnParticleJob = new ParticleSpawnJob
        {
            ecb = _ecb,
            particle = _particlePrefab,
            particleValues = _particle,
            color = _bloodColor,
            count = 1
        }.Schedule();

        spawnParticleJob.Complete();
        _ecb.Playback(EntityManager);
        _ecb.Dispose();
    }
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
            //Debug.Log("BOFA DEEZ PARTICLE NUTS");
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
            ParticleTag particleTag = new ParticleTag();
            ecb.AddComponent(newParticle, particleTag);

            particleValues.color = color;
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

[BurstCompile]
public partial struct ParticleBehaviorJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    [ReadOnly] public FieldData fieldData;
    public float deltaTime;

    void Execute(Entity particleEntity, [EntityInQueryIndex] int entityIndex, ref Particle particle, in ParticleTag particleTag)
    {
        if (!particle.stuck)
        {
            particle.velocity += new float3(0, -1, 0) * ((fieldData.gravity *9.8f) * deltaTime);
            particle.position += particle.velocity * deltaTime;

            if (System.Math.Abs(particle.position.x) > fieldData.size.x * .5f)
            {
                particle.position.x = fieldData.size.x * .5f * Mathf.Sign(particle.position.x);
                float splat = Mathf.Abs(particle.velocity.x * .3f) + 1f;
                particle.size.y *= splat;
                particle.size.z *= splat;
                particle.stuck = true;
            }
            if (System.Math.Abs(particle.position.y) > fieldData.size.y * .5f)
            {
                particle.position.y = fieldData.size.y * .5f * Mathf.Sign(particle.position.y);
                float splat = Mathf.Abs(particle.velocity.y * .3f) + 1f;
                particle.size.z *= splat;
                particle.size.x *= splat;
                particle.stuck = true;
            }
            if (System.Math.Abs(particle.position.z) > fieldData.size.z * .5f)
            {
                particle.position.z = fieldData.size.z * .5f * Mathf.Sign(particle.position.z);
                float splat = Mathf.Abs(particle.velocity.z * .3f) + 1f;
                particle.size.x *= splat;
                particle.size.y *= splat;
                particle.stuck = true;
            }


            Quaternion rotation = Quaternion.identity;
            float3 scale = particle.size * particle.life;
            float magnitude = 0;
            float pvX = particle.velocity.x;
            float pvY = particle.velocity.y;
            float pvZ = particle.velocity.z;
            float speedStretch = 0.25f; // this was set by default in the original
            if (particle.type == ParticleType.Blood)
            {
                rotation = Quaternion.LookRotation(particle.velocity);
                // magnitude = sqrt(x*x+y*y+z*z)
                magnitude = (pvX * pvX + pvY * pvY + pvZ * pvZ);
                scale.z *= 1f + magnitude * speedStretch;
            }
        }

        ecb.SetComponent(entityIndex, particleEntity, new Translation
        {
            Value = particle.position
        });

        ecb.SetComponent(entityIndex, particleEntity, new NonUniformScale
        {
            Value = particle.size
        });

        //if (particle.stuck)
        //{
        particle.life -= deltaTime / particle.lifeDuration;
        particle.color.a = particle.life;
        ecb.SetComponent(entityIndex, particleEntity, new ParticleColorComponent
        {
            Value = particle.color
        });
        //}

        ecb.SetComponent(entityIndex, particleEntity, particle);

        if (particle.life < 0f)
        {
            ecb.DestroyEntity(entityIndex, particleEntity);
        }
    }
}