using UnityEngine;
using Autohand;
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;

public class AutoHandPlayerControllerInputSimulator : MonoBehaviour
{

    [Header("Requirements")]
    public AutoHandPlayer player;  // Autohand class available to the AutoHandPlayer prefab
    public Hand devHandLeft; // The left hand of the debug tool
    public Transform leftDevHandOffset; // The offset of the left dev hand
    public Hand devHandRight; // The right hand of the debug tool
    public Transform rightDevHandOffset; // The offset of the right dev hand

    [Header("Key-Bindings")]
    public float mouseSensitivity = 100.0f; // Mouse sensitivity 
    public KeyCode controlLeftHandKey = KeyCode.Q;  // Key assignment for left hand
    public KeyCode controlRightHandKey = KeyCode.E; // Key assignment for right hand
    public KeyCode resetHandKey = KeyCode.R; // Key assignment for resetting hand pos
    public KeyCode crouchKey = KeyCode.LeftControl; // Key assignment for crouching
    public KeyCode primaryButtonKey = KeyCode.Mouse0; // Primary trigger button

    [Header("Camera")]
    public float fov = 80.0f;  // Field of View used in first person camera

    [Header("Misc")]
    public float headHeight = 1.8f;  // The simulated height of the Mock HMD

    // -- Control variables
    private bool simulate = false;  // True when script is allowed to run
    private SimulatedPoseDriver leftPoser, rightPoser, head;  // placeholder (hands, head)

    // -- First person view
    private float rotY = 0.0f; // rotation around the up/y axis, used in first person camera
    private float rotX = 0.0f; // rotation around the right/x axis, used in first person camera

    private void Start()
    {  // Wait a fraction of a second to make sure the headset hardware is loaded in properly
        Invoke("DelayedStart", 0.1f);
    }

    private void DelayedStart()
    {
        if (!(bool)XRGeneralSettings.Instance?.Manager?.activeLoader.name.Contains("Mock"))
        { // Makes sure all the requirements are met before starting this script
            return;
        }
        else
        { // Set the script indicator on True
            simulate = true;
        }

        if (simulate)
        {  // Turn off the offset of the hands since this gives issues with raycasting
            leftDevHandOffset.position = new Vector3(0, 0, 0);
            rightDevHandOffset.position = new Vector3(0, 0, 0);
            devHandLeft.transform.position = new Vector3(0, 0, 0);
            devHandRight.transform.position = new Vector3(0, 0, 0);

            Cursor.lockState = CursorLockMode.Confined;   // keep confined to center of screen
        }


        var trackedPoseDrivers = FindObjectsOfType<TrackedPoseDriver>();

        foreach (var driver in trackedPoseDrivers)
        {
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.Center)
            { // Create a head
                head = new GameObject("HeadDriver").AddComponent<SimulatedPoseDriver>();
                head.transform.position = new Vector3(0, headHeight, 0);
                driver.poseProviderComponent = head;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.LeftPose)
            { // Create a left hand pose
                leftPoser = new GameObject("LeftDriver").AddComponent<SimulatedPoseDriver>();
                leftPoser.transform.position = new Vector3(-.2f, 1f, 0.3f);
                driver.poseProviderComponent = leftPoser;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.RightPose)
            { // Create a right hand pose
                rightPoser = new GameObject("RightDriver").AddComponent<SimulatedPoseDriver>();
                rightPoser.transform.position = new Vector3(.2f, 1f, 0.3f);
                driver.poseProviderComponent = rightPoser;
            }
        }
        // Obtain player
        player = GetComponent<AutoHandPlayer>();
        // Activate head tracking
        Invoke("MoveHeadToStartTracking", 1f);
    }


