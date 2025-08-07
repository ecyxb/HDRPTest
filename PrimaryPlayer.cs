using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;


public class PrimaryPlayer : MonoBehaviour
{
    private Cinemachine.CinemachineVirtualCamera takePhotoVirtualCamera;
    public TakePhotoCameraMono takePhotoCameraMono { get; private set; }
    public Camera RenderCamera => takePhotoCameraMono.GetComponent<Camera>();
    public RenderTexture computeCameraTexture;
    public PhotoCalculator photoCalculator { get; private set; }


    public StateComp stateComp { get; private set; }
    public AttrComp attrComp { get; private set; }
    public TakePhotoCameraComp takePhotoCameraComp { get; private set; }
    public PlayerMovementComp playerMovementComp { get; private set; }
    public SfxComp sfxComp { get; private set; }


    public bool IsFirstPerson { get; private set; } = true;

    // Start is called before the first frame update
    void Awake()
    {
        this.Assert(G.player == null, "PrimaryPlayer already initialized.");
        G.player = this;

        GameObject takePhotoCameraObject = Instantiate(Resources.Load<GameObject>("Prefabs/TakePhotoMainCamera"));
        takePhotoCameraMono = takePhotoCameraObject.GetComponent<TakePhotoCameraMono>();
        photoCalculator = new PhotoCalculator(new Vector2Int(computeCameraTexture.width, computeCameraTexture.height));
        takePhotoCameraObject.transform.position = transform.position + new Vector3(0, 1.5f, 0);
        // GetComponent<TakePhotoController>().CinemachineCameraTarget = takePhotoCameraObject;

        takePhotoVirtualCamera = Instantiate(Resources.Load<GameObject>("Prefabs/TakePhotoVirtualCamera")).GetComponent<Cinemachine.CinemachineVirtualCamera>();
        var virtualCameraTransform = takePhotoVirtualCamera.transform;
        virtualCameraTransform.position = takePhotoCameraObject.transform.position;
        virtualCameraTransform.SetParent(gameObject.transform);
        virtualCameraTransform.localRotation = Quaternion.Euler(0, 0, 0);

        stateComp = new StateComp(this);
        attrComp = new AttrComp(this);
        takePhotoCameraComp = new TakePhotoCameraComp(this);
        playerMovementComp = new PlayerMovementComp(this);
        sfxComp = new SfxComp(this);

    }

    void Start()
    {
        stateComp.CompStart();
        attrComp.CompStart();
        takePhotoCameraComp.CompStart();
        playerMovementComp.CompStart();
        sfxComp.CompStart();

        EnterTakePhotoMode();

    }

    void OnDestroy()
    {
        stateComp.CompDestroy();
        attrComp.CompDestroy();
        takePhotoCameraComp.CompDestroy();
        playerMovementComp.CompDestroy();
        sfxComp.CompDestroy();
        G.player = null;
    }

    // Update is called once per frame
    void Update()
    {
        sfxComp.RefreshSfxParams_Update();
        
    }

    void LateUpdate()
    {
        takePhotoCameraComp.LateUpdate_TryAutoFocus();
        UpdateRenderCameraData(Time.deltaTime);
    }

    void FixedUpdate()
    {
        playerMovementComp.FixedUpdate_Movement();
    }

    private void EnterTakePhotoMode()
    {
        // G.UI.InstantiatePanel("PanelTakePhotoScreenOutput");
        G.UI.EnsurePanel<PanelTakePhotoMain>("PanelTakePhotoMain");
        // takePhotoVirtualCameraObject.SetActive(true);
        // takePhotoCameraObject.SetActive(true);
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
        transform.Rotate(Vector3.up * angle);
    }
    public void SetYawSelf(float yaw)
    {
        Quaternion q = Quaternion.Euler(0, yaw, 0);
        transform.rotation = q;
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
