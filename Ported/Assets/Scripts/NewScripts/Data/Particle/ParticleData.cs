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

    //public List<BeeParticle> particles;
    //public Matrix4x4[][] matrices;
    //public Vector4[][] colors;
    //public DynamicBuffer<DynamicBuffer<int>> dynamicShit;

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
