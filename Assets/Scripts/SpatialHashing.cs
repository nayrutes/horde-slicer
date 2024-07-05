using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct CellDataEntry
{
    public Entity Entity;
    public float3 Position;
}

public partial struct SpatialHashing : ISystem
{
    private const int zMultiplyer = 1000;
    private const int cellSize = 5;
    public static NativeParallelMultiHashMap<int, CellDataEntry> multiHashMap;
    public static int GetPositionHashKey(float3 pos)
    {
        return (int)(math.floor(pos.x / cellSize) + (zMultiplyer * math.floor(pos.z / cellSize)));
    }

    // public static MultiCellIterator GetSelfAndAdjacent(float3 pos)
    // {
    //     return new MultiCellIterator(multiHashMap, pos, cellSize);
    // }
    public static bool TryGetFirstValue(float3 pos, out CellDataEntry item, out MultiCellIterator it)
    {
        int[] adjacentKeys = new int[9];
        int index = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                float3 adjacentPos = pos + new float3(x * cellSize, y * cellSize, 0);
                adjacentKeys[index++] = GetPositionHashKey(adjacentPos);
            }
        }

        it = new MultiCellIterator(adjacentKeys);
        return TryGetNextValue(out item, ref it);
    }

    public static bool TryGetNextValue(out CellDataEntry item, ref MultiCellIterator it)
    {
        while (it.CurrentKeyIndex < it.Keys.Length)
        {
            if (!it.IsInitialized)
            {
                it.IsInitialized = true;
                if (multiHashMap.TryGetFirstValue(it.Keys[it.CurrentKeyIndex], out item, out it.Iterator))
                {
                    return true;
                }
            }
            else
            {
                if (multiHashMap.TryGetNextValue(out item, ref it.Iterator))
                {
                    return true;
                }
            }

            it.CurrentKeyIndex++;
            it.IsInitialized = false;
        }

        item = default;
        return false;
    }

    private static void DebugDrawCell(float3 pos, float4 color)
    {
        Color c = new Color(color.x, color.y, color.z, color.w);
        const float offset = 0.02f;
        const float shift = 1 - (2 * offset); 
        float3 lowerLeft = new float3(math.floor(pos.x / cellSize) * cellSize + offset, 1f, (math.floor(pos.z / cellSize) * cellSize) + offset);
        Debug.DrawLine(lowerLeft,lowerLeft + new float3(+shift, 0, 0) * cellSize, c);
        Debug.DrawLine(lowerLeft,lowerLeft + new float3(0, 0, +shift) * cellSize, c);
        Debug.DrawLine(lowerLeft + new float3(+shift, 0, 0) * cellSize,lowerLeft + new float3(+shift, 0, +shift) * cellSize, c);
        Debug.DrawLine(lowerLeft + new float3(0, 0, +shift) * cellSize,lowerLeft + new float3(+shift, 0, +shift) * cellSize, c);
    }

    public void OnCreate(ref SystemState state)
    {
        multiHashMap = new NativeParallelMultiHashMap<int, CellDataEntry>(0,Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        multiHashMap.Dispose();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        EntityQuery entityQuery = SystemAPI.QueryBuilder()
            .WithAll<Enemy>()
            .WithAll<LocalToWorld>()
            .Build();

        multiHashMap.Clear();
        int entityCount = entityQuery.CalculateEntityCount();
        if (entityCount > multiHashMap.Capacity)
        {
            multiHashMap.Capacity = entityCount;
        }
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var queryURP = SystemAPI.QueryBuilder().WithAll<URPMaterialPropertyBaseColor>().Build();
        //Debug.Log($"URP hits: {queryURP.CalculateEntityCount()}");
        EntityQueryMask queryMask = queryURP.GetEntityQueryMask();
        
        var random = new Random(123);
        
        // foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<Enemy>().WithEntityAccess())
        // {
        //     int key = GetPositionHashKey(transform.ValueRO.Position);
        //     multiHashMap.Add(key, entity);
        // }

        SetHashMapDataJob setHashMapDataJob = new SetHashMapDataJob()
        {
            multiHashMap = multiHashMap.AsParallelWriter(),
        };
        JobHandle jh = setHashMapDataJob.ScheduleParallel(entityQuery,state.Dependency);
        jh.Complete();
        //setHashMapDataJob.Schedule(entityQuery);
        
        for (int i = -9; i < 9; i++)
        {
            for (int j = -9; j < 9; j++)
            {
                float3 pos = new float3((i * cellSize + cellSize / 2f), 0, (j * cellSize + cellSize / 2f));
                //float4 color = new float4(pos.x / 20 % 1f, pos.y / 20 % 1f, pos.z / 20 % 1f, 1);
                float4 color = RandomColor(ref random);
                DebugDrawCell(pos, color);
                int key = GetPositionHashKey(pos);
                foreach (CellDataEntry cde in multiHashMap.GetValuesForKey(key))
                {
                    ecb.SetComponentForLinkedEntityGroup(cde.Entity, queryMask, new URPMaterialPropertyBaseColor
                    {
                        Value = color
                    });
                }
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    
    static float4 RandomColor(ref Random random)
    {
        // 0.618034005f is inverse of the golden ratio
        var hue = (random.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
    }

    [BurstCompile]
    private partial struct SetHashMapDataJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, CellDataEntry>.ParallelWriter multiHashMap;

        [BurstCompile]
        public void Execute(in LocalToWorld localToWorld, in Entity entity)
        {
            int key = GetPositionHashKey(localToWorld.Position);
            multiHashMap.Add(key, new CellDataEntry()
            {
                Entity = entity,
                Position = localToWorld.Position,
            });
        }
    }
    
}

public struct MultiCellIterator
{
    public NativeParallelMultiHashMapIterator<int> Iterator;
    public int[] Keys;
    public int CurrentKeyIndex;
    public bool IsInitialized;

    public MultiCellIterator(int[] keys)
    {
        Keys = keys;
        CurrentKeyIndex = 0;
        IsInitialized = false;
        Iterator = new NativeParallelMultiHashMapIterator<int>();
    }
}