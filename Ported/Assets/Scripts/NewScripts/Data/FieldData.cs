using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct FieldData : IComponentData
{
    public float3 size;
    public float gravity;
}
