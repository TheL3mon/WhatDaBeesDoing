using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour {
	public Mesh resourceMesh;
	public Material resourceMaterial;
	public float resourceSize;
	public float snapStiffness;
	public float carryStiffness;
	public float spawnRate=.1f;
	public int beesPerResource;
	[Space(10)]
	public int startResourceCount;

	List<Resource> resources;
	List<Matrix4x4> matrices;
	Vector2Int gridCounts;
	Vector2 gridSize;
	Vector2 minGridPos;

	int[,] stackHeights;

	float spawnTimer = 0f;

	public static ResourceManager instance;

	//Tries to get a random resource, if the resorce is not at the top of the stack it is in, it can't be targeted by a bee
	public static Resource TryGetRandomResource() {
		if (instance.resources.Count==0) {
			return null;
		} else {
			Resource resource = instance.resources[Random.Range(0,instance.resources.Count)];
			int stackHeight = instance.stackHeights[resource.gridX,resource.gridY];
			if (resource.holder == null || resource.stackIndex==stackHeight-1) {
				return resource;
			} else {
				return null;
			}
		}
	}

	//Checks if the resource is at the top of it's current stack
	public static bool IsTopOfStack(Resource resource) {
		int stackHeight = instance.stackHeights[resource.gridX,resource.gridY];
		return resource.stackIndex == stackHeight - 1;
	}

	//gets its position on the stack
	Vector3 GetStackPos(int x, int y, int height) {
		return new Vector3(minGridPos.x+x*gridSize.x,-Field.size.y*.5f+(height+.5f)*resourceSize,minGridPos.y+y*gridSize.y);
	}

	//Snaps the resources together so its a solid stack that doesn't fall over
	Vector3 NearestSnappedPos(Vector3 pos) {
		int x, y;
		GetGridIndex(pos,out x,out y);
		return new Vector3(minGridPos.x + x * gridSize.x,pos.y,minGridPos.y + y * gridSize.y);
	}
	void GetGridIndex(Vector3 pos,out int gridX,out int gridY) {
		gridX=Mathf.FloorToInt((pos.x - minGridPos.x + gridSize.x * .5f) / gridSize.x);
		gridY=Mathf.FloorToInt((pos.z - minGridPos.y + gridSize.y * .5f) / gridSize.y);

		gridX = Mathf.Clamp(gridX,0,gridCounts.x - 1);
		gridY = Mathf.Clamp(gridY,0,gridCounts.y - 1);
	}

	//Spawn a resource with a random position
	void SpawnResource() {
		Vector3 pos = new Vector3(minGridPos.x * .25f + Random.value * Field.size.x * .25f,Random.value * 10f,minGridPos.y + Random.value * Field.size.z);
		SpawnResource(pos);
	}
	//spawn a resource with a known position
	void SpawnResource(Vector3 pos) {
		Resource resource = new Resource(pos);

		resources.Add(resource);
		matrices.Add(Matrix4x4.identity);
	}
	//Delete a specific resource
	void DeleteResource(Resource resource) {
		resource.dead = true;
		resources.Remove(resource);
		matrices.RemoveAt(matrices.Count - 1);
	}

	//Allows a bee to grab the resource
	public static void GrabResource(Bee bee, Resource resource) {
		resource.holder = bee;
		resource.stacked = false;
		instance.stackHeights[resource.gridX,resource.gridY]--;
	}

	void Awake() {
		instance = this;
	}

	//Initial setup of the manager
	void Start () {
		resources = new List<Resource>();
		matrices = new List<Matrix4x4>();

		gridCounts = Vector2Int.RoundToInt(new Vector2(Field.size.x,Field.size.z) / resourceSize);
		gridSize = new Vector2(Field.size.x/gridCounts.x,Field.size.z/gridCounts.y);
		minGridPos = new Vector2((gridCounts.x-1f)*-.5f*gridSize.x,(gridCounts.y-1f)*-.5f*gridSize.y);
		stackHeights = new int[gridCounts.x,gridCounts.y];

		//Spawns the initial resources at random positions
		for (int i=0;i<startResourceCount;i++) {
			SpawnResource();
		}
	}

	void Update() {
		//spawn a resource on the mouse if there is less tha 1000 resources (thats a lot of fucking resources dawg)
		if (resources.Count < 1000 && MouseRaycaster.isMouseTouchingField) {
			if (Input.GetKey(KeyCode.Mouse0)) {
				spawnTimer += Time.deltaTime;
				while (spawnTimer > 1f/spawnRate) {
					spawnTimer -= 1f/spawnRate;
					SpawnResource(MouseRaycaster.worldMousePosition);
				}
			}
		}

		//loop through every single resource
		for (int i=0;i<resources.Count;i++) {
			Resource resource = resources[i];
			if (resource.holder != null) {
				//reset holder if holder is dead
				if (resource.holder.dead) {
					resource.holder = null;
				//if holder is not dead set resource position and velocity to matching the holder (or atleast close to)
				} else {
					Vector3 targetPos = resource.holder.position - Vector3.up * (resourceSize + resource.holder.size)*.5f;
					resource.position = Vector3.Lerp(resource.position,targetPos,carryStiffness * Time.deltaTime);
					resource.velocity = resource.holder.velocity;
				}
			//If the resource is not stacked math is made to figure out where it is placed int the world, and if its moving or not
			} else if (resource.stacked == false) {
				resource.position = Vector3.Lerp(resource.position,NearestSnappedPos(resource.position),snapStiffness * Time.deltaTime);
				resource.velocity.y += Field.gravity * Time.deltaTime;
				resource.position += resource.velocity * Time.deltaTime;
				GetGridIndex(resource.position,out resource.gridX,out resource.gridY);
				float floorY = GetStackPos(resource.gridX,resource.gridY,stackHeights[resource.gridX,resource.gridY]).y;
				for (int j = 0; j < 3; j++) {
					if (System.Math.Abs(resource.position[j]) > Field.size[j] * .5f) {
						resource.position[j] = Field.size[j] * .5f * Mathf.Sign(resource.position[j]);
						resource.velocity[j] *= -.5f;
						resource.velocity[(j + 1) % 3] *= .8f;
						resource.velocity[(j + 2) % 3] *= .8f;
					}
				}
				/*
				 * This section is used for checking if a resource has been dropped within either side of the field
				 * If it is within one of the side, and on the floor, new bees are spawned for the correct team, amount of bees spawned, and their location is decided form the resource values
				 * A spawn flash particle is spawned when new bees are spawned
				 * Lastly the resource is deleted
				 */
				if (resource.position.y < floorY) {
					resource.position.y = floorY;
					if (Mathf.Abs(resource.position.x) > Field.size.x * .4f) {
						int team = 0;
						if (resource.position.x > 0f) {
							team = 1;
						}
						for (int j = 0; j < beesPerResource; j++) {
							BeeManager.SpawnBee(resource.position,team);
						}
						ParticleManager.SpawnParticle(resource.position,ParticleType.SpawnFlash,Vector3.zero,6f,5);
						DeleteResource(resource);
					} else {
						resource.stacked = true;
						resource.stackIndex = stackHeights[resource.gridX,resource.gridY];
						if ((resource.stackIndex + 1) * resourceSize < Field.size.y) {
							stackHeights[resource.gridX,resource.gridY]++;
						} else {
							DeleteResource(resource);
						}
						
					}
				}
			}
		}

		Vector3 scale = new Vector3(resourceSize,resourceSize * .5f,resourceSize);
		for (int i=0;i<resources.Count;i++) {
			matrices[i] = Matrix4x4.TRS(resources[i].position,Quaternion.identity,scale);
		}
		Graphics.DrawMeshInstanced(resourceMesh,0,resourceMaterial,matrices);
	}

	private void OnDrawGizmosSelected() {
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(Vector3.zero,Field.size);
	}
}
