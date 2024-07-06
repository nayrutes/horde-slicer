
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateBefore(typeof(DestroySystem))]
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial struct KillZoneSystem : ISystem
{
    
    public void OnUpdate(ref SystemState state)
    {
        //float radius = SystemAPI.GetSingleton<Config>().SafeZoneRadius;
        var playerSingleton = PlayerSingleton.Instance;
        float radius = playerSingleton.KillRadius;
        float3 pos = playerSingleton.Position;

        // Debug rendering (the white circle).
        const float debugRenderStepInDegrees = 20;
        for (float angle = 0; angle < 360; angle += debugRenderStepInDegrees)
        {
            var a = float3.zero;
            var b = float3.zero;
            math.sincos(math.radians(angle), out a.x, out a.z);
            math.sincos(math.radians(angle + debugRenderStepInDegrees), out b.x, out b.z);
            Debug.DrawLine(pos+ (a * radius),pos + (b * radius));
        }

        var safeZoneJob = new KillZoneJob
        {
            SquaredRadius = radius * radius,
            Pos = pos,
        };
        safeZoneJob.ScheduleParallel();
    }
}

[WithAll(typeof(Enemy))]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
[BurstCompile]
public partial struct KillZoneJob : IJobEntity
{
    public float SquaredRadius;
    public float3 Pos;

    // Because we want the global position of a child entity, we read LocalToWorld instead of LocalTransform.
    void Execute(in LocalToWorld transformMatrix, EnabledRefRW<ToDestroy> toDestroyState)
    {
        //toDestroyState.ValueRW = math.lengthsq(transformMatrix.Position) < SquaredRadius;
        toDestroyState.ValueRW = math.distancesq(Pos,transformMatrix.Position) < SquaredRadius;
    }
}
