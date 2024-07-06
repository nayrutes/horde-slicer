
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
        foreach (var (transform, waypointBuffer, moveComponent, navigationDirection, avoidanceDirection) in 
                 SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>, RefRW<MoveComponent>, RefRO<NavigationDirection>, RefRO<AvoidanceDirection>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Move(transform, navigationDirection, avoidanceDirection, moveComponent, ref state);
            }
        }
    }

    [BurstCompile]
    private void Move(RefRW<LocalTransform> transform, 
        RefRO<NavigationDirection> navigationDirection, 
        RefRO<AvoidanceDirection> avoidanceDirection, 
        RefRW<MoveComponent> moveComponent,
        ref SystemState systemState)
    {
        float3 combinedDirection = float3.zero;
        float targetWeight = 1.0f;
        float avoidanceWeight = 1.5f;

        if (navigationDirection.ValueRO.IsEnabled)
        {
            combinedDirection += targetWeight * navigationDirection.ValueRO.Direction;
        }

        if (avoidanceDirection.ValueRO.IsEnabled)
        {
            combinedDirection += avoidanceWeight * avoidanceDirection.ValueRO.Direction;
        }

        moveComponent.ValueRW.Direction = combinedDirection;
        //float3 navDirection = navigationDirection.ValueRO.Direction;
        
        // if (navDirection.Equals(float3.zero))
        // {
        //     Debug.Log("Direction 0,0,0 hit");
        //     //return;
        // }
        
        
        //float3 avoidanceDirectionV = avoidanceDirection.ValueRO.Direction;
        //combinedDirection = navDirection * targetWeight + avoidanceDirectionV * avoidanceWeight;
        
        if (!combinedDirection.Equals(float3.zero))
        {
            float3 combinedDirectionNormalized = math.normalize(combinedDirection);
        
            float angle = math.PI * 0.5f - math.atan2(combinedDirectionNormalized.z, combinedDirectionNormalized.x);
            transform.ValueRW.Rotation = math.slerp(
                transform.ValueRW.Rotation,
                quaternion.Euler(new float3(0, angle, 0)),
                SystemAPI.Time.DeltaTime);
        }
        
        
        float3 targetVel = combinedDirection * moveComponent.ValueRO.MoveSpeed;
        float3 velocityOld = moveComponent.ValueRO.Velocity;
        float3 velocityLerp = math.lerp(velocityOld, targetVel, SystemAPI.Time.DeltaTime);
        
        if (velocityLerp.Equals(float3.zero))
        {
            Debug.Log("Velocity 0,0,0 hit");
            return;
        }
        
        float vL = math.length(velocityLerp);
        float3 vDir = math.normalize(velocityLerp);
        vL = math.clamp(vL, 0, moveComponent.ValueRO.MoveSpeed);
        
        velocityLerp = vDir * vL;
        moveComponent.ValueRW.Velocity = velocityLerp;
        transform.ValueRW.Position += velocityLerp * SystemAPI.Time.DeltaTime;
        //transform.ValueRW.Position += moveComponent.ValueRO.velocity * SystemAPI.Time.DeltaTime;
        
        //transform.ValueRW.Position += moveComponent.ValueRO.velocity * SystemAPI.Time.DeltaTime;
    }
}
