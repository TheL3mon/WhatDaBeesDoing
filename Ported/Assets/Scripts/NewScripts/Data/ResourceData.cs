using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ResourceData : IComponentData
{
    public int beesPerResource;

	public Mesh resourceMesh;
	public Material resourceMaterial;
	public float resourceSize;
	public float snapStiffness;
	public float carryStiffness;
	public float spawnRate; // 0.1f
	[Space(10)]
	public int startResourceCount;

	public List<Resource> resources;
	public List<Matrix4x4> matrices;
	public Vector2Int gridCounts;
	public Vector2 gridSize;
	public Vector2 minGridPos;

	public int[,] stackHeights;

	float spawnTimer; // 0

	//public static ResourceManager instance;
}

public struct Resource : IComponentData
{
	public Vector3 position;
	public bool stacked;
	public int stackIndex;
	public int gridX;
	public int gridY;
	public Bee holder;
	public Vector3 velocity;
	public bool dead;

	//public Resource(Vector3 myPosition)
	//{
	//	position = myPosition;
	//}
}
