using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using EventFramework;

public class PrimaryPlayer : EventObject
{

    private Cinemachine.CinemachineVirtualCamera takePhotoVirtualCamera;
    public TakePhotoCameraMono takePhotoCameraMono { get; private set; }
    public Camera RenderCamera => takePhotoCameraMono.GetComponent<Camera>();
    public RenderTexture computeCameraTexture;
    public PhotoCalculator photoCalculator { get; private set; }


    public StateComp stateComp  => (StateComp)this["stateComp"];
    public AttrComp attrComp  => (AttrComp)this["attrComp"];
    public TakePhotoCameraComp takePhotoCameraComp  => (TakePhotoCameraComp)this["takePhotoCameraComp"];
    public PlayerMovementComp playerMovementComp  => (PlayerMovementComp)this["playerMovementComp"];
    public SfxComp sfxComp  => (SfxComp)this["sfxComp"];


    public bool IsFirstPerson { get; private set; } = true;

    // Start is called before the first frame update
    protected static Dictionary<string, UnionInt64> _DataMap => new Dictionary<string, UnionInt64>
    {
        { "stateComp", new StateComp() },
        { "attrComp", new AttrComp() },
        { "takePhotoCameraComp", new TakePhotoCameraComp() },
        { "playerMovementComp", new PlayerMovementComp() },
        { "sfxComp", new SfxComp() },
    };
    public PrimaryPlayer() : base(_DataMap, true)
    {
        this.gameobject = gameobject;
        computeCameraTexture = new RenderTexture(512, 512, 24);
        computeCameraTexture.enableRandomWrite = true;
        computeCameraTexture.Create();
    }
    protected override void OnStart()
    {
        this.Assert(G.player == null, "PrimaryPlayer already initialized.");
        G.player = this;

        GameObject takePhotoCameraObject = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/TakePhotoMainCamera"));
        takePhotoCameraMono = takePhotoCameraObject.GetComponent<TakePhotoCameraMono>();
        photoCalculator = new PhotoCalculator(new Vector2Int(computeCameraTexture.width, computeCameraTexture.height));
        takePhotoCameraObject.transform.position = gameobject.transform.position + new Vector3(0, 1.5f, 0);
        // GetComponent<TakePhotoController>().CinemachineCameraTarget = takePhotoCameraObject;

        takePhotoVirtualCamera = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/TakePhotoVirtualCamera")).GetComponent<Cinemachine.CinemachineVirtualCamera>();
        var virtualCameraTransform = takePhotoVirtualCamera.transform;
        virtualCameraTransform.position = takePhotoCameraObject.transform.position;
        virtualCameraTransform.SetParent(gameobject.transform);
        virtualCameraTransform.localRotation = Quaternion.Euler(0, 0, 0);

    }

    protected override void OnDestroy()
    {
        G.player = null;
    }

    // Update is called once per frame
    public override void Update()
    {
        sfxComp.RefreshSfxParams_Update();
        
    }

    public override void LateUpdate()
    {
        takePhotoCameraComp.LateUpdate_TryAutoFocus();
        UpdateRenderCameraData(Time.deltaTime);
    }

    public override void FixedUpdate()
    {
        playerMovementComp.FixedUpdate_Movement();
    }

    private void EnterTakePhotoMode()
    {
        // G.UI.InstantiatePanel("PanelTakePhotoScreenOutput");
        G.UI.EnsurePanel<PanelTakePhotoMain>("PanelTakePhotoMain");
        takePhotoVirtualCamera.gameObject.SetActive(true);
        takePhotoCameraMono.gameObject.SetActive(true);
    }
    public void SetExposureMainParams(float shutterSpeed, float aperture, int iso)
    {
        takePhotoVirtualCamera.m_Lens.ShutterSpeed = shutterSpeed;
        takePhotoVirtualCamera.m_Lens.Aperture = aperture;
        takePhotoVirtualCamera.m_Lens.Iso = iso;
    }
    public void SetCameraPitch(float pitch)
    {
        takePhotoVirtualCamera.transform.localRotation = Quaternion.Euler(pitch, 0.0f, 0.0f);
    }
    public float GetCameraPitch()
    {
        return Helpers.AngleNormalize180(takePhotoVirtualCamera.transform.localRotation.eulerAngles.x);
    }

    public void RotateYawSelf(float angle)
    {
        gameobject.transform.Rotate(Vector3.up * angle);
    }
    public void SetYawSelf(float yaw)
    {
        Quaternion q = Quaternion.Euler(0, yaw, 0);
        gameobject.transform.rotation = q;
    }

    void UpdateRenderCameraData(float delta)
    {
        var targetFOV = Helpers.FocalLengthToVerticalFOV(takePhotoVirtualCamera.m_Lens.SensorSize.y, takePhotoCameraComp.FocalLength);
        takePhotoVirtualCamera.m_Lens.FieldOfView = Helpers.FastFloatDiffLerp(takePhotoVirtualCamera.m_Lens.FieldOfView, targetFOV, 1f, delta * 10f);
        takePhotoVirtualCamera.m_Lens.FocusDistance = takePhotoCameraComp.FocusDistance;


        takePhotoVirtualCamera.m_Lens.ShutterSpeed = Helpers.FastFloatMultiDiffLerp(takePhotoVirtualCamera.m_Lens.ShutterSpeed, takePhotoCameraComp.ShutterSpeed, 2f, delta * 5f);
        takePhotoVirtualCamera.m_Lens.Aperture = Helpers.FastFloatMultiDiffLerp(takePhotoVirtualCamera.m_Lens.Aperture, takePhotoCameraComp.Aperture, 1f, delta * 5f);
        takePhotoVirtualCamera.m_Lens.Iso = (int)Helpers.FastFloatMultiDiffLerp(takePhotoVirtualCamera.m_Lens.Iso, takePhotoCameraComp.ISO, 1f, delta * 5f);
    }

    public void SetRenderCameraFocusDistance(float distance)
    {
        takePhotoVirtualCamera.m_Lens.FocusDistance = distance;
    }
    public float GetRenderCameraFocusDistance()
    {
        return takePhotoVirtualCamera.m_Lens.FocusDistance;
    }

}
