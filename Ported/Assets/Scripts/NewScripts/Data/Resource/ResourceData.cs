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

	public float resourceSize;
	public float snapStiffness;
	public float carryStiffness;
	public float spawnRate; // 0.1f
							//[Space(10)]
	public int startResourceCount;

	public int2 gridCounts;
	public float2 gridSize;
	public float2 minGridPos;

	public float spawnTimer; // 0
}

public struct Resource : IComponentData
{
	public float3 position;
	public Entity holder;

	public int holderTeam;

	public int stackIndex;
	public int gridX;
	public int gridY;

	public float3 velocity;
	public bool dead;
	public int height;
}
