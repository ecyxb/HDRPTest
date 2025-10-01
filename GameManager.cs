using System.Collections;
using System.Collections.Generic;
using EventFramework;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public GameObject primaryPlayerPrefab;
    private List<CameraTargetMono> cameraTargetMonos = new List<CameraTargetMono>(4);
    public Timer Timer { get; private set; } = new Timer();

    public Volume[] allVolumes;

    // Start is called before the first frame update
    void Awake()
    {
        Timer.Awake();
        allVolumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        G.gameManager = this;
    }

    void Start()
    {
        GameObject playerObject = Instantiate(primaryPlayerPrefab);
        playerObject.transform.position = new Vector3(-6, 3.5f, 0); // Set initial position

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
        Timer.Update();
    }

    void FixedUpdate()
    {
        Timer.FixedUpdate();
    }
    void LateUpdate()
    {
        Timer.LateUpdate();
    }

    void OnDestroy()
    {
        cameraTargetMonos.Clear();
        G.gameManager = null;
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
