using System.Collections.Generic;
using UnityEngine;
using Autohand;
using UnityEngine.XR.Management;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using NaughtyAttributes;
using System;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
// ready for new input system implementation 
#endif

[DefaultExecutionOrder(-4)]
public class AutoHandPlayerControllerInputSimulator : MonoBehaviour
{
    enum Move
    {
        noInput,
        body,
        head,
        bodyAndHead,
        leftHand,
        rightHand,
        bothHands
    }

    // Publics / serialized
    [Tooltip("Auto-populates, if not assigned.")]
    public AutoHandPlayer player;
    public float headHeight = 1.8f;

    [Header("Status indicators:")]
    [ReadOnly]
    [Tooltip("This just shows whether the simulator is working. " +
    "It is based on whether the the Mock HMD is running. (only updated on start). " +
    "If this is false, it means that this simulator is not doing anything (literally nothing at all).")]
    [SerializeField]
    private bool isSimulating = false;
    [ReadOnly]
    [SerializeField] private Move currentlyMoving = Move.bodyAndHead;

    [AutoToggleHeader("Advanced Options")]
    public bool ignoreMe = false;
    [Header("Adjustments:")]
    [ShowIf("ignoreMe")]
    [Tooltip("Locks cursor to screen, escape the screen by hitting ESC-key")]
    public bool cursorLock = true;
    [ShowIf("ignoreMe")]
    public Vector3 leftHandStartOffset = new Vector3(-.2f, 1.5f, 0.3f), rightHandStartOffset = new Vector3(.2f, 1.5f, 0.3f);
    [ShowIf("ignoreMe")]
    [Range(100f, 1500f)]
    public float mouseLookSensitivity = 800f;
    [ShowIf("ignoreMe")]
    [Range(0.1f, 2f)]
    public float ScrollHandSpeed = 1f;
    [ShowIf("ignoreMe")]
    [Range(0.5f, 6f)]
    public float handMovementSpeed = 3f;
    [ShowIf("ignoreMe")]
    [Tooltip("This will ensure that the regular XRHandPlayerControllerLink is disabled " +
        "and stays disabled while using this simulator. " +
        "Reasons to use: If you enable/disable the XRHandPlayerControllerLink" +
        "while the game is running, it might overtake control and block the commands from this script.")]
    [SerializeField] bool disableXRControllerLink = false;
    [ShowIf("ignoreMe")]
    [Tooltip("Disables pushing, climbing and platforms options on AutoHandPlayer at startup. These might interfere with the function, though not consistently.")]
    public bool disableInterferringFeatures = false;
    [ShowIf("ignoreMe"), Tooltip("Auto-populates if the reference is on this gameobject. Otherwise set it here.")]
    [SerializeField] private MonoBehaviour xRHandPlayerControllerLink;

    [AutoToggleHeader("Key Setup")]
    [SerializeField] bool ignoreMe2;
    [ShowIf("ignoreMe2")]
    public KeyCode controlLeftHandKeyCode = KeyCode.Q,
        controlRightHandKeyCode = KeyCode.E,
        forwardKeyCode = KeyCode.W,
        leftKeyCode = KeyCode.A,
        backKeyCode = KeyCode.S,
        rightKeyCode = KeyCode.D,
        mouseGrabKeyCode = KeyCode.Mouse0;
    [ShowIf("ignoreMe2")]
    public KeyCode crouchKeyCode = KeyCode.LeftControl;
    [ShowIf("ignoreMe2")]
    public KeyCode resetHandKeyCode = KeyCode.R;
    [ShowIf("ignoreMe2")]
    public KeyCode mouseLookKeyCode = KeyCode.Mouse1;
    [ShowIf("ignoreMe2")]
    [EnableIf("cursorLock")]
    public KeyCode escapeFPSKeyCode = KeyCode.Escape;

