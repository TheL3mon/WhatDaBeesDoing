using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BeeData :  IComponentData
{
    public float3 position;
    public float3 velocity;
    //public Vector3 smoothPosition;
    //public Vector3 smoothDirection;
    public int team;
    public float size;
    //public Bee enemyTarget;
    //public Resource resourceTarget;

    public bool dead;
    //public float deathTimer = 1f;
    //public bool isAttacking;
    //public bool isHoldingResource;
    public int index;
}
