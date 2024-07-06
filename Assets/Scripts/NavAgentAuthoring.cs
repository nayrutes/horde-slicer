using Unity.Entities;
using Unity.Mathematics;
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
            
            AddComponent<NavAgentComponent>(authoringEntity);
            AddComponent(authoringEntity, new NavAgentMoveComponent()
            {
                moveSpeed = authoring.moveSpeed
            });
            //AddSharedComponent(authoringEntity, new NavAgentTargetComponent());
            AddBuffer<WaypointBuffer>(authoringEntity);
        }
    }
}

public struct NavAgentComponent : IComponentData
{
    public bool pathCalculated;
    public int currentWaypoint;
    public float nextPathCalculateTime;
}

public struct WaypointBuffer: IBufferElementData{
    public float3 wayPoint;
    
}

public struct NavAgentMoveComponent : IComponentData, IEnableableComponent
{
    public float moveSpeed;
    public float3 velocity;
    public float3 direction;
    public float3 avoidanceDirection;
}