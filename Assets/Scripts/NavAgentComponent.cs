using Unity.Entities;
using Unity.Mathematics;

public struct NavAgentComponent : IComponentData
{
    public bool pathCalculated;
    public int currentWaypoint;
    public float moveSpeed;
    public float nextPathCalculateTime;
}

public struct NavAgentTargetComponent : ISharedComponentData
{
    public float3 targetPosition;
}

public struct WaypointBuffer: IBufferElementData{
    public float3 wayPoint;
    
}
