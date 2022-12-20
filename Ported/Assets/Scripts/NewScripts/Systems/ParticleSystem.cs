using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Entities.EntityQueryBuilder;
using static UnityEditor.PlayerSettings;
using static UnityEngine.ParticleSystem;

public partial class ParticleSystem : SystemBase
{
    float timer;

    public ParticleData _particleData;

    // TEMPORARY - Remove and do it in ParticleData once we know how to do arrays
    //public List<BeeParticle> particles;
    public NativeArray<BeeParticle> particles;
    //public Matrix4x4[][] matrices;
    public NativeArray<NativeArray<Matrix4x4>> matrices;
    //public Vector4[][] colors;
    public NativeArray<NativeArray<float4>> colors;
    //public List<BeeParticle> pooledParticles;
    public NativeArray<BeeParticle> pooledParticles;
    //

    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnStartRunning()
    {
        _particleData = GetSingleton<ParticleData>();

        _particleData.activeBatch = 0;
        _particleData.activeBatchSize = 0;
        _particleData.instancesPerBatch = 1023;
        _particleData.maxParticleCount = 10 * _particleData.instancesPerBatch;

        // ORIGINAL Awake()
		//particles = new List<BeeParticle>();
        particles = new NativeArray<BeeParticle>(1000, Unity.Collections.Allocator.Persistent); // TODO: this should be dynamic, the size is temporary
		
		//pooledParticles = new List<BeeParticle>();
        pooledParticles = new NativeArray<BeeParticle>(1000, Unity.Collections.Allocator.Persistent); // TODO: this should be dynamic, the size is temporary

        //matrices = new Matrix4x4[_particleData.maxParticleCount / _particleData.instancesPerBatch + 1][];
        matrices = new NativeArray<NativeArray<Matrix4x4>>(_particleData.maxParticleCount / _particleData.instancesPerBatch + 1, Unity.Collections.Allocator.Persistent);
		
		//colors = new Vector4[_particleData.maxParticleCount / _particleData.instancesPerBatch + 1][];
        colors = new NativeArray<NativeArray<float4>>(_particleData.maxParticleCount / _particleData.instancesPerBatch + 1, Unity.Collections.Allocator.Persistent);

        //matrices[0] = new Matrix4x4[_particleData.instancesPerBatch];
        matrices[0] = new NativeArray<Matrix4x4>(_particleData.instancesPerBatch, Unity.Collections.Allocator.Persistent);
        
		//colors[0] = new Vector4[_particleData.instancesPerBatch];
        colors[0] = new NativeArray<float4>(_particleData.instancesPerBatch, Unity.Collections.Allocator.Persistent);

        _particleData.activeBatch = 0;
        _particleData.activeBatchSize = 0;

		//matProps = new MaterialPropertyBlock();
		//matProps.SetVectorArray("_Color", new Vector4[instancesPerBatch]);
    }

