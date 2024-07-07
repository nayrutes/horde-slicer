using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ProjectileRotationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Initialize any state or dependencies here if needed
    }

    public void OnDestroy(ref SystemState state)
    {
        // Clean up any resources here if needed
    }

    public void OnUpdate(ref SystemState state)
    {
        // Create and schedule the job
        var orientProjectileJob = new OrientProjectileJob();

        // Schedule the job and manage dependencies
        state.Dependency = orientProjectileJob.ScheduleParallel(state.Dependency);
    }
}
    
[BurstCompile][WithNone(typeof(ProjectileDestroy))]
public partial struct OrientProjectileJob : IJobEntity
{
    public void Execute(ref LocalTransform transform, in Projectile projectile)
    {
        if (math.lengthsq(projectile.Velocity) > 0f)
        {
            // Calculate the forward direction from the velocity
            float3 forward = math.normalize(projectile.Velocity);

            // Update the rotation to align with the forward direction
            transform.Rotation = quaternion.LookRotationSafe(forward, math.up());
        }
    }
}