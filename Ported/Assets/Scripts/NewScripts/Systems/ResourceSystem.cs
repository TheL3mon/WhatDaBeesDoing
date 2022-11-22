using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class ResourceSystem : SystemBase
{
    protected override void OnStartRunning()
    {
    }

    protected override void OnUpdate()
    {

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
