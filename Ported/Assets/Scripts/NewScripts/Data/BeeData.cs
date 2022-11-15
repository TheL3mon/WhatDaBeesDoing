using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct BeeData : IComponentData
{
    public int team;
    public float flightJitter;
    public float teamAttraction;
}
