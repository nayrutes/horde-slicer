
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct NavAgentMoveSystem : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (navAgentComponent, transform, waypointBuffer, moveComponent) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>, RefRW<NavAgentMoveComponent>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Move(navAgentComponent, transform, waypointBuffer, moveComponent, ref state);
            }
        }
    }

    [BurstCompile]
    private void Move(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform,
        DynamicBuffer<WaypointBuffer> waypointBuffer, RefRW<NavAgentMoveComponent> moveComponent,
        ref SystemState state)
    {
        // if (waypointBuffer.IsEmpty)
        // {
        //     return;
        // }
        // if (math.distance(transform.ValueRO.Position, waypointBuffer[navAgent.ValueRO.currentWaypoint].wayPoint) < 0.4f)
        // {
        //     if (navAgent.ValueRO.currentWaypoint + 1 < waypointBuffer.Length)
        //     {
        //         navAgent.ValueRW.currentWaypoint += 1;
        //     }
        // }
        //
        // float3 direction = waypointBuffer[navAgent.ValueRO.currentWaypoint].wayPoint - transform.ValueRO.Position;
        float3 direction = moveComponent.ValueRO.direction;
        //direction.y = 0;

        // float3 pos = transform.ValueRO.Position;
        // //int key = SpatialHashing.GetPositionHashKey(pos);
        // //NativeParallelMultiHashMap<int, CellDataEntry> multiHashMap = SpatialHashing.multiHashMap;
        // //NativeParallelMultiHashMapIterator<int> nmhKeyIterator;
        // MultiCellIterator nmhKeyIterator;
        // CellDataEntry currentLocationToCheck;
        // float currentSqDistance = 1.5f;
        // int total = 0;
        // float3 avoidanceDirection = float3.zero;
        //
        // if (SpatialHashing.TryGetFirstValue(pos, out currentLocationToCheck, out nmhKeyIterator))
        // {
        //     do
        //     {
        //         if (!pos.Equals(currentLocationToCheck.Position))
        //         {
        //             float3 vecFromTo = pos - currentLocationToCheck.Position;
        //             float toCheckSqDist = math.lengthsq(vecFromTo);
        //             if (currentSqDistance > toCheckSqDist)
        //             {
        //                 //currentSqDistance = math.sqrt(toCheckSqDist);
        //                 currentSqDistance = toCheckSqDist;
        //                 //float3 distanceFromTo = pos - currentLocationToCheck.Position;
        //                 avoidanceDirection = math.normalize(vecFromTo / math.sqrt(currentSqDistance));
        //                 total++;
        //             }
        //         }
        //     } while (SpatialHashing.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
        // }
        //
        // //if (!avoidanceDirection.Equals(float3.zero))
        // {
        //     avoidanceDirection.y = 0;
        //     avoidanceDirection *= 2;
        //     Debug.DrawLine(transform.ValueRO.Position, transform.ValueRO.Position + avoidanceDirection);
        //     direction += avoidanceDirection;
        // }
        
        
        direction += moveComponent.ValueRO.avoidanceDirection;

        if (direction.Equals(float3.zero))
        {
            Debug.Log("Direction 0,0,0 hit");
            return;
        }
        
        float angle = math.PI * 0.5f - math.atan2(direction.z, direction.x);

        transform.ValueRW.Rotation = math.slerp(
            transform.ValueRW.Rotation,
            quaternion.Euler(new float3(0, angle, 0)),
            SystemAPI.Time.DeltaTime);

        float3 velocity = moveComponent.ValueRO.velocity;

        //math.normalize(direction);
        velocity = math.lerp(velocity, direction, SystemAPI.Time.DeltaTime);
        
        if (velocity.Equals(float3.zero))
        {
            Debug.Log("Velocity 0,0,0 hit");
            return;
        }
        
        float vL = math.length(velocity);
        float3 vDir = math.normalize(velocity);
        vL = math.clamp(vL, 0, moveComponent.ValueRO.moveSpeed);
        
        velocity = vDir * vL;
        moveComponent.ValueRW.velocity = velocity;
        transform.ValueRW.Position += velocity * SystemAPI.Time.DeltaTime;
    }
}
