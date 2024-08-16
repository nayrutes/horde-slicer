using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ProjectileAuthoring: MonoBehaviour
{
    public double PlayerHitRadius = 2;
    public float ProjectileDamage = 1.3f;

    private class AuthoringBaker : Baker<ProjectileAuthoring>
    {
        
        
        public override void Bake(ProjectileAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(authoringEntity,new ProjectileSettings()
            {
                PlayerHitRadiusSquared = authoring.PlayerHitRadius * authoring.PlayerHitRadius,
                ProjectileDamage = authoring.ProjectileDamage,
            });

            //AddComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
            //SetComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 0, 0.5f, 1) });
        }
    }
}

public struct Projectile : IComponentData
{
    public float3 Velocity;
}

public struct ProjectileSettings : IComponentData
{
    public double PlayerHitRadiusSquared;
    public float ProjectileDamage;
}

public struct ProjectileDestroy : IComponentData
{
    public float TimeToDestroy;
}

public struct ProjectileHitPlayer : IComponentData
{
    
}