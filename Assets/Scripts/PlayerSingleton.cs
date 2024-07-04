
using Unity.Mathematics;
using UnityEngine;

public class PlayerSingleton: MonoBehaviour
{
    public static PlayerSingleton Instance;
    
    [SerializeField] private float killRadius;

    private GameObject playerGo;
    public float3 Position => playerGo.transform.position;
    public float KillRadius => killRadius;
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