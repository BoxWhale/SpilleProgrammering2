using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputActivation : NetworkBehaviour
{
    public PlayerInput playerInput;
    public GameObject camera;
    private void Awake()
    {
        playerInput.enabled = true;
        camera.gameObject.SetActive(true);
    }
}
