
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
        foreach (var (transform, waypointBuffer, moveComponent) in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>, RefRW<NavAgentMoveComponent>>())
        {
            if (!waypointBuffer.IsEmpty)
            {
                Move(transform, moveComponent, ref state);
            }
        }
    }

    [BurstCompile]
    private void Move(RefRW<LocalTransform> transform, RefRW<NavAgentMoveComponent> moveComponent,
        ref SystemState systemState)
    {
        float3 direction = moveComponent.ValueRO.direction;
        
        if (direction.Equals(float3.zero))
        {
            Debug.Log("Direction 0,0,0 hit");
            //return;
        }
        
        float targetWeight = 1.0f;
        float avoidanceWeight = 1.5f;
        
        float3 avoidanceDirection = moveComponent.ValueRO.avoidanceDirection;
        float3 combinedDirection = direction * targetWeight + avoidanceDirection * avoidanceWeight;
        
        if (combinedDirection.Equals(float3.zero))
        {
            Debug.Log("Combined Direction 0,0,0 hit");
            return;
        }
        float3 combinedDirectionNormalized = math.normalize(combinedDirection);
        
        
        float angle = math.PI * 0.5f - math.atan2(combinedDirectionNormalized.z, combinedDirectionNormalized.x);
        transform.ValueRW.Rotation = math.slerp(
            transform.ValueRW.Rotation,
            quaternion.Euler(new float3(0, angle, 0)),
            SystemAPI.Time.DeltaTime);
        
        
        float3 velocityOld = moveComponent.ValueRO.velocity;
        //math.normalize(direction);
        float3 targetVel = combinedDirection * moveComponent.ValueRO.moveSpeed;
        float3 velocityLerp = math.lerp(velocityOld, targetVel, SystemAPI.Time.DeltaTime);
        
        if (velocityLerp.Equals(float3.zero))
        {
            Debug.Log("Velocity 0,0,0 hit");
            return;
        }
        
        float vL = math.length(velocityLerp);
        float3 vDir = math.normalize(velocityLerp);
        vL = math.clamp(vL, 0, moveComponent.ValueRO.moveSpeed);
        
        velocityLerp = vDir * vL;
        moveComponent.ValueRW.velocity = velocityLerp;
        transform.ValueRW.Position += velocityLerp * SystemAPI.Time.DeltaTime;
        //transform.ValueRW.Position += moveComponent.ValueRO.velocity * SystemAPI.Time.DeltaTime;
        
        //transform.ValueRW.Position += moveComponent.ValueRO.velocity * SystemAPI.Time.DeltaTime;
    }
}