    protected override void OnUpdate()
    {
        timer += Time.DeltaTime;

        // SPAWNFLASH test
        if (Input.GetKeyDown(KeyCode.F))
		{
            var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
			var spawnFlashSpawnJob = new ParticleSpawnJob
            {
				ecb = ecb,
                position = new float3(0, 0, 0),
                type = ParticleType.SpawnFlash,
                velocity = 0,
                velocityJitter = 6f,
                count = 1
            }.Schedule();

            spawnFlashSpawnJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

		/*
        // ORIGINAL Update()
        for (int j = 0; j <= _particleData.activeBatch; j++)
        {
            int batchSize = _particleData.instancesPerBatch;
            if (j == _particleData.activeBatch)
            {
                batchSize = _particleData.activeBatchSize;
            }
            int batchOffset = j * _particleData.instancesPerBatch;
            //Matrix4x4[] batchMatrices = matrices[j];
            NativeArray<Matrix4x4> batchMatrices = matrices[j];
            //Vector4[] batchColors = colors[j];
            NativeArray<float4> batchColors = colors[j];
            for (int i = 0; i < batchSize; i++)
            {
                BeeParticle particle = particles[i + batchOffset];

                if (particle.stuck)
                {
                    batchMatrices[i] = particle.cachedMatrix;
                }
                else
                {
                    Quaternion rotation = Quaternion.identity;
                    Vector3 scale = particle.size * particle.life;
                    if (particle.type == ParticleType.Blood)
                    {
                        rotation = Quaternion.LookRotation(particle.velocity);
                        //scale.z *= 1f + particle.velocity.magnitude * speedStretch; // TODO: calculate magnitude differently
                    }
                    batchMatrices[i] = Matrix4x4.TRS(particle.position, rotation, scale);
                }

                float4 color = particle.color;
				color[3] = particle.life;
                batchColors[i] = color;
            }
        }

		for (int i = 0; i <= _particleData.activeBatch; i++)
		{
			int batchSize = _particleData.instancesPerBatch;
			if (i == _particleData.activeBatch)
			{
				batchSize = _particleData.activeBatchSize;
			}
			if (batchSize > 0)
			{
				// TODO: once we have materials working
				//matProps.SetVectorArray("_Color", colors[i]);
				//Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matrices[i], batchSize, matProps);
			}
		}


		/*
            Entities.ForEach((ref Translation translation, in Rotation rotation) => {
            // Implement the work to perform for each entity here.
            // You should only access data that is local or that is a
            // field on this job. Note that the 'rotation' parameter is
            // marked as 'in', which means it cannot be modified,
            // but allows this job to run in parallel with other jobs
            // that want to read Rotation component data.
            // For example,
            //     translation.Value += math.mul(rotation.Value, new float3(0, 0, 1)) * deltaTime;
        }).Schedule();
		*/
	}

    [BurstCompile]
    public partial struct ParticleSpawnJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity particle;
        public float3 position;
		public ParticleType type;
		public float3 velocity;
		public float velocityJitter;
		public int count;

        void Execute(ref BeeParticle beeParticleData)
        {
			// setting the data
			beeParticleData.type = type;
			beeParticleData.position = position;
			beeParticleData.velocity = velocity;
			beeParticleData.size = new float3(1,1,1); // test
			beeParticleData.life = 0; //test
			beeParticleData.lifeDuration = 10; //test
            beeParticleData.color = new float4(1,1,1,0); //test
			beeParticleData.stuck = false;
			beeParticleData.cachedMatrix = new Matrix4x4(); //test
			//

            for (int i = 0; i < count; i++)
            {

				var newParticle = ecb.Instantiate(particle);

                var newTranslation = new Translation
                {
                    Value = position
                };

                ecb.SetComponent(newParticle, newTranslation);
            }
        }

        /*
		void _SpawnParticle(Vector3 position, ParticleType type, Vector3 velocity, float velocityJitter) {
			if (particles.Count==maxParticleCount) {
				return;
			}
			BeeParticle particle;
			if (pooledParticles.Count == 0) {
				particle = new BeeParticle();
			} else {
				particle = pooledParticles[pooledParticles.Count - 1];
				pooledParticles.RemoveAt(pooledParticles.Count - 1);

				particle.stuck = false;
			}
			particle.type = type;
			particle.position = position;
			particle.life = 1f;
			if (type==ParticleType.Blood) {
				particle.velocity = velocity+ Random.insideUnitSphere * velocityJitter;
				particle.lifeDuration = Random.Range(3f,5f);
				particle.size = Vector3.one*Random.Range(.1f,.2f);
				particle.color = Random.ColorHSV(-.05f,.05f,.75f,1f,.3f,.8f);
			} else if (type==ParticleType.SpawnFlash) {
				particle.velocity = Random.insideUnitSphere * 5f;
				particle.lifeDuration = Random.Range(.25f,.5f);
				particle.size = Vector3.one*Random.Range(1f,2f);
				particle.color = Color.white;
			}

			particles.Add(particle);

			if (activeBatchSize == instancesPerBatch) {
				activeBatch++;
				activeBatchSize = 0;
				if (matrices[activeBatch]==null) {
					matrices[activeBatch]=new Matrix4x4[instancesPerBatch];
					colors[activeBatch]=new Vector4[instancesPerBatch];
				}
			}
			activeBatchSize++;
		} 
		*/
    }

