using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Particle : IComponentData
{
    public ParticleType type;
    public float3 position;
    public float3 velocity;
    public float3 size;
    public float life;
    public float lifeDuration;
    public bool stuck;
    public Color color;
}