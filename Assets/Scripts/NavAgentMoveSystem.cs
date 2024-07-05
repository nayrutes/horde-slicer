
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct NavAgentMoveSystem : ISystem
{
    //[BurstCompile]
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

    //[BurstCompile]
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

        float3 pos = transform.ValueRO.Position;
        int key = SpatialHashing.GetPositionHashKey(pos);
        NativeParallelMultiHashMap<int, CellDataEntry> multiHashMap = SpatialHashing.multiHashMap;
        NativeParallelMultiHashMapIterator<int> nmhKeyIterator;
        CellDataEntry currentLocationToCheck;
        float currentDistance = 1.5f;
        int total = 0;
        float3 avoidanceDirection = float3.zero;
        if (multiHashMap.TryGetFirstValue(key, out currentLocationToCheck, out nmhKeyIterator))
        {
            do
            {
                if (!pos.Equals(currentLocationToCheck.Position))
                {
                    if (currentDistance > math.sqrt(math.lengthsq(pos - currentLocationToCheck.Position)))
                    {
                        currentDistance = math.sqrt(math.lengthsq(pos - currentLocationToCheck.Position));
                        float3 distanceFromTo = pos - currentLocationToCheck.Position;
                        avoidanceDirection = math.normalize(distanceFromTo / currentDistance);
                        total++;
                    }
                }
            } while (multiHashMap.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
        }

        if (!avoidanceDirection.Equals(float3.zero))
        {
            direction += avoidanceDirection;
        }
        
        float angle = math.PI * 0.5f - math.atan2(direction.z, direction.x);

        transform.ValueRW.Rotation = math.slerp(
            transform.ValueRW.Rotation,
            quaternion.Euler(new float3(0, angle, 0)),
            SystemAPI.Time.DeltaTime);

        transform.ValueRW.Position += math.normalize(direction) * SystemAPI.Time.DeltaTime * navAgent.ValueRO.moveSpeed;
    }
}
