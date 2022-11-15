using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ResourceData : IComponentData
{
    public int beesPerResource;
}
