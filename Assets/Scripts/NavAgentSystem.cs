using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

public partial struct NavAgentSystem: ISystem
{
    public float3 TargetPosSys;
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (navAgentComponent, transform, navAgentTargetComponent, waypointBuffer) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, NavAgentTargetComponent, DynamicBuffer<WaypointBuffer>>())
        {
            if (navAgentComponent.ValueRO.nextPathCalculateTime < SystemAPI.Time.ElapsedTime)
            {
                navAgentComponent.ValueRW.nextPathCalculateTime += 1;
                navAgentComponent.ValueRW.pathCalculated = false;
                CalculatePath(navAgentComponent, transform, navAgentTargetComponent, waypointBuffer, ref state);
            }
            else
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

    [BurstCompile]
    private void CalculatePath(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, NavAgentTargetComponent navAgentTargetComponent, DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state)
    {
        NavMeshQuery query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 1024);

        float3 fromPosition = transform.ValueRO.Position;
        float3 toPosition = navAgentTargetComponent.targetPosition;
        float3 extents = new float3(2, 2, 2);

        NavMeshLocation fromLocation = query.MapLocation(fromPosition, extents, 0);
        NavMeshLocation toLocation = query.MapLocation(toPosition, extents, 0);

        PathQueryStatus status;
        PathQueryStatus returningStatus;
        int maxPathSize = 200;

        if(query.IsValid(fromLocation) && query.IsValid(toLocation))
        {
            status = query.BeginFindPath(fromLocation, toLocation);
            if(status == PathQueryStatus.InProgress)
            {
                status = query.UpdateFindPath(200, out int iterationsPerformed);
                if (status == PathQueryStatus.Success)
                {
                    status = query.EndFindPath(out int pathSize);

                    NativeArray<NavMeshLocation> result = new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                    NativeArray<StraightPathFlags> straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                    NativeArray<float> vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                    NativeArray<PolygonId> polygonIds = new NativeArray<PolygonId>(pathSize + 1, Allocator.Temp);
                    int straightPathCount = 0;

                    query.GetPathResult(polygonIds);

                    returningStatus = PathUtils.FindStraightPath
                        (
                        query,
                        fromPosition,
                        toPosition,
                        polygonIds,
                        pathSize,
                        ref result,
                        ref straightPathFlag,
                        ref vertexSide,
                        ref straightPathCount,
                        maxPathSize
                        );

                    if(returningStatus == PathQueryStatus.Success)
                    {
                        waypointBuffer.Clear();

                        foreach (NavMeshLocation location in result)
                        {
                            if (location.position != Vector3.zero)
                            {
                                waypointBuffer.Add(new WaypointBuffer { wayPoint = location.position });
                            }
                        }

                        navAgent.ValueRW.currentWaypoint = 0;
                        navAgent.ValueRW.pathCalculated = true;
                    }
                    straightPathFlag.Dispose();
                    polygonIds.Dispose();
                    vertexSide.Dispose();
                }
            }

            //Debug.Log($"path-query status: {(int) status}");
        }
        query.Dispose();
    }
}