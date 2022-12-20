using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class ResourceSystem : SystemBase
{
    private EntityCommandBuffer _ecb;
    private Entity _resourcePrefab;
    private FieldData _fieldData;
    private ResourceData _resourceData;
    private NativeArray<int> _stackHeights;

    //public int[,] stackHeights;

    protected override void OnStartRunning()
    {
        _resourcePrefab = GetSingleton<BeePrefabs>().resource;
        _resourceData = GetSingleton<ResourceData>();
        _fieldData = GetSingleton<FieldData>();
        _ecb = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);
        _fieldData.stackHeights = new DynamicBuffer<int>();

        int size_x = _resourceData.gridCounts[0];
        int size_y = _resourceData.gridCounts[1];

        _stackHeights = new NativeArray<int>(size_x*size_y, Allocator.Persistent);
        Debug.Log("stackHeights: " + (size_x * size_y));
        //stackHeights = new int[_resourceData.gridCounts[0],_resourceData.gridCounts[1]];
    }

    protected override void OnUpdate()
    {
        var ecb2 = new EntityCommandBuffer(World.UpdateAllocator.ToAllocator);

        var dt = Time.DeltaTime;

        var resource = new FallingResourceJob
        {
            dt = dt,
            ecb = ecb2,
            rd = _resourceData,
            fd = _fieldData,
            stackHeights = _stackHeights
        }.Schedule();
        resource.Complete();
        ecb2.Playback(EntityManager);
        ecb2.Dispose();
    }




    //[BurstCompile]
    public partial struct FallingResourceJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public Entity resourcePrefab;
        public ResourceData rd;
        public FieldData fd;
        public float dt;
        public bool onFloor;
        //public int height;
        public NativeArray<int> stackHeights;

        void Execute(Entity resourceEntity, ref Resource resource, in FallingResourceTag frt)
        {
            Debug.Log("Resource exists");
            
            //var resource = ecb.

            //Apply gravity on resource
            var g = fd.gravity * new float3(0.0f, -1.0f, 0.0f);
            resource.velocity += g * dt;
            resource.position += resource.velocity;
            //Debug.Log("dt: "+ dt);
            //Debug.Log("resource position: " + resource.position);

            //Snap to grid
            var gridIndex = GetGridIndex(resource);
            resource.gridX = gridIndex[0];
            resource.gridY = gridIndex[1];

            int index = resource.gridX + resource.gridY * rd.gridCounts.y;

            //Debug.Log("gridPos: (" + resource.gridX + ", " + resource.gridY + ") index: " + index);

            //int height = stackHeights[index];
            int height = 0;

            resource.position.x = resource.gridX;
            resource.position.z = resource.gridY;

            float floorY = GetStackPos(resource.gridX, resource.gridY, height).y;

            //Debug.Log("Floor y: " + floorY);

            if (resource.position.y < floorY)
            {
                resource.position.y = floorY;
                resource.velocity = float3.zero;
                stackHeights[index]++;
                //ecb.Remove
                //ecb.SetComponent(resourceEntity, frt);
                ecb.RemoveComponent(resourceEntity, typeof(FallingResourceTag));

            }

            ecb.SetComponent(resourceEntity, new Translation
            {
                Value = resource.position
            });
        }

        int2 GetGridIndex(Resource resource)
        {
            var pos = resource.position;
            var minGridPos = rd.minGridPos;
            var gridSize = rd.gridSize;
            var gridCounts = rd.gridCounts;

            var gridX = Mathf.FloorToInt((pos.x - minGridPos.x + gridSize.x * .5f) / gridSize.x);
            var gridY = Mathf.FloorToInt((pos.z - minGridPos.y + gridSize.y * .5f) / gridSize.y);

            gridX = Mathf.Clamp(gridX, 0, gridCounts.x - 1);
            gridY = Mathf.Clamp(gridY, 0, gridCounts.y - 1);

            return new int2(gridX, gridY);
        }

        float3 GetStackPos(int x, int y, int height)
        {
            var minGridPos = rd.minGridPos;
            var gridSize = rd.gridSize;
            var gridCounts = rd.gridCounts;
            //return new float3(minGridPos.x + x * gridSize.x, -fd.size.y * .5f + (height + .5f) * rd.resourceSize, minGridPos.y + y * gridSize.y);
            return new float3(minGridPos.x + x * gridSize.x, -fd.size.y * .5f + (height + .5f) * rd.resourceSize, minGridPos.y + y * gridSize.y);
        }

        //float3 GetMovement(float3 position)
        //{
        //    float3 g = fieldData.gravity;



        //    return position * g * dt;
        //}


    }


    //public Resource TryGetRandomResource(ref ResourceData resourceData)
    //{
    //    //if (instance.resources.Count==0) {
    //    //	return null;
    //    //} else {
    //    //	Resource resource = instance.resources[Random.Range(0,instance.resources.Count)];
    //    //	int stackHeight = instance.stackHeights[resource.gridX,resource.gridY];
    //    //	if (resource.holder == null || resource.stackIndex==stackHeight-1) {
    //    //		return resource;
    //    //	} else {
    //    //		return null;
    //    //	}
    //    //}

    //    if (resourceData.resources.Count != 0)
    //    {
    //        /*

    //        var randomResource = resourceData.resources[Random.Range(0, resourceData.resources.Count)];
    //        int stackHeight = resourceData.stackHeights[randomResource.gridX, randomResource.gridY];

    //        if (randomResource.holder == null || randomResource.stackIndex == stackHeight - 1)
    //        {
    //            return randomResource;
    //        }

    //        else
    //        {
    //            return new Resource(); // null
    //        }*/
    //        return new Resource(); // null
    //    }
    //    else
    //    {
    //        return new Resource(); // null
    //    }
    //}

    //public static bool IsTopOfStack(Resource resource, ref ResourceData resourceData)
    //{
    //    int stackHeight = resourceData.stackHeights[resource.gridX, resource.gridY];
    //    return resource.stackIndex == stackHeight - 1;
    //}

    //Vector3 GetStackPos(int x, int y, int height, ref ResourceData resourceData)
    //{
    //    return new Vector3(resourceData.minGridPos.x + x * resourceData.gridSize.x, -Field.size.y * .5f + (height + .5f) * resourceData.resourceSize, resourceData.minGridPos.y + y * resourceData.gridSize.y);
    //}
    //public Resource TryGetRandomResource(ref ResourceData resourceData)
    //{
    //    //if (instance.resources.Count==0) {
    //    //	return null;
    //    //} else {
    //    //	Resource resource = instance.resources[Random.Range(0,instance.resources.Count)];
    //    //	int stackHeight = instance.stackHeights[resource.gridX,resource.gridY];
    //    //	if (resource.holder == null || resource.stackIndex==stackHeight-1) {
    //    //		return resource;
    //    //	} else {
    //    //		return null;
    //    //	}
    //    //}

    //    if (resourceData.resources.Count != 0)
    //    {
    //        /*

    //        var randomResource = resourceData.resources[Random.Range(0, resourceData.resources.Count)];
    //        int stackHeight = resourceData.stackHeights[randomResource.gridX, randomResource.gridY];

    //        if (randomResource.holder == null || randomResource.stackIndex == stackHeight - 1)
    //        {
    //            return randomResource;
    //        }

    //        else
    //        {
    //            return new Resource(); // null
    //        }*/
    //        return new Resource(); // null
    //    }
    //    else
    //    {
    //        return new Resource(); // null
    //    }
    //}

    //public static bool IsTopOfStack(Resource resource, ref ResourceData resourceData)
    //{
    //    int stackHeight = resourceData.stackHeights[resource.gridX, resource.gridY];
    //    return resource.stackIndex == stackHeight - 1;
    //}

    //Vector3 GetStackPos(int x, int y, int height, ref ResourceData resourceData)
    //{
    //    return new Vector3(resourceData.minGridPos.x + x * resourceData.gridSize.x, -Field.size.y * .5f + (height + .5f) * resourceData.resourceSize, resourceData.minGridPos.y + y * resourceData.gridSize.y);
    //}
}
