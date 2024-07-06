using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class TargetUpdater : MonoBehaviour
{
    public Transform target;
    
    private void Update()
    {
        //EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        //SystemHandle sh = World.DefaultGameObjectInjectionWorld.GetExistingSystem<NavAgentSystem>();
        
        //SystemAPI.SetComponent(sh,new NavAgentSystem());
        //em.GetComponentData<NavAgentSystem>(sh);
        //em.SetComponentData<NavAgentSystem>(sh, new NavAgentSystem());

        
        // EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
        //     .WithAllRW<NavAgentTargetComponent>()
        //     .Build(em);
        //
        // NavAgentTargetComponent natc = new NavAgentTargetComponent()
        // {
        //     targetPosition = target.position,
        // };
        // em.SetSharedComponent(query, natc);
        
        // foreach (Entity entity in query.ToEntityArray(Allocator.Temp))
        // {
        //     NavAgentTargetComponent nac = em.GetComponentData<NavAgentTargetComponent>(entity);
        //     nac.targetPosition = target.position;
        //     em.SetComponentData(entity, nac);
        // }
    }
}