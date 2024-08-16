using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

            //Melee Damage
            EntityQuery enemyDamageQueryMelee = SystemAPI.QueryBuilder().WithAll<Enemy, LocalTransform>().Build();
            NativeArray<float> damagesMelee =
                new NativeArray<float>(enemyDamageQueryMelee.CalculateEntityCount(), Allocator.TempJob);

            var damageJobMelee = new PlayerDamageMeeleJob
            {
                DamageArray = damagesMelee,
                PlayerPos = pos,
                Delta = SystemAPI.Time.DeltaTime,
            };
            JobHandle handleMelee = damageJobMelee.ScheduleParallel(enemyDamageQueryMelee, state.Dependency);

            //Projectile Damage
            EntityQuery enemyDamageQueryProjectile = SystemAPI.QueryBuilder().WithAll<Projectile, LocalTransform, ProjectileSettings>().WithAbsent<ProjectileHitPlayer>().Build();
            NativeArray<float> damagesProjectile =
                new NativeArray<float>(enemyDamageQueryProjectile.CalculateEntityCount(), Allocator.TempJob);
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            var damageJobProjectile = new PlayerDamageProjectileJob()
            {
                DamageArray = damagesProjectile,
                PlayerPos = pos,
                Ecb = ecb.AsParallelWriter(),
            
            };
            JobHandle handleProjectile = damageJobProjectile.ScheduleParallel(enemyDamageQueryProjectile, state.Dependency);
            
            //JobHandle.CompleteAll(ref handleMelee, ref handleProjectile);
            handleMelee.Complete();
            handleProjectile.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            
            float totalDamage = 0f;
            for (int i = 0; i < damagesMelee.Length; i++)
            {
                totalDamage += damagesMelee[i];
            }
            damagesMelee.Dispose();
            for (int i = 0; i < damagesProjectile.Length; i++)
            {
                totalDamage += damagesProjectile[i];
            }
            damagesProjectile.Dispose();
            PlayerSingleton.Instance.ApplyDamage(totalDamage);
        }
    }

    [BurstCompile]
    public partial struct PlayerDamageMeeleJob : IJobEntity
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

    [BurstCompile]
    public partial struct PlayerDamageProjectileJob : IJobEntity
    {
        public float3 PlayerPos;
        public NativeArray<float> DamageArray;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(ref Projectile projectile, in LocalTransform transform, in ProjectileSettings projSet, [EntityIndexInQuery] int index, Entity entity)
        {
            bool isInRange = math.distancesq(PlayerPos, transform.Position) < projSet.PlayerHitRadiusSquared;
            float damage = 0;
            if (isInRange)
            {
                damage = projSet.ProjectileDamage;
                Ecb.AddComponent(index, entity, new ProjectileHitPlayer()
                {
                    //TODO add rel Transform
                });
            }
            DamageArray[index] = damage;
            
        }
    }
}