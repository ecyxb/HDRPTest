using System.Collections.Generic;
using UnityEngine;
using EventFramework;



public class GameManager : MonoBehaviour
{
    public GameObject primaryPlayerPrefab;
    private List<CameraTargetMono> cameraTargetMonos = new List<CameraTargetMono>(4);
    public Timer Timer { get; private set; } = new Timer();

    public UnityEngine.Rendering.Volume[] allVolumes;
    private HashSet<EventObject> allEventObject = new HashSet<EventObject>();

    // Start is called before the first frame update
    void Awake()
    {
        Timer.Awake();
        allVolumes = FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        G.gameManager = this;
    }

    void Start()
    {
        GameObject playerObject = Instantiate(primaryPlayerPrefab);
        playerObject.transform.position = new Vector3(-6, 3.5f, 0); // Set initial position
        AddEventObject<PrimaryPlayer>(playerObject);
    }

    void AddEventObject<T>(GameObject gameObject) where T : EventObject, new()
    {
        T e = new T();
        allEventObject.Add(e);
        e.BindGameObject(gameObject);
        e.__on_start__();
    }

    public void EnableVolumeForCamera(GameObject camera, string[] volumeNames, bool reset = false)
    {
        var cameraData = camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
        if (cameraData == null)
        {
            return;
        }
        if (reset)
        {
            cameraData.volumeLayerMask = 0;
        }
        foreach (var volume in allVolumes)
        {
            foreach (var name in volumeNames)
            {
                if (volume.name == name)
                {
                    volume.enabled = true;
                    cameraData.volumeLayerMask |= 1 << volume.gameObject.layer;
                }
            }
        }
    }
    public void SetRadialBlurParams(string volume, float blurFactor, Vector2 blurCenter, float clearRadius, float transitionWidth, int sampleCount = 6)
    {
        foreach (var vol in allVolumes)
        {
            if (vol.name != volume) continue;

            // 获取RadialBlurPostProcess组件并设置参数
            if (vol.profile == null)
            {
                Debug.LogWarning($"Volume '{volume}' does not have a profile.");
                return;
            }
            if (vol.profile.TryGet<RadialBlurPostProcess>(out var radialBlur))
            {
                radialBlur.blurFactor.value = blurFactor;
                radialBlur.blurCenter.value = blurCenter;
                radialBlur.clearRadius.value = clearRadius;
                radialBlur.transitionWidth.value = transitionWidth;
                radialBlur.sampleCount.value = sampleCount;
            }
            else
            {
                Debug.LogWarning($"RadialBlurPostProcess not found in volume '{volume}'.");
            }
        }
    }

    void Update()
    {

        foreach (var e in allEventObject)
        {
            if (e.NeedUpdate)
            {
                e.Update();
            }
        }
        Timer.Update();
    }

    void FixedUpdate()
    {

        foreach (var e in allEventObject)
        {
            if (e.NeedFixedUpdate)
            {
                e.FixedUpdate();
            }
        }
        Timer.FixedUpdate();
    }
    void LateUpdate()
    {
        foreach (var e in allEventObject)
        {
            if (e.NeedLateUpdate)
            {
                e.LateUpdate();
            }
        }
        Timer.LateUpdate();
    }

    void OnDestroy()
    {
        cameraTargetMonos.Clear();
        G.gameManager = null;
        foreach (var e in allEventObject)
        {
            e.__on_destroy__();
        }
    }

    public void RegisterCameraTarget(CameraTargetMono cameraTargetMono)
    {
        if (!cameraTargetMonos.Contains(cameraTargetMono))
        {
            cameraTargetMonos.Add(cameraTargetMono);
        }
    }

    public List<CameraTargetMono> GetCameraTargets()
    {
        return new List<CameraTargetMono>(cameraTargetMonos);
    }

}