    [AutoToggleHeader("Events")]
    [SerializeField] bool ignorMe3;
    [ShowIf("ignorMe3")]
    public UnityHandEvent grabEvent, releaseEvent;

    // Privates
    private SimulatedPoseDriver leftPoser, rightPoser, headPoser;
    private Vector2 screenSize;
    private Vector2 previousMousePos = Vector2.zero;
    private bool firstFrame = true;
    private bool controlLeftHand, controlRightHand, forwardKey, backKey, leftKey, rightKey, mouseGrabKey, mouseLookKey, crouchKey, resetHandKey, mouseLookKeyDown, escapeFPSKeyDown;
    private Vector2 movementInputs = Vector2.zero;
    private Vector2 mouseDeltaPosition = Vector2.zero;
    private Vector2 mouseScrollDelta = Vector2.zero;

    private void Start()
    {
        var activeXRSystemName = XRGeneralSettings.Instance?.Manager?.activeLoader.name;

        // Check for MockHMD OR Open XR, if it is not running, then don't do anything
        if (!activeXRSystemName.Contains("Mock"))
        {
            Debug.Log("From AutohandSim: an active XR system was found, aborting simulation (XR system was: [" + activeXRSystemName + "])");
            isSimulating = false;
            return;
        }

        // Check for active HMD's displaying the game, if none, then continue, otherwise don't do anything
        bool areHMDsConnected = false;
        var activeXRSystem = XRGeneralSettings.Instance?.Manager?.activeLoader.name;
        List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);
        foreach (var subsystem in displaySubsystems)
        {
            if (subsystem.running && !subsystem.subsystemDescriptor.id.Contains("Mock"))
                areHMDsConnected = true;
        }

        if (areHMDsConnected)
            return;
        else
            isSimulating = true;

        // Get tracked pose drivers (these are the ones on Controller right/left and head in the standard Autohand setup)
        var trackedPoseDrivers = FindObjectsOfType<TrackedPoseDriver>();

        // Assign a SimulatedPoseDriver into the Base Pose Provider slot in each of the tracked pose drivers 
        foreach (var driver in trackedPoseDrivers)
        {
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.Center)
            {
                headPoser = new GameObject("HeadDriver").AddComponent<SimulatedPoseDriver>();
                headPoser.transform.position = new Vector3(0, headHeight, 0);
                driver.poseProviderComponent = headPoser;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.LeftPose)
            {
                leftPoser = new GameObject("LeftDriver").AddComponent<SimulatedPoseDriver>();
                leftPoser.transform.position = leftHandStartOffset;
                driver.poseProviderComponent = leftPoser;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.RightPose)
            {
                rightPoser = new GameObject("RightDriver").AddComponent<SimulatedPoseDriver>();
                rightPoser.transform.position = rightHandStartOffset;
                driver.poseProviderComponent = rightPoser;
            }
        }

        player = GetComponent<AutoHandPlayer>();

        if (grabEvent == null)
            grabEvent = new UnityHandEvent();
        if (releaseEvent == null)
            releaseEvent = new UnityHandEvent();

        if (disableXRControllerLink)
            xRHandPlayerControllerLink = (MonoBehaviour)FindObjectOfType<Autohand.Demo.XRHandPlayerControllerLink>();

        // This is to ensure that the XRHandControllerLink script doesn't block the movement input.
        if (disableXRControllerLink && xRHandPlayerControllerLink != null)
            xRHandPlayerControllerLink.enabled = false;

        if (disableInterferringFeatures)
        {
            player.allowBodyPushing = false;
            player.allowClimbing = false;
            player.allowClimbingMovement = false;
            player.allowPlatforms = false;
        }

        // This is to ensure that the movement initializes properly after start
        // (The headFollower of Autohand doesn't start without an initial movement)
        Invoke("MoveHeadToStartTracking", 0.2f);

        // Service message to change Input manager
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogWarning("Thanks for checking out Autohand simulator! Unfortunalely, you appear to have the new input system as the default input system. " +
            "Currently, it is not supported by this simulator, so in order to use it, you'll have to enable the old/legacy input system." +
            "You can do that by going into Edit >> Project Settings >> Player >> and change the [Active Input Handling] to [Legacy] or [Both].");
