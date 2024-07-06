using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class EnemyAuthoring: MonoBehaviour
{
    private class AuthoringBaker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.None);

            AddComponent<Enemy>(authoringEntity);
            AddComponent(authoringEntity, new ToDestroy());
            SetComponentEnabled<ToDestroy>(authoringEntity, false);
            AddComponent(authoringEntity, new URPMaterialPropertyBaseColor() { Value = new float4(0, 1, 0, 1) });
        }
    }
}

public struct Enemy : IComponentData
{
    
}

public struct ToDestroy : IComponentData, IEnableableComponent
{
    
}