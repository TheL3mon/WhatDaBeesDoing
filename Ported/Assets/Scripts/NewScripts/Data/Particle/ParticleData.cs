using Unity.Entities;

[GenerateAuthoringComponent]
public struct ParticleData : IComponentData
{
    public float speedStretch;
    public int maxParticleCount; // = 10 * instancesPerBatch;
    public Entity particlePrefab;
}

public enum ParticleType
{
    Blood,
    SpawnFlash
}