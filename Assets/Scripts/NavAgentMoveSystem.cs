
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct NavAgentMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (navAgentComponent, transform, waypointBuffer) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Move(navAgentComponent, transform, waypointBuffer, ref state);
            }
        }
    }

    [BurstCompile]
    private void Move(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state)
    {
        if (waypointBuffer.IsEmpty)
        {
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
        float angle = math.PI * 0.5f - math.atan2(direction.z, direction.x);

        transform.ValueRW.Rotation = math.slerp(
            transform.ValueRW.Rotation,
            quaternion.Euler(new float3(0, angle, 0)),
            SystemAPI.Time.DeltaTime);

        transform.ValueRW.Position += math.normalize(direction) * SystemAPI.Time.DeltaTime * navAgent.ValueRO.moveSpeed;
    }
}
