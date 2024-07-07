
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(NavAgentMoveSystem))]
[UpdateAfter(typeof(SpatialHashingSystem))]
public partial struct EntityAvoidanceSystem : ISystem
{
    private bool debugView;
    public void OnUpdate(ref SystemState state)
    {
        debugView = PlayerSingleton.Instance.AvoidanceDebug;
        foreach (var (transform, avoidanceDirection, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AvoidanceDirection>>().WithEntityAccess())
        {
            Avoid(transform, avoidanceDirection, entity, ref state);
        }
    }
    
    [BurstCompile]
    private void Avoid(RefRO<LocalTransform> transform, RefRW<AvoidanceDirection> avoidanceDirection, Entity entity,
        ref SystemState state)
    {
        float3 pos = transform.ValueRO.Position;
        MultiCellIterator nmhKeyIterator;
        CellDataEntry currentLocationToCheck;
        float currentDistance = 2f;
        float3 avoidanceDirectionV = float3.zero;
        
        if (SpatialHashingSystem.TryGetFirstValue(pos, out currentLocationToCheck, out nmhKeyIterator))
        {
            do
            {
                if (!currentLocationToCheck.Entity.Equals(entity))
                {
                    float3 toOther = pos - currentLocationToCheck.Position;
                    if (toOther.Equals(float3.zero))
                    {
                        Debug.Log($"To other is 0,0,0");
                        toOther = new float3(0.1f, 0, 0);
                    }

                    if (debugView)
                    {
                        Debug.DrawLine(pos,currentLocationToCheck.Position, Color.red);
                    }
                    if (currentDistance > math.sqrt(math.lengthsq(toOther)))
                    {
                        currentDistance = math.sqrt(math.lengthsq(toOther));
                        avoidanceDirectionV = math.normalizesafe(toOther / currentDistance);
                        if (debugView)
                        {
                            Debug.DrawLine(pos,currentLocationToCheck.Position, Color.cyan);
                        }
                    }
                }
            } while (SpatialHashingSystem.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
        }
        avoidanceDirectionV.y = 0;
        if (!avoidanceDirectionV.Equals(float3.zero) && debugView)
        {
            Debug.DrawLine(transform.ValueRO.Position, transform.ValueRO.Position + avoidanceDirectionV);
        }
        avoidanceDirection.ValueRW.Direction = avoidanceDirectionV;
    }
}