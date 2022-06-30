using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerControl : NetworkBehaviour
{
    [SerializeField]
    private Image healthBarImage;

    [SerializeField]
    private float walkSpeed = 2f;

    [SerializeField]
    private float runSpeedOffset = 2.0f;

    [SerializeField]
    private Vector2 defaultInitialPositionOnPlane = new Vector2(-4, 4);

    [SerializeField]
    private NetworkVariable<Vector3> networkPositionDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<Vector3> networkRotationDirection = new NetworkVariable<Vector3>();

    [SerializeField]
    private NetworkVariable<PlayerState> networkPlayerState = new NetworkVariable<PlayerState>();


    [SerializeField]
    private NetworkVariable<int> networkPlayerHealth = new NetworkVariable<int>(100);

    [SerializeField]
    private GameObject leftHand;

    [SerializeField]
    private GameObject rightHand;

    [SerializeField]
    private GameObject leftFoot;

    [SerializeField]
    private GameObject rightFoot;

    [SerializeField]
    private float minPunchDistance = 1.0f;

    [SerializeField]
    private float minKickDistance = 1.0f;

    [SerializeField]
    private NetworkVariable<float> networkPlayerPunchBlend = new NetworkVariable<float>();

    [SerializeField]
    private NetworkVariable<float> networkPlayerKickBlend = new NetworkVariable<float>();

    private CharacterController characterController;

    //Trying smt
    private float lookSensitivity = 3f;


    private Vector3 velocity = Vector3.zero;
    private Vector3 rotation = Vector3.zero;
    private float cameraRotationX = 0f;
    //Still trying

    //max Hp
    float maxHp = 100;
    // client caches positions
    private Vector3 oldInputPosition = Vector3.zero;
    private Vector3 oldInputRotation = Vector3.zero;
    private PlayerState oldPlayerState = PlayerState.Idle;

    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        
        if (IsClient && IsOwner)
        {
            transform.position = new Vector3(Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y), 0,
                   Random.Range(defaultInitialPositionOnPlane.x, defaultInitialPositionOnPlane.y));

            PlayerCameraFollow.Instance.FollowPlayer(transform.Find("PlayerCameraRoot"));
        }
    }
    void Update()
    {   

        if (IsClient && IsOwner)
        {
            ClientInput();
            
        }
        ClientVisuals();
        ClientMoveAndRotate();
        if (IsOwner) return;
        {
            healthBarImage.fillAmount = Mathf.Clamp(networkPlayerHealth.Value / maxHp, 0, 1f);
        }

    }
    public void RotateCamera(float _cameraRotationX)
    {
        cameraRotationX = _cameraRotationX;
    }
    public void Rotate(Vector3 _rotation)
    {
        rotation = _rotation;
    }
    public void Move(Vector3 _velocity)
    {
        velocity = _velocity;
    }

    private void FixedUpdate()
    {
        if (IsClient && IsOwner)
        {
            if (networkPlayerState.Value == PlayerState.Punch && ActivePunchActionKey())
            {
                CheckPunch(rightHand.transform, Vector3.up);
                CheckPunch(leftHand.transform, Vector3.down);
            }
            if(networkPlayerState.Value == PlayerState.Kick && ActiveKickActionKey())
            {
                CheckKick(leftFoot.transform, Vector3.down);
                CheckKick(rightFoot.transform, Vector3.up);
            }
        }
    }

    private void CheckPunch(Transform hand, Vector3 aimDirection)
    {
        RaycastHit hit;

        int layerMask = LayerMask.GetMask("Player");

        if (Physics.Raycast(hand.position, hand.transform.TransformDirection(aimDirection), out hit, minPunchDistance, layerMask))
        {
            Debug.DrawRay(hand.position, hand.transform.TransformDirection(aimDirection) * minPunchDistance, Color.yellow);

            var playerHit = hit.transform.GetComponent<NetworkObject>();
            if (playerHit != null)
            { 
                UpdateHealthServerRpc(2, playerHit.OwnerClientId);
            }
        }
        else
        {
            Debug.DrawRay(hand.position, hand.transform.TransformDirection(aimDirection) * minPunchDistance, Color.yellow);
        }
    }
    private void CheckKick(Transform foot, Vector3 aimDirection)
    {
        RaycastHit hit;

        int layerMask = LayerMask.GetMask("Player");

        if (Physics.Raycast(foot.position, foot.transform.TransformDirection(aimDirection), out hit, minKickDistance, layerMask))
        {
            Debug.DrawRay(foot.position, foot.transform.TransformDirection(aimDirection) * minKickDistance, Color.red);

            var playerHit = hit.transform.GetComponent<NetworkObject>();
            if (playerHit != null)
            {
                UpdateHealthServerRpc(2, playerHit.OwnerClientId);
            }
        }
        else
        {
            Debug.DrawRay(foot.position, foot.transform.TransformDirection(aimDirection) * minKickDistance, Color.red);
        }
    }
    private void ClientVisuals()
    {
        if (oldPlayerState != networkPlayerState.Value)
        {
            oldPlayerState = networkPlayerState.Value;
            
        }
        if (networkPlayerState.Value == PlayerState.Punch)
        {
            animator.SetFloat($"{networkPlayerState.Value}Blend", networkPlayerPunchBlend.Value);
        }
        if (networkPlayerState.Value == PlayerState.Kick)
        {
            animator.SetFloat($"{networkPlayerState.Value}Blend", networkPlayerKickBlend.Value);
        }
        animator.SetTrigger($"{networkPlayerState.Value}");
    }
    private void ClientMoveAndRotate()
    {
        if (networkPositionDirection.Value != Vector3.zero)
        {
            characterController.SimpleMove(networkPositionDirection.Value);
        }
        if (networkRotationDirection.Value != Vector3.zero)
        {
            transform.Rotate(networkRotationDirection.Value, Space.World);
        }
    }


    private void ClientInput()
    {
        float _xMov = Input.GetAxis("Horizontal");
        float _zMov = Input.GetAxis("Vertical");

        Vector3 direction = transform.TransformDirection(Vector3.forward);
        Vector3 inputPosition = direction * _zMov;


        Vector3 _movVertical = transform.forward * _zMov;        

        Vector3 _velocity = (_movVertical) * walkSpeed;

        Move(_velocity);

        // change fighting states
        if (ActivePunchActionKey() && _zMov == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Punch);
            return;
        }
        if (ActiveKickActionKey() && _zMov == 0)
        {
            UpdatePlayerStateServerRpc(PlayerState.Kick);
            return;
        }

        // change motion states
        if (_zMov == 0)
            UpdatePlayerStateServerRpc(PlayerState.Idle);
        else if (!ActiveRunningActionKey() && _zMov > 0 && _zMov <= 1)
            UpdatePlayerStateServerRpc(PlayerState.Walk);
        else if (ActiveRunningActionKey() && _zMov > 0 && _zMov <= 1)
        {
            inputPosition = direction * runSpeedOffset;
            UpdatePlayerStateServerRpc(PlayerState.Run);
        }
        else if (!ActiveRunningActionKey() && _zMov < 0 && _zMov >= -1)
            UpdatePlayerStateServerRpc(PlayerState.ReverseWalk);
        else if (ActiveRunningActionKey() && _zMov < 0 && _zMov >= -1)
        {
            inputPosition = -direction * runSpeedOffset;
            UpdatePlayerStateServerRpc(PlayerState.ReverseRun);
        }
        //left-right move
        this.transform.Translate(Input.GetAxis("Horizontal") / 50, 0, 0);


        //Rotation
        float _yRot = Input.GetAxisRaw("Mouse X");
        Vector3 _rotation = new Vector3(0f, _yRot, 0f) * lookSensitivity;

        Rotate(_rotation);

        float _xRot = Input.GetAxisRaw("Mouse Y");

        float _cameraRotationX = _xRot * lookSensitivity;

        RotateCamera(_cameraRotationX);

        // let server know about position and rotation client changes
        if (oldInputPosition != inputPosition ||
            oldInputRotation != _rotation)
        {
            oldInputPosition = inputPosition;
            oldInputRotation = _rotation;
            UpdateClientPositionAndRotationServerRpc(inputPosition * walkSpeed, _rotation);
        }
    }
    
    private static bool ActiveRunningActionKey()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    private static bool ActivePunchActionKey()
    {
        return Input.GetKey(KeyCode.Z);           
    }
    private static bool ActiveKickActionKey()
    {
        return Input.GetKey(KeyCode.X);
    }
    [ServerRpc]
    public void UpdateClientPositionAndRotationServerRpc(Vector3 newPosition, Vector3 newRotation)
    {
        networkPositionDirection.Value = newPosition;
        networkRotationDirection.Value = newRotation;
    }

    [ServerRpc]
    public void UpdateHealthServerRpc(int takeAwayPoint, ulong clientId)
    {
        var clientWithDamaged = NetworkManager.Singleton.ConnectedClients[clientId]
            .PlayerObject.GetComponent<PlayerControl>();

        if (!IsOwner && clientWithDamaged != null && clientWithDamaged.networkPlayerHealth.Value > 0)
        {
            clientWithDamaged.networkPlayerHealth.Value -= takeAwayPoint;
            Debug.Log(networkPlayerHealth.Value);
        }

        // execute method on a client getting punch
        NotifyHealthChangedClientRpc(takeAwayPoint, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    public void NotifyHealthChangedClientRpc(int takeAwayPoint, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner) return;

        Logger.Instance.LogInfo($"Client got punch {takeAwayPoint}");
        Logger.Instance.LogInfo($"Client got kick {takeAwayPoint}");
    }

    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        networkPlayerState.Value = state;
        if (state == PlayerState.Punch)
        {
            networkPlayerPunchBlend.Value = Random.Range(0.0f, 1.0f);
        }
        if (state == PlayerState.Kick)
        {
            networkPlayerKickBlend.Value = Random.Range(0.0f, 1.0f);
        }
    }
}