    /*
    FROM THE OLD SCRIPT (ParticleManager.cs)
    ########################################

    public static void SpawnParticle(Vector3 position,ParticleType type,Vector3 velocity,float velocityJitter=6f,int count=1) {
		for (int i = 0; i < count; i++) {
			instance._SpawnParticle(position,type,velocity,velocityJitter);
		}
	}

	VVVVVVVVVVVVVVVVVVV
	VVV WE ARE HERE VVV
	VVVVVVVVVVVVVVVVVVV

	void _SpawnParticle(Vector3 position, ParticleType type, Vector3 velocity, float velocityJitter) {
		if (particles.Count==maxParticleCount) {
			return;
		}
		BeeParticle particle;
		if (pooledParticles.Count == 0) {
			particle = new BeeParticle();
		} else {
			particle = pooledParticles[pooledParticles.Count - 1];
			pooledParticles.RemoveAt(pooledParticles.Count - 1);

			particle.stuck = false;
		}
		particle.type = type;
		particle.position = position;
		particle.life = 1f;
		if (type==ParticleType.Blood) {
			particle.velocity = velocity+ Random.insideUnitSphere * velocityJitter;
			particle.lifeDuration = Random.Range(3f,5f);
			particle.size = Vector3.one*Random.Range(.1f,.2f);
			particle.color = Random.ColorHSV(-.05f,.05f,.75f,1f,.3f,.8f);
		} else if (type==ParticleType.SpawnFlash) {
			particle.velocity = Random.insideUnitSphere * 5f;
			particle.lifeDuration = Random.Range(.25f,.5f);
			particle.size = Vector3.one*Random.Range(1f,2f);
			particle.color = Color.white;
		}

		particles.Add(particle);

		if (activeBatchSize == instancesPerBatch) {
			activeBatch++;
			activeBatchSize = 0;
			if (matrices[activeBatch]==null) {
				matrices[activeBatch]=new Matrix4x4[instancesPerBatch];
				colors[activeBatch]=new Vector4[instancesPerBatch];
			}
		}
		activeBatchSize++;
	}


	VVVVVVVVVVVVVVVVVVVVVVV
	// DONE PORTING Awake()
	VVVVVVVVVVVVVVVVVVVVVVV

	private void Awake() {
		instance = this;

		particles = new List<BeeParticle>();
		pooledParticles = new List<BeeParticle>();
		matrices = new Matrix4x4[maxParticleCount/instancesPerBatch+1][];
		colors = new Vector4[maxParticleCount/instancesPerBatch+1][];

		matrices[0]=new Matrix4x4[instancesPerBatch];
		colors[0]=new Vector4[instancesPerBatch];
		activeBatch = 0;
		activeBatchSize = 0;

		matProps = new MaterialPropertyBlock();
		matProps.SetVectorArray("_Color",new Vector4[instancesPerBatch]);
	}
	
	void FixedUpdate () {
		float deltaTime = Time.deltaTime;
		for (int i=0;i<particles.Count;i++) {
			BeeParticle particle = particles[i];
			if (!particle.stuck) {
				particle.velocity += Vector3.up * (Field.gravity * deltaTime);
				particle.position += particle.velocity * deltaTime;
				
				if (System.Math.Abs(particle.position.x) > Field.size.x * .5f) {
					particle.position.x = Field.size.x * .5f * Mathf.Sign(particle.position.x);
					float splat = Mathf.Abs(particle.velocity.x*.3f) + 1f;
					particle.size.y *= splat;
					particle.size.z *= splat;
					particle.stuck = true;
				}
				if (System.Math.Abs(particle.position.y) > Field.size.y * .5f) {
					particle.position.y = Field.size.y * .5f * Mathf.Sign(particle.position.y);
					float splat = Mathf.Abs(particle.velocity.y * .3f) + 1f;
					particle.size.z *= splat;
					particle.size.x *= splat;
					particle.stuck = true;
				}
				if (System.Math.Abs(particle.position.z) > Field.size.z * .5f) {
					particle.position.z = Field.size.z * .5f * Mathf.Sign(particle.position.z);
					float splat = Mathf.Abs(particle.velocity.z * .3f) + 1f;
					particle.size.x *= splat;
					particle.size.y *= splat;
					particle.stuck = true;
				}

				if (particle.stuck) {
					particle.cachedMatrix = Matrix4x4.TRS(particle.position,Quaternion.identity,particle.size);
				}
			}

			particle.life -= deltaTime / particle.lifeDuration;
			if (particle.life < 0f) {
				activeBatchSize--;
				if (activeBatchSize==0 && activeBatch>0) {
					activeBatch--;
					activeBatchSize = instancesPerBatch;
				}

				pooledParticles.Add(particle);
				particles.RemoveAt(i);
				i--;
			}
		}
	}

	
	VVVVVVVVVVVVVVVVVVVVVVVV
	// DONE PORTING Update()
	VVVVVVVVVVVVVVVVVVVVVVVV

	void Update() {
		for (int j = 0; j <= activeBatch; j++) {
			int batchSize = instancesPerBatch;
			if (j == activeBatch) {
				batchSize = activeBatchSize;
			}
			int batchOffset = j * instancesPerBatch;
			Matrix4x4[] batchMatrices = matrices[j];
			Vector4[] batchColors = colors[j];
			for (int i = 0; i < batchSize; i++) {
				BeeParticle particle = particles[i + batchOffset];

				if (particle.stuck) {
					batchMatrices[i] = particle.cachedMatrix;
				} else {
					Quaternion rotation = Quaternion.identity;
					Vector3 scale = particle.size * particle.life;
					if (particle.type == ParticleType.Blood) {
						rotation = Quaternion.LookRotation(particle.velocity);
						scale.z *= 1f + particle.velocity.magnitude * speedStretch;
					}
					batchMatrices[i] = Matrix4x4.TRS(particle.position,rotation,scale);
				}

				Color color = particle.color;
				color.a = particle.life;
				batchColors[i] = color;
			}
		}

		for (int i = 0; i <= activeBatch; i++) {
			int batchSize = instancesPerBatch;
			if (i==activeBatch) {
				batchSize = activeBatchSize;
			}
			if (batchSize > 0) {
				matProps.SetVectorArray("_Color",colors[i]);
				Graphics.DrawMeshInstanced(particleMesh,0,particleMaterial,matrices[i],batchSize,matProps);
			}
		}
	}
    */
}
