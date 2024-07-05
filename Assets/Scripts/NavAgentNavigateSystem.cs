
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
        foreach (var (navAgentComponent, transform, waypointBuffer, moveComponent) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRO<LocalTransform>, DynamicBuffer<WaypointBuffer>, RefRW<NavAgentMoveComponent>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Navigate(navAgentComponent, transform, waypointBuffer, moveComponent, ref state);
            }
        }
    }

    [BurstCompile]
    private void Navigate(RefRW<NavAgentComponent> navAgent, RefRO<LocalTransform> transform,
        DynamicBuffer<WaypointBuffer> waypointBuffer, RefRW<NavAgentMoveComponent> moveComponent, ref SystemState state)
    {
        if (waypointBuffer.IsEmpty)
        {
            moveComponent.ValueRW.direction = float3.zero;
            return;
        }

        if (navAgent.ValueRO.currentWaypoint >= waypointBuffer.Length)
        {
            Debug.Log($"Hit waypoint index not in buffer");
            moveComponent.ValueRW.direction = float3.zero;
            return;
        }
        
        if (math.distance(transform.ValueRO.Position, waypointBuffer[navAgent.ValueRO.currentWaypoint].wayPoint) < 0.4f)
        {
            if (navAgent.ValueRO.currentWaypoint + 1 < waypointBuffer.Length)
            {
                navAgent.ValueRW.currentWaypoint += 1;
            }
        }

        float3 direction = waypointBuffer[navAgent.ValueRO.currentWaypoint].wayPoint - transform.ValueRO.Position;
        //Debug.Log($"Nav Direction {direction}");
        moveComponent.ValueRW.direction = direction;
    }
}