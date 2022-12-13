using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct FieldData : IComponentData
{
    public float3 size;
    public float gravity;
    //public DynamicBuffer<int> stackHeights;
    public DynamicBuffer<int> stackHeights;
    //public DynamicBuffer<>
}

public struct StackBufferElement : IBufferElementData
{
    //public Dynamic

}

public struct StackBufferElementLookup : IBufferElementData
{
    public int2 index;
    public int height;
}