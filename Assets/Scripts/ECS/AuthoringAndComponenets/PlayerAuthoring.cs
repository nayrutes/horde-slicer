using Unity.Entities;
using UnityEngine;

namespace ECS.AuthoringAndComponenets
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float heightOffset;
        private class PlayerAuthoringBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity authoringEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(authoringEntity, new Player()
                {
                    HeightOffset = authoring.heightOffset,
                });
            }
        }
    }

    public struct Player : IComponentData
    {
        public float HeightOffset;
    }
}