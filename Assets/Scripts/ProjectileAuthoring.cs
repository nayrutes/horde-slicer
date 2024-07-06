using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring: MonoBehaviour
{
    private class AuthoringBaker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.None);

            //AddComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
            //SetComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
        }
    }
}

public struct Projectile : IComponentData
{
    public float3 Velocity;
}

public struct ProjectileDestroy : IComponentData
{
    public float TimeToDestroy;
}