using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ResourceData : IComponentData
{
	public int beesPerResource;

	//public Mesh resourceMesh;
	//public Material resourceMaterial;
	public float resourceSize;
	public float snapStiffness;
	public float carryStiffness;
	public float spawnRate; // 0.1f
							//[Space(10)]
	public int startResourceCount;

	//public List<Resource> resources;
	//public List<Matrix4x4> matrices;
	public int2 gridCounts;
	public float2 gridSize;
	public float2 minGridPos;

	//public NativeArray<int> stackHeights;

	public float spawnTimer; // 0

	//public static ResourceSystem instance;
}

public struct Resource : IComponentData
{
	public float3 position;
	public Entity holder;
	public bool stacked;
	public bool topOfStack;
	public int stackIndex;
	public int gridX;
	public int gridY;
	//public Bee holder;
	public float3 velocity;
	public bool dead;

    //public Resource(Vector3 myPosition)
    //{
    //	position = myPosition;
    //}
}
