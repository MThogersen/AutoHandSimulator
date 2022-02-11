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

    private SimulatedPoseDriver left, right, head;
    private Vector2 screenSize;
    public bool isSimulating = false;
    private void Start()
    {
        if (!(bool)XRGeneralSettings.Instance?.Manager?.activeLoader.name.Contains("Mock"))
            return;
        
        isSimulating = true;

        var trackedPoseDrivers = FindObjectsOfType<TrackedPoseDriver>();

        foreach (var driver in trackedPoseDrivers)
        {
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.Center)
            {
                head = new GameObject("HeadDriver").AddComponent<SimulatedPoseDriver>();
                head.transform.position = new Vector3(0,1.5f,0);
                driver.poseProviderComponent = head;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.LeftPose)
            {
                left = new GameObject("LeftDriver").AddComponent<SimulatedPoseDriver>();
                left.transform.position = new Vector3(-.2f, 1f, 0.3f);
                driver.poseProviderComponent = left;
            }
            if (driver.poseSource == TrackedPoseDriver.TrackedPose.RightPose)
            {
                right = new GameObject("RightDriver").AddComponent<SimulatedPoseDriver>();
                right.transform.position = new Vector3(.2f, 1f, 0.3f);
                driver.poseProviderComponent = right;
            }
        }

        player = GetComponent<AutoHandPlayer>();

    }

#if UNITY_EDITOR
    Vector3 mouseStartPos = Vector3.zero;
    public bool setMouseStart = true;
    void Update()
    {

        HandleMovement();

        HandleMouseHeadRotation();
        
    }

    void FixedUpdate()
    {
        player.Move(GetMovementControls());
    }

    void HandleMovement()
    {
        player.Move(GetMovementControls());
        
        player.Turn(GetSnapTurnControls());
    }

    void HandleMouseHeadRotation()
    {
        if (!Input.GetKey(KeyCode.Mouse1))
        {
            setMouseStart = true;
            return;
        }

        if (setMouseStart)
        {
            mouseStartPos = Input.mousePosition;
            setMouseStart = false;
        }

        var deltaRot = GetMouseInput();

        player.AddRotation(Quaternion.Euler(0, deltaRot.x * 3f, 0));
        head.transform.Rotate(-deltaRot.y * 3f, 0, 0, Space.Self);
    }

    private Vector2 GetMouseInput()
    {
        screenSize = new Vector2((float)Screen.currentResolution.height, (float)Screen.currentResolution.width);
        var mouseDiff = (Vector2)(Input.mousePosition - mouseStartPos);
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

    float GetSnapTurnControls()
    {
        var rot = Input.GetKey(KeyCode.Q) ? -1 : 0;
        rot += Input.GetKey(KeyCode.E) ? 1 : 0;
        return rot;
    }
    #endif
}

