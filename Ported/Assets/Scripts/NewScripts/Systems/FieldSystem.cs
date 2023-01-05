using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/*

public partial class FieldSystem : SystemBase
{
    protected override void OnCreate()
    {
        var ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        Entities.WithAll<FieldData>().ForEach((Entity e, ref FieldData data, ref NonUniformScale sc)
        =>
        {
            //var newSC = new NonUniformScale { Value = data.size };
            //ecb.SetComponent(e, newSC);

            var newDatasize = new float3(sc.Value.x, sc.Value.y, sc.Value.z);


            var newFD = new FieldData
            {
                gravity = data.gravity,
                size = newDatasize
            };

            ecb.SetComponent(e, newFD);


            /*
            data.size.x = sc.Value.c0.x;
            data.size.y = sc.Value.c1.y;
            data.size.z = sc.Value.c2.z;


        }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
    protected override void OnUpdate()
    {
        this.Enabled = false;
    }
}*/
