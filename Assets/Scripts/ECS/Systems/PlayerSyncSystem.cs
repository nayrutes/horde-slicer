using ECS.AuthoringAndComponenets;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public partial struct PlayerSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        
        public void OnUpdate(ref SystemState state)
        {
            var playerSingleton = PlayerSingleton.Instance;
            var job = new SyncPlayerJob()
            {
                Position = playerSingleton.Position,
                Rotation = playerSingleton.Rotation,
            };

            JobHandle handle = job.ScheduleParallel(state.Dependency);
            handle.Complete();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
        
        [BurstCompile][WithAll(typeof(Player))]
        public partial struct SyncPlayerJob : IJobEntity
        {
            public float3 Position;
            public quaternion Rotation;
        
            public void Execute([ChunkIndexInQuery] int chunkIndex, ref LocalTransform transform, in Player player)
            {
                transform.Position = Position + new float3(0, player.HeightOffset, 0);
                transform.Rotation = Rotation;
            }
        }
    }
}