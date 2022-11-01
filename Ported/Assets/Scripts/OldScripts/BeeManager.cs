using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeeManager : MonoBehaviour {
	public Mesh beeMesh;
	public Material beeMaterial;
	public Color[] teamColors;
	public float minBeeSize;
	public float maxBeeSize;
	public float speedStretch;
	public float rotationStiffness;
	[Space(10)]
	[Range(0f,1f)]
	public float aggression;
	public float flightJitter;
	public float teamAttraction;
	public float teamRepulsion;
	[Range(0f,1f)]
	public float damping;
	public float chaseForce;
	public float carryForce;
	public float grabDistance;
	public float attackDistance;
	public float attackForce;
	public float hitDistance;
	public float maxSpawnSpeed;
	[Space(10)]
	public int startBeeCount;

	List<Bee> bees;
	List<Bee>[] teamsOfBees;
	List<Bee> pooledBees;

	int activeBatch = 0;
	List<List<Matrix4x4>> beeMatrices;
	List<List<Vector4>> beeColors;

	static BeeManager instance;

	const int beesPerBatch=1023;
	MaterialPropertyBlock matProps;

	//Used when the position of the bees to spawn aren't known
	public static void SpawnBee(int team) {
		Vector3 pos = Vector3.right * (-Field.size.x * .4f + Field.size.x * .8f * team);
		instance._SpawnBee(pos,team);
	}

	//Used when the position for the bees to spawn is known
	public static void SpawnBee(Vector3 pos,int team) {
		instance._SpawnBee(pos,team);
	}

	//The function that actually spawns the bees
	void _SpawnBee(Vector3 pos, int team) {
		Bee bee;
		if (pooledBees.Count == 0) {
			bee = new Bee();
		} else {
			bee = pooledBees[pooledBees.Count-1];
			pooledBees.RemoveAt(pooledBees.Count - 1);
		}
		bee.Init(pos,team,Random.Range(minBeeSize,maxBeeSize));
		bee.velocity = Random.insideUnitSphere * maxSpawnSpeed;
		bees.Add(bee);
		teamsOfBees[team].Add(bee);
		if (beeMatrices[activeBatch].Count == beesPerBatch) {
			activeBatch++;
			if (beeMatrices.Count==activeBatch) {
				beeMatrices.Add(new List<Matrix4x4>());
				beeColors.Add(new List<Vector4>());
			}
		}
		beeMatrices[activeBatch].Add(Matrix4x4.identity);
		beeColors[activeBatch].Add(teamColors[team]);
	}

	//Deletes the bees from both the game and the team when needed
	void DeleteBee(Bee bee) {
		pooledBees.Add(bee);
		bees.Remove(bee);
		teamsOfBees[bee.team].Remove(bee);
		if (beeMatrices[activeBatch].Count == 0 && activeBatch>0) {
			activeBatch--;
		}
		beeMatrices[activeBatch].RemoveAt(beeMatrices[activeBatch].Count - 1);
		beeColors[activeBatch].RemoveAt(beeColors[activeBatch].Count - 1);
	}

	void Awake() {
		instance = this;
	}
	void Start () {
		bees = new List<Bee>(50000);
		teamsOfBees = new List<Bee>[2];
		pooledBees = new List<Bee>(50000);

		beeMatrices = new List<List<Matrix4x4>>();
		beeMatrices.Add(new List<Matrix4x4>());
		beeColors = new List<List<Vector4>>();
		beeColors.Add(new List<Vector4>());

		matProps = new MaterialPropertyBlock();

		for (int i=0;i<2;i++) {
			teamsOfBees[i] = new List<Bee>(25000);
		}
		//Spawns the initial bees
		for (int i=0;i<startBeeCount;i++) {
			int team = i%2;
			SpawnBee(team);
		}

		matProps = new MaterialPropertyBlock();
		matProps.SetVectorArray("_Color",new Vector4[beesPerBatch]);
	}

	void FixedUpdate() {
		float deltaTime = Time.fixedDeltaTime;

		//loop through every single bee in the game
		for (int i = 0; i < bees.Count; i++) {
			Bee bee = bees[i];
			bee.isAttacking = false;
			bee.isHoldingResource = false;
			//Check if bee is not dead
			if (bee.dead == false) {
				//Set velocity to random direction
				bee.velocity += Random.insideUnitSphere * (flightJitter * deltaTime);
				bee.velocity *= (1f-damping);

				//Find all allied bees
				List<Bee> allies = teamsOfBees[bee.team];
				//Pick random friendly bee
				Bee attractiveFriend = allies[Random.Range(0,allies.Count)];
				//calculate difference in position between the two bees
				Vector3 delta = attractiveFriend.position - bee.position;
				//Calculate distance
				float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
				//Update bee velocity if distance is greater than 0
				if (dist > 0f) {
					bee.velocity += delta * (teamAttraction * deltaTime / dist);
				}

				//Find another random friendly bee (why is this not used)
				//Do same operations as above, think this is coded incorrectly
				Bee repellentFriend = allies[Random.Range(0,allies.Count)];
				delta = repellentFriend.position - bee.position;
				dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
				if (dist > 0f) {
					bee.velocity -= delta * (teamRepulsion * deltaTime / dist);
				}

				//check if bee has not targeted another bee or a resource
				if (bee.enemyTarget == null && bee.resourceTarget == null) {
					//If number is lower than the aggression rate, then we find a target
					if (Random.value < aggression) {
						//find the opposite team
						List<Bee> enemyTeam = teamsOfBees[1 - bee.team];
						if (enemyTeam.Count > 0) {
							bee.enemyTarget = enemyTeam[Random.Range(0,enemyTeam.Count)];
						}
					//If we don't target another bee we try to target a resource instead
					} else {
						bee.resourceTarget = ResourceManager.TryGetRandomResource();
					}
				//Check if we have targeted a bee
				} else if (bee.enemyTarget != null) {
					//If the targeted bee is dead we reset our target
					if (bee.enemyTarget.dead) {
						bee.enemyTarget = null;
					} else {
						//Calculate distance between current bee and the targeted bee
						delta = bee.enemyTarget.position - bee.position;
						float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
						//if the distance is longer than the total attack range the current bee moves closer to the target
						if (sqrDist > attackDistance * attackDistance) {
							bee.velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
						} else {
							//If the distance is shorter than total attack range the bee attacks its target
							bee.isAttacking = true;
							bee.velocity += delta * (attackForce * deltaTime / Mathf.Sqrt(sqrDist));
							//calculate if we hit the target bee
							if (sqrDist < hitDistance * hitDistance) {
								//If the targeted bee is hit is is killed and blood is spawned, the dead bee has its velocity cut in half
								ParticleManager.SpawnParticle(bee.enemyTarget.position,ParticleType.Blood,bee.velocity * .35f,2f,6);
								bee.enemyTarget.dead = true;
								bee.enemyTarget.velocity *= .5f;
								bee.enemyTarget = null;
							}
						}
					}
				//The bee has a resource targeted
				} else if (bee.resourceTarget != null) {
					//get targeted resource
					Resource resource = bee.resourceTarget;
					//Check if the resource currently isn't being held by another bee
					if (resource.holder == null) {
						//If resource is dead we clear the target
						if (resource.dead) {
							bee.resourceTarget = null;
						//If the targeted resource isn't at the top of the stack we clear the target
						} else if (resource.stacked && ResourceManager.IsTopOfStack(resource) == false) {
							bee.resourceTarget = null;
						//If the resource is at the top of the stack the bee moves towards it and grabs the resource when it is close enough
						} else {
							delta = resource.position - bee.position;
							float sqrDist = delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
							if (sqrDist > grabDistance * grabDistance) {
								bee.velocity += delta * (chaseForce * deltaTime / Mathf.Sqrt(sqrDist));
							} else if (resource.stacked) {
								ResourceManager.GrabResource(bee,resource);
							}
						}
					//If the current bee is holding the resource it flies towards its own side and dropsthe resource there
					} else if (resource.holder == bee) {
						Vector3 targetPos = new Vector3(-Field.size.x * .45f + Field.size.x * .9f * bee.team,0f,bee.position.z);
						delta = targetPos - bee.position;
						dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
						bee.velocity += (targetPos - bee.position) * (carryForce * deltaTime / dist);
						if (dist < 1f) {
							resource.holder = null;
							bee.resourceTarget = null;
						} else {
							bee.isHoldingResource = true;
						}
					//If the resource is held by a bee on the other team, that bee is set at a target to attack
					} else if (resource.holder.team != bee.team) {
						bee.enemyTarget = resource.holder;
					//If the resource is held by a bee on own team the target is cleared
					} else if (resource.holder.team == bee.team) {
						bee.resourceTarget = null;
					}
				}
			//If the bee is dead blood will spawn from it until its death-timer has run out, then it gets deleted
			} else {
				if (Random.value<(bee.deathTimer-.5f)*.5f) {
					ParticleManager.SpawnParticle(bee.position,ParticleType.Blood,Vector3.zero);
				}

				bee.velocity.y += Field.gravity * deltaTime;
				bee.deathTimer -= deltaTime / 10f;
				if (bee.deathTimer < 0f) {
					DeleteBee(bee);
				}
			}
			//Update position of the bee in regards to dealtaTime and the velocity it has
			bee.position += deltaTime * bee.velocity;

			//Checks to keep the bees within the arena
			if (System.Math.Abs(bee.position.x) > Field.size.x * .5f) {
				bee.position.x = (Field.size.x * .5f) * Mathf.Sign(bee.position.x);
				bee.velocity.x *= -.5f;
				bee.velocity.y *= .8f;
				bee.velocity.z *= .8f;
			}
			if (System.Math.Abs(bee.position.z) > Field.size.z * .5f) {
				bee.position.z = (Field.size.z * .5f) * Mathf.Sign(bee.position.z);
				bee.velocity.z *= -.5f;
				bee.velocity.x *= .8f;
				bee.velocity.y *= .8f;
			}
			float resourceModifier = 0f;
			if (bee.isHoldingResource) {
				resourceModifier = ResourceManager.instance.resourceSize;
			}
			if (System.Math.Abs(bee.position.y) > Field.size.y * .5f - resourceModifier) {
				bee.position.y = (Field.size.y * .5f - resourceModifier) * Mathf.Sign(bee.position.y);
				bee.velocity.y *= -.5f;
				bee.velocity.z *= .8f;
				bee.velocity.x *= .8f;
			}

			// only used for smooth rotation:
			Vector3 oldSmoothPos = bee.smoothPosition;
			if (bee.isAttacking == false) {
				bee.smoothPosition = Vector3.Lerp(bee.smoothPosition,bee.position,deltaTime * rotationStiffness);
			} else {
				bee.smoothPosition = bee.position;
			}
			bee.smoothDirection = bee.smoothPosition - oldSmoothPos;
		}
	}
	private void Update() {
		//Loops through each bee, changing it's size, rotation and color if it is dead
		for (int i=0;i<bees.Count;i++) {
			float size = bees[i].size;
			Vector3 scale = new Vector3(size,size,size);
			if (bees[i].dead == false) {
				float stretch = Mathf.Max(1f,bees[i].velocity.magnitude * speedStretch);
				scale.z *= stretch;
				scale.x /= (stretch-1f)/5f+1f;
				scale.y /= (stretch-1f)/5f+1f;
			}
			Quaternion rotation = Quaternion.identity;
			if (bees[i].smoothDirection != Vector3.zero) {
				rotation=Quaternion.LookRotation(bees[i].smoothDirection);
			}
			//Fades the color of the bee a bit if it is dead
			Color color= teamColors[bees[i].team];
			if (bees[i].dead) {
				color *= .75f;
				scale *= Mathf.Sqrt(bees[i].deathTimer);
			}
			beeMatrices[i/beesPerBatch][i%beesPerBatch] = Matrix4x4.TRS(bees[i].position,rotation,scale);
			beeColors[i/beesPerBatch][i%beesPerBatch] = color;
		}
		//Draws the mesh of each bee
		for (int i = 0; i <= activeBatch; i++) {
			if (beeMatrices[i].Count > 0) {
				matProps.SetVectorArray("_Color",beeColors[i]);
				Graphics.DrawMeshInstanced(beeMesh,0,beeMaterial,beeMatrices[i],matProps);
			}
		}
	}
}
