
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(NavAgentMoveSystem))]
[UpdateAfter(typeof(SpatialHashing))]
public partial struct NavAgentEntityAvoidanceSystem : ISystem
{
   
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, moveComponent, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<NavAgentMoveComponent>>().WithEntityAccess())
        {
            Avoid(transform, moveComponent, entity, ref state);
        }
    }
    
    [BurstCompile]
    private void Avoid(RefRO<LocalTransform> transform, RefRW<NavAgentMoveComponent> moveComponent, Entity entity,
        ref SystemState state)
    {
        float3 pos = transform.ValueRO.Position;
        //int key = SpatialHashing.GetPositionHashKey(pos);
        //NativeParallelMultiHashMap<int, CellDataEntry> multiHashMap = SpatialHashing.multiHashMap;
        //NativeParallelMultiHashMapIterator<int> nmhKeyIterator;
        MultiCellIterator nmhKeyIterator;
        CellDataEntry currentLocationToCheck;
        float currentSqDistance = 3f;
        float currentDistance = 2f;
        int total = 0;
        float3 avoidanceDirection = float3.zero;
        
        if (SpatialHashing.TryGetFirstValue(pos, out currentLocationToCheck, out nmhKeyIterator))
        {
            do
            {
                //if (math.lengthsq(pos - currentLocationToCheck.Position) > (0.25f*0.25f))
                if (!currentLocationToCheck.Entity.Equals(entity))
                {
                    // float3 vecFromTo = pos - currentLocationToCheck.Position;
                    // float toCheckSqDist = math.lengthsq(vecFromTo);
                    // if (currentSqDistance > toCheckSqDist)
                    // {
                    //     //currentSqDistance = math.sqrt(toCheckSqDist);
                    //     currentSqDistance = toCheckSqDist;
                    //     //float3 distanceFromTo = pos - currentLocationToCheck.Position;
                    //     avoidanceDirection = math.normalize(vecFromTo / math.sqrt(currentSqDistance));
                    //     total++;
                    // }

                    float3 toOther = pos - currentLocationToCheck.Position;
                    if (toOther.Equals(float3.zero))
                    {
                        Debug.Log($"To other is 0,0,0");
                        toOther = new float3(0.1f, 0, 0);
                    }
                    
                    if (currentDistance > math.sqrt(math.lengthsq(toOther)))
                    {
                        currentDistance = math.sqrt(math.lengthsq(toOther));
                        //float3 distanceFromTo = pos - currentLocationToCheck.Position;
                        avoidanceDirection = math.normalizesafe(toOther / currentDistance);
                        //avoidanceDirection = distanceFromTo;
                        Debug.DrawLine(pos,currentLocationToCheck.Position, Color.cyan);
                    }
                }
            } while (SpatialHashing.TryGetNextValue(out currentLocationToCheck, ref nmhKeyIterator));
        }
        avoidanceDirection.y = 0;
        if (!avoidanceDirection.Equals(float3.zero))
        {
            Debug.DrawLine(transform.ValueRO.Position, transform.ValueRO.Position + avoidanceDirection);
        }
        moveComponent.ValueRW.avoidanceDirection = avoidanceDirection;
    }
}