#if UNITY_EDITOR
    enum Move
    { // Contains possible outcomes of moving elements
        dontMove,
        bodyAndHead,
        leftHand,
        rightHand,
        bothHands
    }

    Move move = Move.bodyAndHead;

    void Update()
    {
        // If script is not allowed to run, deny service
        if (!simulate)
            return;

        // Check what we are moving
        move = DetermineWhatToMove();

        switch (move)
        {
            case Move.dontMove:
                break;
            case Move.bodyAndHead:
                player.allowBodyPushing = false;
                player.allowClimbing = false;
                player.allowClimbingMovement = false;
                player.allowPlatforms = false;
                HandleBodyMovement();
                break;
            case Move.leftHand:
                HandleHandControl(Move.leftHand);
                break;
            case Move.rightHand:
                HandleHandControl(Move.rightHand);
                break;
            case Move.bothHands:
                HandleHandControl(Move.rightHand);
                HandleHandControl(Move.leftHand);
                break;
            default:
                break;
        }
    }

    private void LateUpdate()
    {
        if (!simulate)
        {
            return;
        }

        // If we do not move the hands we would like to freeze the camera so it (and the object we try to interact with) stays into frame
        if (!(Input.GetKey(controlLeftHandKey) | Input.GetKey(controlRightHandKey)))
        { HandleMouseHeadRotation(); }
        UserInput();
    }

    Quaternion GetRotationTargetBasedOnMouse() 
    { // Set target rotation to mouse pointer in 3D space
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        rotY += mouseX * mouseSensitivity * Time.deltaTime;
        rotX += mouseY * mouseSensitivity * Time.deltaTime;

        rotX = Mathf.Clamp(rotX, -fov, fov);

        Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
        return localRotation;
    }

    private Vector3 GetPostionTargetBasedOnMouse() 
    {  // Converts mouse position to 3D position
        var hand = move == Move.leftHand ? leftPoser : rightPoser;
        var target_hand = devHandLeft;
        if (hand.transform.name == "RightDriver")
        {
            target_hand = devHandRight;
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100))
        {
            return new Vector3(hit.point.x, hit.point.y, hit.point.z);
        }
        return target_hand.transform.position;
    }

    Move DetermineWhatToMove()
    { // Check what inputs are provided to determine what we are moving
        var controlLeftHand = Input.GetKey(controlLeftHandKey);
        var controlRightHand = Input.GetKey(controlRightHandKey);

        if (!(controlLeftHand | controlRightHand))
            move = Move.bodyAndHead;
        else if (controlLeftHand && controlRightHand)
            move = Move.bothHands;
        else if (controlLeftHand)
            move = Move.leftHand;
        else if (controlRightHand)
            move = Move.rightHand;

        return move;
    }

    void HandleBodyMovement()
    {  // Handels player movement W, A, S, D
        player.Move(GetMovementControls());
    }

    void HandleMouseHeadRotation()
    {  // Handels where the user is looking at in first person
        if (head) { 
            head.transform.rotation = GetRotationTargetBasedOnMouse();
        }
    }

    void HandleHandControl(Move move)
    { // Moves hands to correct position
        var hand = move == Move.leftHand ? leftPoser : rightPoser;  // Check which hand we currently using
        Vector3 target = GetPostionTargetBasedOnMouse(); // Fetch target position
        if (hand.transform.name == "LeftDriver") 
        {
            devHandLeft.SetHandLocation(target, GetRotationTargetBasedOnMouse());
        }
        else if (hand.transform.name == "RightDriver")
        {
            devHandRight.SetHandLocation(target, GetRotationTargetBasedOnMouse());
        }        
    }

    Vector2 GetMovementControls()
    {
        var ud = Input.GetKey(KeyCode.W) ? 1 : 0;
        ud += Input.GetKey(KeyCode.S) ? -1 : 0;
        var lr = Input.GetKey(KeyCode.A) ? -1 : 0;
        lr += Input.GetKey(KeyCode.D) ? 1 : 0;
        return new Vector2(lr, ud);
    }

    public void MoveHeadToStartTracking()
    {
        head.transform.position += new Vector3(0, 0.01f, 0);
        // Set firstperson view rotation
        Vector3 rot = head.transform.localRotation.eulerAngles;
        rotY = rot.y;
        rotX = rot.x;
    }

    private void UserInput() 
    { // handles user input
        if (move == Move.bodyAndHead)
        {
            player.Move(GetMovementControls());
        }

        if (Input.GetKeyDown(crouchKey)) 
        { // Allows the user to crouch
            player.crouching = true;
        }
        else if (Input.GetKeyUp(crouchKey)) 
        { // Stand up
            player.crouching = false;
        }

        if (Input.GetKey(resetHandKey))
        { // Reset hands in viewport
            devHandLeft.ResetHandLocation();
            devHandRight.ResetHandLocation();
        }

        if (move == Move.leftHand)
        {
            if (Input.GetKeyDown(primaryButtonKey))
            {
                if (devHandLeft.GetHeldGrabbable() != null)
                {
                    Debug.Log("Releasing");
                    devHandLeft.Release();
                }
                else
                {
                    Debug.Log("Grabbing");
                    devHandLeft.Grab();
                }
            }
        
        }
        else if (move == Move.rightHand)
        {
            if (Input.GetKeyDown(primaryButtonKey))
            { 
                if (devHandRight.GetHeldGrabbable() != null)
                {
                    devHandRight.Release();
                }
                else
                {
                    devHandRight.Grab();
                }
            }
        }
        else if (move == Move.bothHands)
        {
            if (Input.GetKeyDown(primaryButtonKey))
            {
                if (devHandRight.GetHeldGrabbable() != null && devHandLeft.GetHeldGrabbable() != null)
                {
                    devHandRight.Release();
                    devHandLeft.Release();
                }
                else
                {
                    devHandRight.Grab();
                    devHandLeft.Grab();
                }
            }
        }
    }

#endif
}
