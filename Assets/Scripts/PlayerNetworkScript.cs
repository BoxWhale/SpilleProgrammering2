using System;
using Mirror;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkScript : NetworkBehaviour
{
    [Header("References")]
    private Rigidbody _rb;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject sun;
    [SerializeField, SyncVar(hook = nameof(ChangeName))] public string playerName;
    [SerializeField] private TextMeshPro playerNameText;
    
    [Header("Movement")]
    [SerializeField] [Tooltip("Player movement speed")] 
    private float speed = 5f;
    [SerializeField] [Tooltip("How quickly the player accelerates")]
    private float acceleration = 10f;
    [SerializeField] [HideInInspector]
    private Vector2 movement;
    
    [Header("Camera")]
    [SerializeField] [Tooltip("Mouse look sensitivity")]
    private float mouseSensitivity = 3f;
    [SerializeField] [Tooltip("Vertical look sensitivity multiplier")]
    private float yMouseOffsetSensitivityFactor = 1f;
    [SerializeField] [Tooltip("Camera position offset from player")]
    private Vector3 offset = new Vector3(0, 2, -5);
    [SerializeField] [Tooltip("Camera look target height offset")]
    private float viewOffset = 1f;
    [SerializeField] [Tooltip("x = min, y = max vertical look angle")]
    private Vector2 verticalClamp = new Vector2(-30f, 60f);
    [SerializeField] [HideInInspector]
    private Vector2 look;
    
    [Header("Input")]
    private PlayerInput playerInput;
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
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            CmdSetPlayerName(PlayerPrefs.GetString("PlayerName"));
        }
        
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        CreateNameDisplay();
        
    }
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        playerCamera.SetActive(false);
        playerInput.enabled = false;
        moveAction.Disable();
        lookAction.Disable();
        leaveAction.Disable();

    }
    
    void ChangeName(string oldName, string newName)
    {
    
        if (playerNameText != null)
        {
            playerNameText.text = newName;
        }
    }
    private void CreateNameDisplay()
    {
        if (playerNameText != null)
        {
            // If the name display already exists, just update the text
            playerNameText.text = playerName;
            return;
        }
        
        // Create a new GameObject as a child of the player
        GameObject nameDisplayObject = new GameObject("NameDisplay");
        nameDisplayObject.transform.SetParent(transform);
        nameDisplayObject.transform.localPosition = Vector3.up * 1f;
        nameDisplayObject.transform.localRotation = Quaternion.Euler(0,-90,0);
        
        // Add TextMeshPro component
        playerNameText = nameDisplayObject.AddComponent<TextMeshPro>();
        playerNameText.alignment = TextAlignmentOptions.Center;
        playerNameText.fontSize = 3;
        playerNameText.color = Color.white;
    
        // Set the current name if available
        if (!string.IsNullOrEmpty(playerName))
        {
            playerNameText.text = playerName;
        }
    }
    
    [Command]
    void CmdSetPlayerName(string name)
    {
        playerName = name;
        RpcPlayerName();
    }
    [ClientRpc]
    private void RpcPlayerName()
    {
        CreateNameDisplay();
    }
    
    void LateUpdate()
    {
        if (isServer) ShadowDetection();
        if (isLocalPlayer) CameraRotation();
    }
    [Client]
    private void Update()
    {
        if(!isLocalPlayer) return;
        PlayerMovement();
        

        if(leaveAction.triggered)
        {
            LeaveGame();
        }
    }

    [Client]
    void CameraRotation()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + look.x*1/mouseSensitivity, 0);
        offset = new(offset.x, Mathf.Clamp(offset.y + -look.y*((1/mouseSensitivity)/yMouseOffsetSensitivityFactor),verticalClamp.x,verticalClamp.y), offset.z);
        
        float r = new Vector3(offset.x, offset.y, offset.z).magnitude;
        float theta = Mathf.Acos(offset.y / r);
        float phi = -transform.eulerAngles.y * Mathf.Deg2Rad;

        float x = -(r * Mathf.Sin(theta) * Mathf.Cos(phi));
        float y = r * Mathf.Cos(theta);
        float z = -(r * Mathf.Sin(theta) * Mathf.Sin(phi));
            
        playerCamera.transform.position = transform.position + new Vector3(x, y, z);
        playerCamera.transform.LookAt(transform.position + Vector3.up * viewOffset);
    }

    [Client]
    void PlayerMovement()
    {
        movement = moveAction.ReadValue<Vector2>();
        
        Vector3 verticalVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        Vector3 horizontalVelocity = new Vector3(movement.y, 0, -movement.x)*speed;
        
        Vector3 targetVelocity = verticalVelocity + transform.TransformDirection(horizontalVelocity);
        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, acceleration*Time.deltaTime);
    }

    [Server]
    void ShadowDetection()
    {
        if (sun == null) return;
        Ray ray = new Ray(transform.position-Vector3.up*transform.localScale.y/2, sun.transform.rotation*Vector3.back*100f);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Shadow")))
        {
            Debug.DrawRay(ray.origin, ray.direction*1000, Color.green);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction*1000, Color.red);
        }
        
    }

    void LeaveGame()
    {
        if (NetworkServer.active && NetworkClient.active) // Host (server + client)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active) // Client only
            NetworkManager.singleton.StopClient();
    }
}
