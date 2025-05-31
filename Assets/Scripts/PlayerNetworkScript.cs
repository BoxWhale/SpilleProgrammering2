using System;
using Mirror;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkScript : NetworkBehaviour
{
    private Rigidbody _rb;
    public float speed;
    public float acceleration;
    public float mouseSensitivity;
    public float yMouseOffsetSensitivityFactor;
    public bool toggleMouseVisibility;
    public GameObject playerCamera;
    
    private PlayerInput playerInput;
    
    private InputAction moveAction;
    public Vector2 movement;
    
    public InputAction lookAction;
    public Vector2 look;

    private InputAction leaveAction;
    
    public Vector3 offset;
    public float viewOffset;
    [Tooltip("x = min\ny = max")]
    public Vector2 verticalClamp;
    public GameObject sun;
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
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        playerCamera.SetActive(false);
        playerInput.enabled = false;
        moveAction.Disable();
        lookAction.Disable();
        leaveAction.Disable();
        
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
        // Uncomment for debugging movement input
        //Debug.Log(moveAction.ReadValue<Vector2>());
        
        //Return to main menu on Escape key press
        if(leaveAction.triggered) 
        {
            

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
