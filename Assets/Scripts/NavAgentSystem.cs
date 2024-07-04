using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

public partial struct NavAgentSystem: ISystem
{
    //public float3 TargetPosSys;
    
    
    public void OnUpdate(ref SystemState state)
    {
        var playerSingleton = PlayerSingleton.Instance;
        //float radius = playerSingleton.KillRadius;
        float3 pos = playerSingleton.Position;
        
        //TODO move condition to another system and add enablecomponent to indicate if recalculation is needed
        foreach (var (navAgentComponent, transform, navAgentTargetComponent, waypointBuffer) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, NavAgentTargetComponent, DynamicBuffer<WaypointBuffer>>())
        {
            if (navAgentComponent.ValueRO.nextPathCalculateTime < SystemAPI.Time.ElapsedTime)
            {
                navAgentComponent.ValueRW.nextPathCalculateTime += 1;
                navAgentComponent.ValueRW.pathCalculated = false;
                CalculatePath(navAgentComponent, transform, navAgentTargetComponent, waypointBuffer, ref state, pos);
            }
        }
    }
    
    [BurstCompile]
    private void CalculatePath(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, NavAgentTargetComponent navAgentTargetComponent, DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state, float3 pos)
    {
        NavMeshQuery query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 1024);

        float3 fromPosition = transform.ValueRO.Position;
        float3 toPosition = pos;
        //float3 toPosition = navAgentTargetComponent.targetPosition;
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