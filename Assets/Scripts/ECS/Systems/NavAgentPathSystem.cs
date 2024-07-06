using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;
using ISystem = Unity.Entities.ISystem;

public partial struct NavAgentPathSystem: ISystem
{
    //public float3 TargetPosSys;
    
    
    public void OnUpdate(ref SystemState state)
    {
        var playerSingleton = PlayerSingleton.Instance;
        float3 pos = playerSingleton.Position;
        
        //TODO move condition to another system and add enablecomponent to indicate if recalculation is needed
        // foreach (var (navAgentComponent, transform, waypointBuffer) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>, DynamicBuffer<WaypointBuffer>>())
        // {
        //     if (navAgentComponent.ValueRO.nextPathCalculateTime < SystemAPI.Time.ElapsedTime)
        //     {
        //         navAgentComponent.ValueRW.nextPathCalculateTime += 1;
        //         navAgentComponent.ValueRW.pathCalculated = false;
        //         CalculatePath(navAgentComponent, transform, waypointBuffer, ref state, pos);
        //     }
        // }

        // EntityQuery entityQuery = SystemAPI.QueryBuilder()
        //     .WithAll<NavAgentComponent, LocalTransform, WaypointBuffer>()
        //     .Build();
        // int counter = 0;
        // NativeArray<NavMeshQuery> navMeshQueries = new NativeArray<NavMeshQuery>(entityQuery.CalculateEntityCount(),Allocator.TempJob);
        // NativeArray<JobHandle> calcPathJobHandles =
        //     new NativeArray<JobHandle>(entityQuery.CalculateEntityCount(), Allocator.TempJob);
        foreach (var (navAgentComponent, transform, waypointBuffer) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRO<LocalTransform>, DynamicBuffer<WaypointBuffer>>())
        {
            if (navAgentComponent.ValueRO.NextPathCalculateTime >= SystemAPI.Time.ElapsedTime)
            {
                continue;
            }
            navAgentComponent.ValueRW.NextPathCalculateTime += 1;
            navAgentComponent.ValueRW.PathCalculated = false;
            CalculatePath(navAgentComponent, transform, waypointBuffer, ref state, pos);
            
            // NavMeshQuery query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 1024);
            // navMeshQueries[counter] = query;
            // CalcPathJob setHashMapDataJob = new CalcPathJob()
            // {
            //     TargetPos = pos,
            //     query = query,
            //     navAgent = navAgentComponent.ValueRW,
            //     transform = transform.ValueRO,
            //     waypointBuffer = waypointBuffer,
            // };
            // calcPathJobHandles[counter] = setHashMapDataJob.Schedule(state.Dependency);
            // counter++;
        }
        // JobHandle.CompleteAll(calcPathJobHandles);
        // foreach (NavMeshQuery query in navMeshQueries)
        // {
        //     query.Dispose();
        // }
        // navMeshQueries.Dispose();
        // calcPathJobHandles.Dispose();
    }
    
    [BurstCompile]
    private struct CalcPathJob : IJob
    {
        public float3 TargetPos;
        public NavMeshQuery query;
        public NavAgentComponent navAgent;
        public LocalTransform transform;
        [NativeDisableContainerSafetyRestriction]
        public DynamicBuffer<WaypointBuffer> waypointBuffer;
        
        

        [BurstCompile]
        public void Execute()
        {
            //NavMeshQuery query = new NavMeshQuery(NavMeshWorld, Allocator.TempJob, 1024);

            float3 fromPosition = transform.Position;
            //Debug.Log($"From: {fromPosition}");
            float3 toPosition = TargetPos;
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
                                    waypointBuffer.Add(new WaypointBuffer { WayPoint = location.position });
                                }
                            }

                            navAgent.CurrentWaypoint = 0;
                            navAgent.PathCalculated = true;
                        }
                        straightPathFlag.Dispose();
                        polygonIds.Dispose();
                        vertexSide.Dispose();
                    }
                }

                //Debug.Log($"path-query status: {(int) status}");
            }
        }
    }
    
    [BurstCompile]
    private void CalculatePath(RefRW<NavAgentComponent> navAgent, RefRO<LocalTransform> transform, DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state, float3 pos)
    {
        NavMeshQuery query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.TempJob, 1024);

        float3 fromPosition = transform.ValueRO.Position;
        //Debug.Log($"From: {fromPosition}");
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
                                waypointBuffer.Add(new WaypointBuffer { WayPoint = location.position });
                            }
                        }

                        navAgent.ValueRW.CurrentWaypoint = 0;
                        navAgent.ValueRW.PathCalculated = true;
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