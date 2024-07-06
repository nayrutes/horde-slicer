using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct ProjectileMoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        //transform.ValueRW.Position += moveComponent.ValueRO.velocity * SystemAPI.Time.DeltaTime;
        var job = new MoveProjectileJob()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Gravity = 9.81f,
            Ecb = ecb.AsParallelWriter(),
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
    
    [BurstCompile][WithNone(typeof(ProjectileDestroy))]
    public partial struct MoveProjectileJob : IJobEntity
    {
        public float DeltaTime;
        public float Gravity;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform, ref Projectile projectile, Entity entity)
        {
            if (transform.Position.y < 1)
            {
                Ecb.AddComponent(chunkIndex, entity, new ProjectileDestroy()
                {
                    TimeToDestroy = 2f
                });
                return;
            }
            projectile.Velocity.y -= Gravity * DeltaTime;
            
            transform.Position += projectile.Velocity * DeltaTime;
        }
    }
}

