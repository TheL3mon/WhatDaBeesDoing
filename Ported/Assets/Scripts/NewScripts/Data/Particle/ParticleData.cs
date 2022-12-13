using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ParticleData : IComponentData
{
    //public Mesh particleMesh;
    //public Material particleMaterial;
    public float speedStretch;

    // MOVED TO ParticleSystem
    //public List<BeeParticle> particles;
    //public Matrix4x4[][] matrices;
    //public Vector4[][] colors;
    //public List<BeeParticle> pooledParticles;

    public int activeBatch; // = 0;
    public int activeBatchSize; // = 0;

    //static ParticleManager instance;

    public int instancesPerBatch; // = 1023;
    public int maxParticleCount; // = 10 * instancesPerBatch;

    //public MaterialPropertyBlock matProps;
}
public enum ParticleType
{
    Blood,
    SpawnFlash
}

public struct BeeParticle : IComponentData
{
    public ParticleType type;
    public float3 position;
    public float3 velocity;
    public float3 size;
    public float life;
    public float lifeDuration;
    public float4 color;
    public bool stuck;
    public Matrix4x4 cachedMatrix;
}