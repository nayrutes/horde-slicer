using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class EnemyAuthoring: MonoBehaviour
{
    public float MeleeDamage = 1;
    public float MeleeCooldown = 1;
    public float MeleeRadius = 1;
    private class AuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.None);

            AddComponent(authoringEntity,new Enemy()
            {
                MeleeDamage = authoring.MeleeDamage,
                MeleeCooldown = authoring.MeleeCooldown,
                MeleeRadius = authoring.MeleeRadius,
            });
            AddComponent(authoringEntity, new ToDestroy());
            SetComponentEnabled<ToDestroy>(authoringEntity, false);
            AddComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 1, 0, 1) });
        }
    }
}

public struct Enemy : IComponentData
{
    public float MeleeDamage;
    public float MeleeCooldown;
    public float CurrentMeleeCooldown;
    public float MeleeRadius;
    public double MeleeRadiusSquared => MeleeRadius * MeleeRadius;
}

public struct ToDestroy : IComponentData, IEnableableComponent
{
    
}