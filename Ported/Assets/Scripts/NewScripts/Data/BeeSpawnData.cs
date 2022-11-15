using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public partial struct BeeSpawnData : IComponentData
{
    public int initialSpawnPerTeam;
    public int spawnPerAction;
}
