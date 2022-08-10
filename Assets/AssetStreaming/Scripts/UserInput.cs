// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

// Switches locomotion type and handles inputs received from the debug menu.

public class UserInput : MonoBehaviour
{
    public enum LocomotionType
    {
        Teleport,
        Walk,
        Free
    }

    private LocomotionType locomotionType;
    private LocomotionController lc;
    private LocomotionTeleport TeleportController
    {
        get
        {
            return lc.GetComponent<LocomotionTeleport>();
        }
    }

    private Text uiLocomotionType;
    private Text uiForceLODText;

    private bool UsingTeleportLocomotion
    {
        get
        {
            return TeleportController.enabled;
        }
    }

    public void Start()
    {
        lc = FindObjectOfType<LocomotionController>();
        SetupNodeTeleport();
        
        uiLocomotionType = DebugUIBuilder.instance.AddButton("Switch Locomotion Type (Teleport)", SwitchLocomotionType).GetComponentInChildren<UnityEngine.UI.Text>();
        DebugUIBuilder.instance.AddToggle("Toggle LOD Debug View", ToggleDebugView, false);
        DebugUIBuilder.instance.AddToggle("Toggle Benchmark", ToggleBenchmark, false);
        var sliderRect = DebugUIBuilder.instance.AddSlider("Force LOD", -1.0f, 2.0f, ForceLOD, true);
        sliderRect.GetComponentInChildren<Slider>().SetValueWithoutNotify(-1);
        var textElementsInSlider = sliderRect.GetComponentsInChildren<Text>();
        textElementsInSlider[0].text = "Force LOD";
        uiForceLODText = textElementsInSlider[1];
        uiForceLODText.text = "Off";
        DebugUIBuilder.instance.AddToggle("Freeze LOD Levels", FreezeLODLevels, false);
    }

