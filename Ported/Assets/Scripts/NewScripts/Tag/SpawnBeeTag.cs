using Unity.Entities;

[GenerateAuthoringComponent]
public struct SpawnBeeTag : IComponentData
{
    public int team;
}
