using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SpawnBeeFromResourceJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity resourcePrefab;
    public ResourceData rd;
    public FieldData fd;
    public Entity bee;
    public float3 position;

    void Execute(Entity resourceEntity, ref Resource resource, in SpawnBeeFromResourceJob srj)
    {
        for (int i = 0; i < rd.beesPerResource; i++)
        {
            var newBee = ecb.Instantiate(bee);

            var newTranslation = new Translation
            {
                Value = resource.position
            };

            ecb.SetComponent(newBee, newTranslation);
            ecb.RemoveComponent(resourceEntity, typeof(SpawnBeeFromResourceJob));

        }
    }
}
