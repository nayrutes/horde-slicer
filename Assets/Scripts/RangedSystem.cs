using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct RangedSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        var job = new RangedJob()
        {
            Ecb = ecb.AsParallelWriter(),
            targetPos = PlayerSingleton.Instance.Position,
        };
        
        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    
    [BurstCompile]
    public partial struct RangedJob : IJobEntity
    {
        public float3 targetPos;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform, ref Ranged ranged, Entity entity)
        {
            float distSq = math.distancesq(targetPos, transform.Position);
            bool aboveMin = distSq > ranged.MinDistance * ranged.MinDistance;
            bool belowMax = distSq < ranged.MaxDistance * ranged.MaxDistance;
            float midDist = (ranged.MinDistance + ranged.MaxDistance)/2.0f;
            bool belowMid = distSq < midDist * midDist;
            
            Ecb.SetComponentEnabled<Shooting>(chunkIndex, entity, aboveMin && belowMax);
            Ecb.SetComponentEnabled<NavAgentMoveComponent>(chunkIndex, entity, !belowMid);

        }
    }
}