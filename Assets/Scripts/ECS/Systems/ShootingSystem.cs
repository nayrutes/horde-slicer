
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ShootingSystem: ISystem, ISystemStartStop
{
    BeginSimulationEntityCommandBufferSystem.Singleton bi_ecb;

    public void OnStartRunning(ref SystemState state)
    {
        bi_ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new SpawnJob
        {
            ecb = bi_ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            deltaTime = SystemAPI.Time.DeltaTime,
            gravity = 9.81f,
            targetPos = PlayerSingleton.Instance.Position
        }.ScheduleParallel();
    }
    public void OnStopRunning(ref SystemState state) { }

    [BurstCompile]
    partial struct SpawnJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float deltaTime;
        public float gravity;// = 9.81f;
        public float3 targetPos;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int chunkIndex, RefRW<Shooting> shooter, RefRO<LocalTransform> localTransform)
        {
            if (shooter.ValueRO.CurrentCoolDown <= 0)
            {
                float3 pos = localTransform.ValueRO.Position + shooter.ValueRO.SpawnOffset;
                Entity e = InstantiateEntity(chunkIndex, pos, shooter.ValueRO.PrefabProjectile);
                ecb.AddComponent(chunkIndex, e, new Projectile()
                {
                    Velocity = CalculateInitialVelocity(pos, targetPos),
                    
                });
                
                shooter.ValueRW.CurrentCoolDown = shooter.ValueRO.CoolDownReset;
            }

            shooter.ValueRW.CurrentCoolDown -= deltaTime;
            // if (spawner.ValueRO.NextSpawnTime < elapsedTime)
            // {
            //     int gridWidth = spawner.ValueRO.GridWidth;
            //     int gridDepth = spawner.ValueRO.GridDepth;
            //     float spacing = spawner.ValueRO.Spacing;
            //
            //     float offsetX = (gridWidth - 1) * spacing / 2.0f;
            //     float offsetZ = (gridDepth - 1) * spacing / 2.0f;
            //
            //     for (int i = 0; i < gridWidth; i++)
            //     {
            //         for (int j = 0; j < gridDepth; j++)
            //         {
            //             int d = ((int)elapsedTime + i * 3 + j * 7 % 2);
            //             Entity prefab = d==0 ? spawner.ValueRO.PrefabMelee : spawner.ValueRO.PrefabRanged;
            //             float3 posOffset = new float3(i * spacing - offsetX, 0, j * spacing - offsetZ);
            //             InstantiateEntity(chunkIndex, spawner, prefab, posOffset);
            //         }
            //     }
            //     
            //
            //     spawner.ValueRW.NextSpawnTime = (float)elapsedTime + spawner.ValueRO.SpawnRate;
            // }
        }

        private Entity InstantiateEntity(int chunkIndex,float3 pos, Entity prefab)
        {
            Entity newEntity = ecb.Instantiate(chunkIndex, prefab);
            ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(pos));
            return newEntity;
        }
        
        float3 CalculateInitialVelocity(float3 start, float3 target)
        {
            // Offsetting target to hit above the ground
            target.y += 2.0f;
            
            // Calculate the distance to the target
            float3 direction = target - start;
            float3 directionXZ = new float3(direction.x, 0, direction.z);
        
            // Calculate the horizontal distance
            float distanceXZ = math.length(directionXZ);
        
            // Calculate the vertical distance
            float distanceY = direction.y;

            // Assume a launch angle (theta)
            float launchAngle = 45.0f * math.TORADIANS; // Convert degrees to radians

            // Calculate the initial velocity magnitude
            float v0 = math.sqrt((gravity * distanceXZ * distanceXZ) / (2 * (distanceXZ * math.tan(launchAngle) - distanceY) * math.cos(launchAngle) * math.cos(launchAngle)));

            // Calculate the initial velocity vector
            float3 velocity = math.normalize(directionXZ) * v0 * math.cos(launchAngle);
            velocity.y = v0 * math.sin(launchAngle);

            return velocity;
        }
    }
}