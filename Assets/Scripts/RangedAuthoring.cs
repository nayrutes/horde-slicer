using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RangedAuthoring: MonoBehaviour
{
    public float MinDistance = 5;
    public float MaxDistance = 8;
    public float CoolDownReset = 1;
    public GameObject prefabProjectile;
    public Transform spawnPosition;
    private class AuthoringBaker : Baker<RangedAuthoring>
    {
        public override void Bake(RangedAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(authoringEntity,new Ranged()
            {
                MinDistance = authoring.MinDistance,
                MaxDistance = authoring.MaxDistance,
            });
            AddComponent(authoringEntity,new Shooting()
            {
                PrefabProjectile = GetEntity(authoring.prefabProjectile, TransformUsageFlags.Dynamic),
                CoolDownReset = authoring.CoolDownReset,
                SpawnOffset = authoring.spawnPosition.position - authoring.transform.position,
            });
            //AddComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
            //SetComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
        }
    }
}

public struct Ranged : IComponentData
{
    public float MinDistance;
    public float MaxDistance;
}


public struct Shooting : IComponentData, IEnableableComponent
{
    public Entity PrefabProjectile;
    public float CoolDownReset;
    public float CurrentCoolDown;
    public float3 SpawnOffset;
}