using UnityEngine;

public class LevelCheckPoint : MonoBehaviour
{
    [SerializeField] 
    private int checkpointId;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerStats playerStat))
        {
            if (playerStat.level != checkpointId)
            {
                playerStat.level = checkpointId;
            }
            else
            {
                Debug.Log("Player is already registered to this checkpoint: " + checkpointId);
            }
        }
        else
        {
            Debug.Log("Collision with non-player object detected: " + collision.gameObject.name);
        }
    }
}