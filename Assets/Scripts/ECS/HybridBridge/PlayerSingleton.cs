
using Unity.Mathematics;
using UnityEngine;

public class PlayerSingleton: MonoBehaviour
{
    public static PlayerSingleton Instance;
    
    [SerializeField] private float killRadius;
    [SerializeField] private bool isAttackActive;
    
    private GameObject playerGo;
    [SerializeField] public bool SpatialHashingDebug;
    public bool AvoidanceDebug;
    public float3 Position => playerGo.transform.position;
    public float KillRadius => killRadius;
    public bool IsAttackActive => isAttackActive;

    private void Awake()
    {
        playerGo = gameObject;
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}