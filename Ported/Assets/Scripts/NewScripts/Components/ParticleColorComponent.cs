using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

[GenerateAuthoringComponent]
[MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
public struct ParticleColorComponent : IComponentData
{
    public Color Value;
}
