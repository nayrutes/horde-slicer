﻿
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct DestroySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<ToDestroy>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //QueryEnumerable<Enemy> query = SystemAPI.Query<Enemy>().WithAll<ToDestroy>();
        EntityQuery entityQuery = SystemAPI.QueryBuilder().WithAll<Enemy,ToDestroy>().Build();
        //var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        //var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        //ECB.DestroyEntity(entityQuery,EntityQueryCaptureMode.AtPlayback);
        //ECB.Playback(state.EntityManager);
        
        
        //This only works without linked entity groups?!
        //state.EntityManager.DestroyEntity(entityQuery);

        var entityArray = entityQuery.ToEntityArray(Allocator.Temp);
        state.EntityManager.DestroyEntity(entityArray);

        
    }
}