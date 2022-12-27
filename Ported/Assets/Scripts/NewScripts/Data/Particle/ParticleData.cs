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

    public int instancesPerBatch; // = 1023;
    public int maxParticleCount; // = 10 * instancesPerBatch;

    //public MaterialPropertyBlock matProps;

    public Entity particlePrefab;
}

public enum ParticleType
{
    Blood,
    SpawnFlash
}