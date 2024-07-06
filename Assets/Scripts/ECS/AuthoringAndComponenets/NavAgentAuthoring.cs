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
            AddComponent(authoringEntity, new MoveComponent()
            {
                MoveSpeed = authoring.moveSpeed
            });
            AddComponent(authoringEntity, new NavigationDirection().SetEnabled(true));
            AddComponent(authoringEntity, new AvoidanceDirection().SetEnabled(true));
            //AddSharedComponent(authoringEntity, new NavAgentTargetComponent());
            AddBuffer<WaypointBuffer>(authoringEntity);
        }
    }
}

public struct NavAgentComponent : IComponentData
{
    public bool PathCalculated;
    public int CurrentWaypoint;
    public float NextPathCalculateTime;
}

public struct WaypointBuffer: IBufferElementData{
    public float3 WayPoint;
    
}

public struct MoveComponent : IComponentData
{
    public float MoveSpeed;
    public float3 Velocity;
    public float3 Direction;
}

public struct NavigationDirection : IComponentData
{
    public float3 Direction;
    private bool _enabled;
    public bool HasWayPoint;
    public bool IsEnabled => _enabled && HasWayPoint;

    public NavigationDirection SetEnabled(bool b)
    {
        _enabled = b;
        return this;
    }
}

public struct AvoidanceDirection : IComponentData
{
    public float3 Direction;
    private bool _enabled;
    public bool IsEnabled => _enabled;

    public AvoidanceDirection SetEnabled(bool b)
    {
        _enabled = b;
        return this;
    }
}