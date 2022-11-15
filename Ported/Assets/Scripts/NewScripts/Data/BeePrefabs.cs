using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct BeePrefabs : IComponentData
{
    public Entity blueBee;
    public Entity yellowBee;
    public Entity mouseMarker;
    public Entity resource;
}
