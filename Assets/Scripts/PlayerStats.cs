using Mirror;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Tooltip("Visual display of what the initial load data looks like\nData is not saved through this variable")]
    [SyncVar] public int displayData;

    [SyncVar] public int scene;
    [SyncVar] public int level;
}
