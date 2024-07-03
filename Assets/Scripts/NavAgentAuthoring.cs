using Unity.Entities;
using UnityEngine;

public class NavAgentAuthoring : MonoBehaviour
{
    //[SerializeField] private Transform targetTransform;
    [SerializeField] private float moveSpeed;

    private class AuthoringBaker : Baker<NavAgentAuthoring>
    {
        public override void Bake(NavAgentAuthoring authoring)
        {
            Entity authoringEntity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(authoringEntity, new NavAgentComponent
            {
                moveSpeed = authoring.moveSpeed
            });
            AddSharedComponent(authoringEntity, new NavAgentTargetComponent());
            AddBuffer<WaypointBuffer>(authoringEntity);
        }
    }
}