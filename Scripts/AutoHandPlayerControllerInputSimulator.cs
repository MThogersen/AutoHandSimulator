using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;
using UnityEngine.Experimental.XR.Interaction;
using UnityEngine.XR.Management;
using UnityEngine.SpatialTracking;

public class AutoHandPlayerControllerInputSimulator : MonoBehaviour
{
    public AutoHandPlayer player;
    public KeyCode controlLeftHandKey = KeyCode.Q;
    public KeyCode controlRightHandKey = KeyCode.E;
    public bool isSimulating = false;

    private bool simulate = false;
    private SimulatedPoseDriver leftPoser, rightPoser, head;
    private Vector2 screenSize;
    private bool firstFrame = true;

    private void Start()
    {
        Invoke("DelayedStart", 0.1f);
    }

    private void DelayedStart()
    {
        if (!(bool)XRGeneralSettings.Instance?.Manager?.activeLoader.name.Contains("Mock"))
        {
            return;
        }
        else
            simulate = true;

        isSimulating = true;

        var trackedPoseDrivers = FindObjectsOfType<TrackedPoseDriver>();

        foreach (var driver in trackedPoseDrivers)
        {
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.Center)
            {
                head = new GameObject("HeadDriver").AddComponent<SimulatedPoseDriver>();
                head.transform.position = new Vector3(0, 1.8f, 0);
                driver.poseProviderComponent = head;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.LeftPose)
            {
                leftPoser = new GameObject("LeftDriver").AddComponent<SimulatedPoseDriver>();
                leftPoser.transform.position = new Vector3(-.2f, 1f, 0.3f);
                driver.poseProviderComponent = leftPoser;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.RightPose)
            {
                rightPoser = new GameObject("RightDriver").AddComponent<SimulatedPoseDriver>();
                rightPoser.transform.position = new Vector3(.2f, 1f, 0.3f);
                driver.poseProviderComponent = rightPoser;
            }
        }

        player = GetComponent<AutoHandPlayer>();
        Invoke("MoveHeadToStartTracking", 1f);
    }


#if UNITY_EDITOR
    Vector3 previousMousePos = Vector3.zero;
    enum Move
    {
        dontMove,
        bodyAndHead,
        leftHand,
        rightHand,
        bothHands
    }

    Move move = Move.bodyAndHead;

    void Update()
    {
        if (!simulate)
            return;

        move = DetermineWhatToMove();

        switch (move)
        {
            case Move.dontMove:
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

        previousMousePos = Input.mousePosition; // to have the delta mousePos

    }

    void FixedUpdate()
    {
        if (!simulate)
            return;

        if (move == Move.bodyAndHead)
            player.Move(GetMovementControls());
    }

    Move DetermineWhatToMove()
    {
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
    {
        player.Move(GetMovementControls());
    }

    void HandleMouseHeadRotation()
    {
        if (!Input.GetKey(KeyCode.Mouse1))
        {
            firstFrame = true;
            return;
        }

        if (!firstFrame)
        {
            var deltaRot = GetMouseInput(previousMousePos, Input.mousePosition);
            player.AddRotation(Quaternion.Euler(0, deltaRot.x * 500f, 0));
            head.transform.Rotate(-deltaRot.y * 500f, 0, 0, Space.Self);
        }

        firstFrame = false;

    }

    void HandleHandControl(Move move)
    {
        var hand = move == Move.leftHand ? leftPoser : rightPoser;
        var zDelta = Input.mouseScrollDelta.y / 50f;
        var xyDelta = GetMouseInput(previousMousePos, Input.mousePosition)*3f;
        hand.transform.position += new Vector3(xyDelta.x, xyDelta.y, zDelta);

        if (!Input.GetKeyDown(KeyCode.Mouse0))
            return;

        if(move == Move.leftHand)
        {
            if(player.handLeft.GetHeldGrabbable() != null)
            {
                player.handLeft.Release();
            }   
            else
            {
                player.handLeft.Grab();
            }
                
        }
        else if (move == Move.rightHand)
        {
            if (player.handRight.GetHeldGrabbable() != null)
            {
                player.handRight.Release();
            }
            else
            {
                player.handRight.Grab();
            }
                
        }
    }

    private Vector2 GetMouseInput(Vector3 previousMousePos, Vector3 mousePos)
    {
        screenSize = new Vector2((float)Screen.currentResolution.height, (float)Screen.currentResolution.width);
        var mouseDiff = (Vector2)(mousePos - previousMousePos);
        return mouseDiff / screenSize;
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
    }

#endif
}

