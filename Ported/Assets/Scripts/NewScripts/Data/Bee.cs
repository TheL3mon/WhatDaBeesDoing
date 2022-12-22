using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Bee : IComponentData
{
    public Entity enemyTarget;
    public Entity resourceTarget;

    public bool dead;
    public float deathTimer;

    public bool isAttacking;
    public bool isHoldingResource;

    public Matrix4x4 beeMatrix; //Matrix that is used for transforming the bee.
    //FIXME In original version they use batching, is necessary in our case? Is batching automatic in DOTS?

    public float4 beeColor;

    public int team;
}
