using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public partial struct SpawnerSystem : ISystem, ISystemStartStop
{
    BeginSimulationEntityCommandBufferSystem.Singleton bi_ecb;

    public void OnStartRunning(ref SystemState state)
    {
        bi_ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new SpawnJob
        {
            ecb = bi_ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            deltaTime = SystemAPI.Time.DeltaTime,
            elapsedTime = SystemAPI.Time.ElapsedTime,
        }.ScheduleParallel();
    }
    public void OnStopRunning(ref SystemState state) { }

    [BurstCompile]
    partial struct SpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float deltaTime;
        public double elapsedTime;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int chunkIndex, RefRW<Spawner> spawner)
        {
            if (spawner.ValueRO.NextSpawnTime < elapsedTime)
            {
                int gridWidth = spawner.ValueRO.GridWidth;
                int gridDepth = spawner.ValueRO.GridDepth;
                float spacing = spawner.ValueRO.Spacing;

                float offsetX = (gridWidth - 1) * spacing / 2.0f;
                float offsetZ = (gridDepth - 1) * spacing / 2.0f;

                for (int i = 0; i < gridWidth; i++)
                {
                    for (int j = 0; j < gridDepth; j++)
                    {
                        int d = ((int)elapsedTime + i * 3 + j * 7 % 2);
                        Entity prefab = d==0 ? spawner.ValueRO.PrefabMelee : spawner.ValueRO.PrefabRanged;
                        float3 posOffset = new float3(i * spacing - offsetX, 0, j * spacing - offsetZ);
                        InstantiateEntity(chunkIndex, spawner, prefab, posOffset);
                    }
                }
                

                spawner.ValueRW.NextSpawnTime = (float)elapsedTime + spawner.ValueRO.SpawnRate;
            }
        }

        private void InstantiateEntity(int chunkIndex, RefRW<Spawner> spawner, Entity prefab, float3 posOffset)
        {
            Entity newEntity = ecb.Instantiate(chunkIndex, prefab);

            ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(
                spawner.ValueRO.SpawnPosition + posOffset));
        }
    }

    
    // public void OnCreate(ref SystemState state) { }
    //
    // public void OnDestroy(ref SystemState state) { }
    //
    // [BurstCompile]
    // public void OnUpdate(ref SystemState state)
    // {
    //     // Queries for all Spawner components. Uses RefRW because this system wants
    //     // to read from and write to the component. If the system only needed read-only
    //     // access, it would use RefRO instead.
    //     foreach (RefRW<Spawner> spawner in SystemAPI.Query<RefRW<Spawner>>())
    //     {
    //         ProcessSpawner(ref state, spawner);
    //     }
    // }
    //
    // private void ProcessSpawner(ref SystemState state, RefRW<Spawner> spawner)
    // {
    //     // If the next spawn time has passed.
    //     if (spawner.ValueRO.NextSpawnTime < SystemAPI.Time.ElapsedTime)
    //     {
    //         // Spawns a new entity and positions it at the spawner.
    //         Entity newEntity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
    //         // LocalPosition.FromPosition returns a Transform initialized with the given position.
    //         state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(
    //             spawner.ValueRO.SpawnPosition +
    //             new float3(UnityEngine.Random.Range(-5f,5f),0,UnityEngine.Random.Range(-5f,5f))
    //             ));
    //         
    //         // Resets the next spawn time.
    //         spawner.ValueRW.NextSpawnTime = (float)SystemAPI.Time.ElapsedTime + spawner.ValueRO.SpawnRate;
    //     }
    // }
}