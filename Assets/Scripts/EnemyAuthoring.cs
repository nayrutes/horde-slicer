using Unity.Entities;
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
        }
    }
}

public struct Enemy : IComponentData
{
    
}

public struct ToDestroy : IComponentData, IEnableableComponent
{
    
}