    public void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Start))
        {
            SwitchLocomotionType();
        }

        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            ToggleMenu();
        }
    }

    public void SwitchLocomotionType()
    {
        switch (locomotionType)
        {
            case LocomotionType.Teleport:
                locomotionType = LocomotionType.Walk;
                uiLocomotionType.text = "Switch Locomotion Type (Walk)";
                SetupWalkOnly();
                break;
            case LocomotionType.Walk:
                locomotionType = LocomotionType.Free;
                uiLocomotionType.text = "Switch Locomotion Type (Free)";
                SetupFreeFlying();
                break;
            case LocomotionType.Free:
                locomotionType = LocomotionType.Teleport;
                uiLocomotionType.text = "Switch Locomotion Type (Teleport)";
                SetupNodeTeleport();
                break;
        }
    }

    public void ToggleMenu()
    {
        if (DebugUIBuilder.instance.gameObject.activeSelf)
            DebugUIBuilder.instance.Hide();
        else
            DebugUIBuilder.instance.Show();
    }

    public void ToggleDebugView(Toggle t = null)
    {
        LODManager[] managers = FindObjectsOfType<LODManager>();
        foreach (LODManager m in managers)
            m.SetLODDebugView(t.isOn);
    }

    public void ToggleBenchmark(Toggle t = null)
    {
        BenchmarkWalker benchmarkWalker = GetComponent<BenchmarkWalker>();
        if(benchmarkWalker != null)
        {
            benchmarkWalker.enabled = !benchmarkWalker.enabled;
            PlayerSpawn playerSpawn = GetComponent<PlayerSpawn>();
            if (playerSpawn != null)
                transform.position = playerSpawn.spawnPosition;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.ResetCenterOfMass();
            }
        }
    }

    public void ForceLOD(float f)
    {
        int level = (int)f;
        switch(level)
        {
            case -1:
                uiForceLODText.text = "Off";
                break;
            case 0:
                uiForceLODText.text = "0";
                break;
            case 1:
                uiForceLODText.text = "1";
                break;
            case 2:
                uiForceLODText.text = "2";
                break;
        }

        LODManager[] managers = FindObjectsOfType<LODManager>();
        foreach (LODManager m in managers)
            m.ForceLOD(level);
    }

    public void FreezeLODLevels(Toggle t = null)
    {
        LODManager[] managers = FindObjectsOfType<LODManager>();
        foreach (LODManager m in managers)
            m.freezeLOD = t.isOn;
    }

    /// <summary>
    /// This method will ensure only one specific type TActivate in a given group of components derived from the same TCategory type is enabled.
    /// This is used by the sample support code to select between different targeting, input, aim, and other handlers.
    /// </summary>
    /// <typeparam name="TCategory"></typeparam>
    /// <typeparam name="TActivate"></typeparam>
    /// <param name="target"></param>
    public static TActivate ActivateCategory<TCategory, TActivate>(GameObject target) where TCategory : MonoBehaviour where TActivate : MonoBehaviour
    {
        var components = target.GetComponents<TCategory>();
        Debug.Log("Activate " + typeof(TActivate) + " derived from " + typeof(TCategory) + "[" + components.Length + "]");
        TActivate result = null;
        for (int i = 0; i < components.Length; i++)
        {
            var c = (MonoBehaviour)components[i];
            var active = c.GetType() == typeof(TActivate);
            Debug.Log(c.GetType() + " is " + typeof(TActivate) + " = " + active);
            if (active)
            {
                result = (TActivate)c;
            }
            if (c.enabled != active)
            {
                c.enabled = active;
            }
        }
        return result;
    }

    /// <summary>
    /// This generic method is used for activating a specific set of components in the LocomotionController. This is just one way 
    /// to achieve the goal of enabling one component of each category (input, aim, target, orientation and transition) that
    /// the teleport system requires.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TAim"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TOrientation"></typeparam>
    /// <typeparam name="TTransition"></typeparam>
    protected void ActivateHandlers<TInput, TAim, TTarget, TOrientation, TTransition>()
        where TInput : TeleportInputHandler
        where TAim : TeleportAimHandler
        where TTarget : TeleportTargetHandler
        where TOrientation : TeleportOrientationHandler
        where TTransition : TeleportTransition
    {
        ActivateInput<TInput>();
        ActivateAim<TAim>();
        ActivateTarget<TTarget>();
        ActivateOrientation<TOrientation>();
        ActivateTransition<TTransition>();
    }

    protected void ActivateInput<TActivate>() where TActivate : TeleportInputHandler
    {
        ActivateCategory<TeleportInputHandler, TActivate>();
    }

    protected void ActivateAim<TActivate>() where TActivate : TeleportAimHandler
    {
        ActivateCategory<TeleportAimHandler, TActivate>();
    }

    protected void ActivateTarget<TActivate>() where TActivate : TeleportTargetHandler
    {
        ActivateCategory<TeleportTargetHandler, TActivate>();
    }

    protected void ActivateOrientation<TActivate>() where TActivate : TeleportOrientationHandler
    {
        ActivateCategory<TeleportOrientationHandler, TActivate>();
    }

    protected void ActivateTransition<TActivate>() where TActivate : TeleportTransition
    {
        ActivateCategory<TeleportTransition, TActivate>();
    }

    protected TActivate ActivateCategory<TCategory, TActivate>() where TCategory : MonoBehaviour where TActivate : MonoBehaviour
    {
        return ActivateCategory<TCategory, TActivate>(lc.gameObject);
    }

    protected void UpdateToggle(Toggle toggle, bool enabled)
    {
        if (enabled != toggle.isOn)
        {
            toggle.isOn = enabled;
        }
    }

    void SetupNonCap()
    {
        var input = TeleportController.GetComponent<TeleportInputHandlerTouch>();
        input.InputMode = TeleportInputHandlerTouch.InputModes.SeparateButtonsForAimAndTeleport;
        input.AimButton = OVRInput.RawButton.A;
        input.TeleportButton = OVRInput.RawButton.A;
    }

    void SetupTeleportDefaults()
    {
        TeleportController.enabled = true;
        lc.PlayerController.EnableFreeFlight = false;
        //lc.PlayerController.SnapRotation = true;
        lc.PlayerController.RotationEitherThumbstick = false;
        //lc.PlayerController.FixedSpeedSteps = 0;
        TeleportController.EnableMovement(false, false, false, false);
        TeleportController.EnableRotation(false, false, false, false);

        var input = TeleportController.GetComponent<TeleportInputHandlerTouch>();
        input.InputMode = TeleportInputHandlerTouch.InputModes.CapacitiveButtonForAimAndTeleport;
        input.AimButton = OVRInput.RawButton.A;
        input.TeleportButton = OVRInput.RawButton.A;
        input.CapacitiveAimAndTeleportButton = TeleportInputHandlerTouch.AimCapTouchButtons.A;
        input.FastTeleport = false;

        var hmd = TeleportController.GetComponent<TeleportInputHandlerHMD>();
        hmd.AimButton = OVRInput.RawButton.A;
        hmd.TeleportButton = OVRInput.RawButton.A;

        var orient = TeleportController.GetComponent<TeleportOrientationHandlerThumbstick>();
        orient.Thumbstick = OVRInput.Controller.LTouch;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }


    protected GameObject AddInstance(GameObject template, string label)
    {
        var go = Instantiate(template);
        go.transform.SetParent(transform, false);
        go.name = label;
        return go;
    }

    // Teleport between node with A buttons. Display laser to node. Allow snap turns.
    void SetupNodeTeleport()
    {
        SetupTeleportDefaults();
        SetupNonCap();
        //lc.PlayerController.SnapRotation = true;
        //lc.PlayerController.FixedSpeedSteps = 1;
        lc.PlayerController.RotationEitherThumbstick = true;
        TeleportController.EnableRotation(true, false, false, true);
        ActivateHandlers<TeleportInputHandlerTouch, TeleportAimHandlerLaser, TeleportTargetHandlerPhysical, TeleportOrientationHandlerThumbstick, TeleportTransitionBlink>();
        var input = TeleportController.GetComponent<TeleportInputHandlerTouch>();
        input.AimingController = OVRInput.Controller.RTouch;
        //var input = TeleportController.GetComponent<TeleportAimHandlerLaser>();
        //input.AimingController = OVRInput.Controller.RTouch;
    }

    // Symmetrical controls. Forward or back on stick initiates teleport, then stick allows orient.
    // Snap turns allowed.
    void SetupTwoStickTeleport()
    {
        SetupTeleportDefaults();
        TeleportController.EnableRotation(true, false, false, true);
        TeleportController.EnableMovement(false, false, false, false);
        //lc.PlayerController.SnapRotation = true;
        lc.PlayerController.RotationEitherThumbstick = true;
        //lc.PlayerController.FixedSpeedSteps = 1;

        var input = TeleportController.GetComponent<TeleportInputHandlerTouch>();
        input.InputMode = TeleportInputHandlerTouch.InputModes.ThumbstickTeleportForwardBackOnly;
        input.AimingController = OVRInput.Controller.Touch;
        ActivateHandlers<TeleportInputHandlerTouch, TeleportAimHandlerParabolic, TeleportTargetHandlerPhysical, TeleportOrientationHandlerThumbstick, TeleportTransitionBlink>();
        var orient = TeleportController.GetComponent<TeleportOrientationHandlerThumbstick>();
        orient.Thumbstick = OVRInput.Controller.Touch;
    }

    // Shut down teleport. Basically reverts to OVRPlayerController.
    void SetupWalkOnly()
    {
        SetupTeleportDefaults();
        TeleportController.enabled = false;
        lc.PlayerController.EnableLinearMovement = true;
        lc.PlayerController.EnableRotation = true;
        //lc.PlayerController.SnapRotation = true;
        lc.PlayerController.RotationEitherThumbstick = false;
        //lc.PlayerController.FixedSpeedSteps = 1;
    }

    // 
    void SetupLeftStrafeRightTeleport()
    {
        SetupTeleportDefaults();
        TeleportController.EnableRotation(true, false, false, true);
        TeleportController.EnableMovement(true, false, false, false);
        //lc.PlayerController.SnapRotation = true;
        //lc.PlayerController.FixedSpeedSteps = 1;

        var input = TeleportController.GetComponent<TeleportInputHandlerTouch>();
        input.InputMode = TeleportInputHandlerTouch.InputModes.ThumbstickTeleportForwardBackOnly;
        input.AimingController = OVRInput.Controller.RTouch;
        ActivateHandlers<TeleportInputHandlerTouch, TeleportAimHandlerParabolic, TeleportTargetHandlerPhysical, TeleportOrientationHandlerThumbstick, TeleportTransitionBlink>();
        var orient = TeleportController.GetComponent<TeleportOrientationHandlerThumbstick>();
        orient.Thumbstick = OVRInput.Controller.RTouch;
    }

    void SetupFreeFlying()
    {
        SetupWalkOnly();
        lc.PlayerController.EnableFreeFlight = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
        }
    }
}
