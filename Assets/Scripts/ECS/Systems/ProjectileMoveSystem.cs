using ECS.AuthoringAndComponenets;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
        var job = new MoveProjectileJob()
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Gravity = 9.81f,
            Ecb = ecb.AsParallelWriter(),
        };

        JobHandle handle = job.ScheduleParallel(state.Dependency);
        handle.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        
        EntityQuery playerQuery = SystemAPI.QueryBuilder().WithAll<Player, LocalTransform>().Build();
        Entity playerEntity = playerQuery.GetSingletonEntity();
        
        EntityCommandBuffer ecb2 = new EntityCommandBuffer(Allocator.TempJob);
        var job2 = new ProjectileHitPlayerJob()
        {
            Ecb = ecb2.AsParallelWriter(),
            PlayerEntity = playerEntity,
            PlayerPos = playerQuery.GetSingleton<LocalTransform>().Position,
        };

        JobHandle handle2 = job2.ScheduleParallel(state.Dependency);
        handle2.Complete();
        ecb2.Playback(state.EntityManager);
        ecb2.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
    
    [BurstCompile][WithNone(typeof(ProjectileDestroy), typeof(ProjectileHitPlayer))]
    public partial struct MoveProjectileJob : IJobEntity
    {
        public float DeltaTime;
        public float Gravity;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private const float HeightStop = 1.0f;
        private const float TimeUntilDespawn = 2.0f;
        
        public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform, ref Projectile projectile, Entity entity)
        {
            if (transform.Position.y < HeightStop)
            {
                Ecb.AddComponent(chunkIndex, entity, new ProjectileDestroy()
                {
                    TimeToDestroy = TimeUntilDespawn
                });
                return;
            }
            projectile.Velocity.y -= Gravity * DeltaTime;
            
            transform.Position += projectile.Velocity * DeltaTime;
        }
    }
    
    [BurstCompile][WithAll(typeof(ProjectileHitPlayer))][WithAbsent(typeof(Parent))]
    public partial struct ProjectileHitPlayerJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity PlayerEntity;
        public float3 PlayerPos;

        public void Execute(ref Projectile projectile, ref LocalTransform transform, in ProjectileSettings projSet,
            [EntityIndexInQuery] int index, Entity entity)
        {
            Ecb.AddComponent(index, entity, new Parent()
            {
                Value = PlayerEntity,
            });
            transform.Position = PlayerPos - transform.Position;
        }
    }
}

