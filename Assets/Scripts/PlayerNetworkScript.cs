using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerNetworkScript : NetworkBehaviour
{
    [Header("References")] private Rigidbody _rb;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject sun;
    [SerializeField] private GameObject nameDisplayObject;

    [SerializeField] private TextMeshPro playerNameText;

    #region PlayerDisplayName
    // Add SyncVar for player name to ensure it syncs properly across network
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerDisplayName = "";

    [Command]
    private void CmdSetPlayerName(string newName)
    {
        playerDisplayName = newName;
        Debug.Log($"Player name set to: {newName} for player {netId}");
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newName;
        }
    }
    #endregion

    [Header("Movement")] [SerializeField] [Tooltip("Player movement speed")]
    private float speed = 5f;

    [SerializeField] [Tooltip("How quickly the player accelerates")]
    private float acceleration = 10f;

    [SerializeField] [HideInInspector] private Vector2 movement;

    [Header("Camera")] [SerializeField] [Tooltip("Mouse look sensitivity")]
    private float mouseSensitivity = 3f;

    [SerializeField] [Tooltip("Vertical look sensitivity multiplier")]
    private float yMouseOffsetSensitivityFactor = 1f;

    [SerializeField] [Tooltip("Camera position offset from player")]
    private Vector3 offset = new(0, 2, -5);

    [SerializeField] [Tooltip("Camera look target height offset")]
    private float viewOffset = 1f;

    [SerializeField] [Tooltip("x = min, y = max vertical look angle")]
    private Vector2 verticalClamp = new(-30f, 60f);


    [Header("Input")] private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction leaveAction;

    [Client]
    private void OnEnable()
    {
        sun = GameObject.Find("Sun");
        playerCamera = transform.Find("PlayerCamera").gameObject;
        _rb = gameObject.GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Player/Look"];
        moveAction = playerInput.actions["Player/Move"];
        leaveAction = playerInput.actions["Player/Leave"];
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        playerCamera.SetActive(true);
        playerInput.enabled = true;
        moveAction.Enable();
        lookAction.Enable();
        leaveAction.Enable();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // If this is our own player, set the player name on the server
        if (isLocalPlayer)
        {
            string playerName = "DefaultPlayer";
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("PlayerName")))
            {
                playerName = PlayerPrefs.GetString("PlayerName");
            }
            CmdSetPlayerName(playerName);
            
            // Local player requests name creation from server
            if (nameDisplayObject == null)
            {
                CmdRequestCreateNameDisplay();
            }
        }
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        playerCamera.SetActive(false);
        playerInput.enabled = false;
        moveAction.Disable();
        lookAction.Disable();
        leaveAction.Disable();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    #region DisplayNameCreation
    
    // Track if we've already created displays for all players in the scene
    private bool hasCreatedNameDisplays = false;
    
    // Called at the start of the game and when new players join
    [Command]
    private void CmdRequestCreateNameDisplay()
    {
        // Create name display for the requesting client
        if (nameDisplayObject == null)
        {
            CreateNameDisplayLocal();
        }
        
        // Tell all clients to create this player's name display
        RpcCreateNameDisplayForPlayer(netId);
        
        // Tell this client to create name displays for all existing players
        TargetCreateNameDisplaysForAllPlayers(connectionToClient);
    }
    
    // Creates name displays for the player with the given netId on all clients
    [ClientRpc]
    private void RpcCreateNameDisplayForPlayer(uint playerNetId)
    {
        foreach (var player in FindObjectsOfType<PlayerNetworkScript>())
        {
            if (player.netId == playerNetId)
            {
                // Make sure player doesn't already have a name display
                RemoveDuplicateNameDisplays(player);
                
                // Create name display if needed
                if (player.nameDisplayObject == null)
                {
                    player.CreateNameDisplayLocal();
                }
                break;
            }
        }
    }
    
    // Creates name displays for all existing players on the target client
    [TargetRpc]
    private void TargetCreateNameDisplaysForAllPlayers(NetworkConnection target)
    {
        if (hasCreatedNameDisplays) return;
        
        foreach (var player in FindObjectsOfType<PlayerNetworkScript>())
        {
            // Make sure player doesn't already have a name display
            RemoveDuplicateNameDisplays(player);
            
            // Create name display if needed
            if (player.nameDisplayObject == null)
            {
                player.CreateNameDisplayLocal();
            }
        }
        
        hasCreatedNameDisplays = true;
    }
    
    // Removes duplicate name displays from a player if any exist
    private void RemoveDuplicateNameDisplays(PlayerNetworkScript player)
    {
        if (player.nameDisplayObject != null && player.playerNameText != null)
        {
            // Check if the player has multiple name display objects as children
            foreach (Transform child in player.transform)
            {
                if (child.name == "NameDisplay" && child.gameObject != player.nameDisplayObject)
                {
                    Destroy(child.gameObject);
                    Debug.Log($"Removed duplicate name display from player {player.netId}");
                }
            }
        }
    }

    private void CreateNameDisplayLocal()
    {
        // Skip if we already have a name display
        if (nameDisplayObject != null) return;
        
        // Create a new GameObject as a child of the player
        nameDisplayObject = new GameObject("NameDisplay");
        nameDisplayObject.transform.SetParent(transform);
        nameDisplayObject.transform.localPosition = Vector3.up * 1f;
        nameDisplayObject.transform.localRotation = Quaternion.Euler(0, -90, 0);

        // Add TextMeshPro component
        playerNameText = nameDisplayObject.AddComponent<TextMeshPro>();
        playerNameText.alignment = TextAlignmentOptions.Center;
        playerNameText.fontSize = 3;
        playerNameText.color = Color.white;
        // Set name to default if playerDisplayName is null or empty
        if (string.IsNullOrEmpty(playerDisplayName)) playerNameText.text = "DefaultPlayer";
        else playerNameText.text = playerDisplayName;
    }
    #endregion

    private void LateUpdate()
    {
        if (isLocalPlayer)
        {
            ShadowDetection();
            CameraRotation();
        }
        
    }

    [Client]
    private void Update()
    {
        if (!isLocalPlayer) return;
        RotateDisplay();
        PlayerMovement();
        if (leaveAction.triggered) LeaveGame();
    }


    [Client]
    private void RotateDisplay()
    {
        if (isLocalPlayer && playerCamera != null)
        {
            // Find all player name displays in the scene
            foreach (var player in FindObjectsOfType<PlayerNetworkScript>())
            {
                if (player.playerNameText != null && player.playerNameText.gameObject != null)
                {
                    // Make the name display face our camera
                    player.playerNameText.transform.LookAt(playerCamera.transform.position);
                    player.playerNameText.transform.Rotate(0, 180, 0); // Face the camera
                }
            }
        }
    }

    [Client]
    private void CameraRotation()
    {
        var look = lookAction.ReadValue<Vector2>();
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + look.x * 1 / mouseSensitivity, 0);
        offset = new Vector3(offset.x,
            Mathf.Clamp(offset.y + -look.y * (1 / mouseSensitivity / yMouseOffsetSensitivityFactor), verticalClamp.x,
                verticalClamp.y), offset.z);

        var r = new Vector3(offset.x, offset.y, offset.z).magnitude;
        var theta = Mathf.Acos(offset.y / r);
        var phi = -transform.eulerAngles.y * Mathf.Deg2Rad;

        var x = -(r * Mathf.Sin(theta) * Mathf.Cos(phi));
        var y = r * Mathf.Cos(theta);
        var z = -(r * Mathf.Sin(theta) * Mathf.Sin(phi));

        playerCamera.transform.position = transform.position + new Vector3(x, y, z);
        playerCamera.transform.LookAt(transform.position + Vector3.up * viewOffset);
    }

    [Client]
    private void PlayerMovement()
    {
        movement = moveAction.ReadValue<Vector2>();

        var verticalVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        var horizontalVelocity = new Vector3(movement.y, 0, -movement.x) * speed;

        var targetVelocity = verticalVelocity + transform.TransformDirection(horizontalVelocity);
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, acceleration * Time.deltaTime);
    }

    [Client]
    private void ShadowDetection()
    {
        if (sun == null) return;
        var ray = new Ray(transform.position - Vector3.up * transform.localScale.y / 2,
            sun.transform.rotation * Vector3.back * 100f);
        if (Physics.Raycast(ray, out var hit, 100, LayerMask.GetMask("Shadow")))
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.green);
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red);
            
            LevelCheckPoint spawnCheckpoint = null;
            PlayerStats ps = transform.GetComponent<PlayerStats>(); 
            foreach (var cp in FindObjectsByType<LevelCheckPoint>(FindObjectsSortMode.None))
            {
                var idField = typeof(LevelCheckPoint).GetField("checkpointId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int cpId = (int)idField.GetValue(cp);
                if (cpId == ps.level)
                {
                    spawnCheckpoint = cp;
                    break;
                }
            }
            transform.position = spawnCheckpoint.transform.position;
            
        }
            
    }

    private void LeaveGame()
    {
        if (isLocalPlayer)
        {
            Debug.Log("Player leaving game - disconnecting");
            // No need to manually save data here as OnServerDisconnect will handle it
            // All player data is saved and managed in the OnServerDisconnect method of CustomNetworkManager
        }
        
        if (NetworkServer.active && NetworkClient.active) // Host (server + client)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active) // Client only
            NetworkManager.singleton.StopClient();
    }
}