#endif
    }

#if UNITY_EDITOR
    // Idea is:
    // 1. Get all relevant keyboard/mouse inputs using GetInputs() (values saved in variables)
    // 2. Check the inputs from keyboard/mouse using DetermineWhatToMove() 
    // 3. Depending on the result, handle movement of the different parts (i.e., hands, head and body)
    void Update()
    {
        if (!isSimulating)
            return;

        if (player == null)
            return;

        if (!Application.isFocused)
            return;

        GetInputs();

        HandleFPSMode();

        currentlyMoving = DetermineWhatToMove();

        player.crouching = crouchKey;

        if (resetHandKey)
            ResetHandPositions();

        switch (currentlyMoving)
        {
            case Move.noInput:
                break;
            case Move.body:
                HandleBodyMovement();
                break;
            case Move.head:
                HandleMouseHeadRotation();
                break;
            case Move.bodyAndHead:
                HandleBodyMovement();
                HandleMouseHeadRotation();
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

        // This is a bit of a hack to accommodate a change in the Move() command in Autohand 3.0
        if (!(currentlyMoving == Move.body || currentlyMoving == Move.bodyAndHead))
            player.Move(Vector2.zero); 
    }
    void FixedUpdate()
    {
        if (!isSimulating)
            return;

        if (player == null)
            return;
        
        if (!Application.isFocused)
            return;

        if (currentlyMoving == Move.bodyAndHead || currentlyMoving == Move.body)
        {
            if (disableXRControllerLink && xRHandPlayerControllerLink != null)
                xRHandPlayerControllerLink.enabled = false;
            player.Move(movementInputs);
        }
        if(currentlyMoving == Move.bodyAndHead || currentlyMoving == Move.head)
        {
            HandleMouseHeadRotation();
        }

    }

    private void GetInputs()
    {
        // If using old input system or both:
#if ENABLE_LEGACY_INPUT_MANAGER
        controlLeftHand = Input.GetKey(controlLeftHandKeyCode);
        controlRightHand = Input.GetKey(controlRightHandKeyCode);

        forwardKey = Input.GetKey(forwardKeyCode);
        leftKey = Input.GetKey(leftKeyCode);
        backKey = Input.GetKey(backKeyCode);
        rightKey = Input.GetKey(rightKeyCode);

        crouchKey = Input.GetKey(crouchKeyCode);
        resetHandKey = Input.GetKeyDown(resetHandKeyCode);

        mouseGrabKey = Input.GetKeyDown(mouseGrabKeyCode);
        mouseLookKey = Input.GetKey(mouseLookKeyCode);
        mouseLookKeyDown = Input.GetKeyDown(mouseLookKeyCode);
        mouseScrollDelta = Input.mouseScrollDelta;
        mouseDeltaPosition = GetMouseScreenDeltaPosition();
        movementInputs = GetMovementControls();
        escapeFPSKeyDown = Input.GetKeyDown(escapeFPSKeyCode);
#endif
        // If only the new input system is available:
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // ready for new input system implementation 
#endif
    }

    private void HandleFPSMode()
    {
        if (cursorLock)
        {
            if (mouseGrabKey)
                Cursor.lockState = CursorLockMode.Locked;
            if (escapeFPSKeyDown || mouseLookKey)
                Cursor.lockState = CursorLockMode.None;
        }
        else
            Cursor.lockState = CursorLockMode.None;
    }

    Move DetermineWhatToMove()
    {
        currentlyMoving = Move.noInput;

        if (!(controlLeftHand | controlRightHand))
        {
            if (mouseLookKey == true || (cursorLock && Cursor.lockState == CursorLockMode.Locked))
            {
                if (movementInputs != Vector2.zero)
                    currentlyMoving = Move.bodyAndHead;
                else
                    currentlyMoving = Move.head;
            }
            else if (movementInputs != Vector2.zero)
            {
                currentlyMoving = Move.body;
            }
        }
        else if (controlLeftHand && controlRightHand)
            currentlyMoving = Move.bothHands;
        else if (controlLeftHand)
            currentlyMoving = Move.leftHand;
        else if (controlRightHand)
            currentlyMoving = Move.rightHand;

        return currentlyMoving;
    }

    void HandleBodyMovement()
    {
        if (player == null)
            return;

        if (disableXRControllerLink && xRHandPlayerControllerLink != null)
            xRHandPlayerControllerLink.enabled = false;
        player.Move(movementInputs);
    }

    void HandleMouseHeadRotation()
    {
        if (headPoser == null)
            return;

        if (mouseLookKeyDown)
        {
            firstFrame = true;
            return;
        }

        if (!firstFrame)
        {
            headPoser.transform.Rotate(0, mouseDeltaPosition.x * mouseLookSensitivity, 0, Space.World);

            foreach (var hand in new List<SimulatedPoseDriver> { leftPoser, rightPoser })
            {
                hand.transform.RotateAround(headPoser.transform.position, Vector3.up, mouseDeltaPosition.x * mouseLookSensitivity);
            }

            headPoser.transform.Rotate(-mouseDeltaPosition.y * mouseLookSensitivity, 0, 0, Space.Self);
        }

        firstFrame = false;
        
    }

    void HandleHandControl(Move move)
    {
        var handPoser = move == Move.leftHand ? leftPoser : rightPoser;

        var zDelta = (mouseScrollDelta / 50f).y * ScrollHandSpeed;
        var xyDelta = mouseDeltaPosition * 3f;
        
        var toMove = new Vector3(xyDelta.x, xyDelta.y, zDelta) * handMovementSpeed;

        handPoser.transform.position += handPoser.transform.TransformDirection(toMove);

        if (!mouseGrabKey)
            return;

        if (move == Move.leftHand)
        {
            if (player.handLeft.GetHeldGrabbable() != null)
            {
                player.handLeft.Release();
                releaseEvent.Invoke(player.handLeft);
            }
            else
            {
                player.handLeft.Grab();
                grabEvent.Invoke(player.handLeft);
            }

        }
        else if (move == Move.rightHand)
        {
            if (player.handRight.GetHeldGrabbable() != null)
            {
                player.handRight.Release();
                releaseEvent.Invoke(player.handRight);
            }
            else
            {
                player.handRight.Grab();
                grabEvent.Invoke(player.handRight);
            }

        }
    }

    private Vector2 GetMouseScreenDeltaPosition()
    {
        screenSize = new Vector2((float)Screen.currentResolution.height, (float)Screen.currentResolution.width);

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        var rotY = mouseX * mouseLookSensitivity * Time.deltaTime;
        var rotX = mouseY * mouseLookSensitivity * Time.deltaTime;

        return new Vector2(rotY, -rotX) / screenSize;
    }

    Vector2 GetMovementControls()
    {
        var upDown = forwardKey ? 1 : 0;
        upDown += backKey ? -1 : 0;
        var leftRight = leftKey ? -1 : 0;
        leftRight += rightKey ? 1 : 0;
        return new Vector2(leftRight, upDown);
    }

    void ResetHandPositions()
    {
        var currentForwardRotation = Quaternion.Euler(0, headPoser.transform.rotation.eulerAngles.y, 0);

        leftPoser.transform.position = currentForwardRotation * leftHandStartOffset;
        rightPoser.transform.position = currentForwardRotation * rightHandStartOffset;
    }

    public void MoveHeadToStartTracking()
    {
        headPoser.transform.position += new Vector3(0, 0.01f, 0);
    }

#endif
}

