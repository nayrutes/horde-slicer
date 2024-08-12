using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS.Systems
{
    public partial struct PlayerDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var playerSingleton = PlayerSingleton.Instance;
            float3 pos = playerSingleton.Position;

            var enemyDamageQuery = SystemAPI.QueryBuilder().WithAll<Enemy, LocalTransform>().Build();
            NativeArray<float> damages =
                new NativeArray<float>(enemyDamageQuery.CalculateEntityCount(), Allocator.TempJob);

            var damageJob = new PlayerDamageMeele
            {
                DamageArray = damages,
                PlayerPos = pos,
                Delta = SystemAPI.Time.DeltaTime,
            };
            var handle = damageJob.ScheduleParallel(enemyDamageQuery, state.Dependency);
            handle.Complete();

            float totalDamage = 0f;
            for (int i = 0; i < damages.Length; i++)
            {
                totalDamage += damages[i];
            }

            damages.Dispose();
            PlayerSingleton.Instance.ApplyDamage(totalDamage);
        }
    }

    [BurstCompile]
    public partial struct PlayerDamageMeele : IJobEntity
    {
        public float3 PlayerPos;
        public NativeArray<float> DamageArray;
        public float Delta;

        public void Execute(ref Enemy enemy, in LocalTransform transform, [EntityIndexInQuery] int index)
        {
            enemy.CurrentMeleeCooldown -= Delta;
            bool isReady = enemy.CurrentMeleeCooldown < 0;

            bool isInRange = math.distancesq(PlayerPos, transform.Position) < enemy.MeleeRadiusSquared;

            float damage = 0;
            if (isReady && isInRange)
            {
                damage = enemy.MeleeDamage;
                enemy.CurrentMeleeCooldown = enemy.MeleeCooldown;
            }

            DamageArray[index] = damage;
        }
    }
}