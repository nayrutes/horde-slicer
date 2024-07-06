using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;

class SpawnerAuthoring : MonoBehaviour
{
    [FormerlySerializedAs("Prefab")] public GameObject prefabMelee;
    public GameObject prefabRanged;
    public float SpawnRate;
    public int GridWidth = 1;
    public int GridDepth = 1;
    public float Spacing = 1;
    //public Transform target;
}

class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Spawner
        {
            // By default, each authoring GameObject turns into an Entity.
            // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
            PrefabMelee = GetEntity(authoring.prefabMelee, TransformUsageFlags.Dynamic),
            PrefabRanged = GetEntity(authoring.prefabRanged, TransformUsageFlags.Dynamic),
            SpawnPosition = authoring.transform.position,
            NextSpawnTime = 0.0f,
            SpawnRate = authoring.SpawnRate,
            GridWidth = authoring.GridWidth,
            GridDepth = authoring.GridDepth,
            Spacing = authoring.Spacing
        });
    }
}

public struct Spawner : IComponentData
{
    public Entity PrefabMelee;
    public Entity PrefabRanged;
    public float3 SpawnPosition;
    public float NextSpawnTime;
    public float SpawnRate;
    public int GridWidth;
    public int GridDepth;
    public float Spacing;
}