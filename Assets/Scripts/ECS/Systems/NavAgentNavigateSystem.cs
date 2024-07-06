
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(NavAgentPathSystem))]
public partial struct NavAgentNavigateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (navAgentComponent, transform, waypointBuffer, navigationDirection) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRO<LocalTransform>, DynamicBuffer<WaypointBuffer>, RefRW<NavigationDirection>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Navigate(navAgentComponent, transform, waypointBuffer, navigationDirection, ref state);
            }
        }
    }

    [BurstCompile]
    private void Navigate(RefRW<NavAgentComponent> navAgent, RefRO<LocalTransform> transform,
        DynamicBuffer<WaypointBuffer> waypointBuffer, RefRW<NavigationDirection> navigationDirection, ref SystemState state)
    {
        navigationDirection.ValueRW.HasWayPoint = !waypointBuffer.IsEmpty;
        if (waypointBuffer.IsEmpty)
        {
            //navAgentComponent.ValueRW.NavigationDirection = float3.zero;
            return;
        }

        // if (navAgent.ValueRO.CurrentWaypoint >= waypointBuffer.Length)
        // {
        //     Debug.Log($"Hit waypoint index not in buffer");
        //     navAgentComponent.ValueRW.NavigationDirection = float3.zero;
        //     return;
        // }
        
        if (math.distance(transform.ValueRO.Position, waypointBuffer[navAgent.ValueRO.CurrentWaypoint].WayPoint) < 0.4f)
        {
            if (navAgent.ValueRO.CurrentWaypoint + 1 < waypointBuffer.Length)
            {
                navAgent.ValueRW.CurrentWaypoint += 1;
            }
        }

        float3 direction = waypointBuffer[navAgent.ValueRO.CurrentWaypoint].WayPoint - transform.ValueRO.Position;
        //Debug.Log($"Nav Direction {direction}");
        navigationDirection.ValueRW.Direction = direction;
        
    }
}