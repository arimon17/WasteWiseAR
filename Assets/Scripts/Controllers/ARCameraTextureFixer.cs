using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[DisallowMultipleComponent]
public class ARCameraTextureFixer : MonoBehaviour
{
    private const float RefreshDelaySeconds = 0.5f;

    [Header("AR References")]
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private ARSession arSession;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        StartCoroutine(RefreshArComponentsAfterDelay());
    }

    private IEnumerator RefreshArComponentsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RefreshDelaySeconds);

        ResolveReferences();

        if (cameraManager == null && arSession == null)
        {
            Debug.LogWarning("[WasteWise] ARCameraTextureFixer tidak menemukan ARCameraManager maupun ARSession.");
            yield break;
        }

        if (cameraManager != null)
        {
            cameraManager.enabled = false;
        }

        if (arSession != null)
        {
            arSession.enabled = false;
        }

        yield return null;

        if (arSession != null)
        {
            arSession.enabled = true;

            if (arSession.enabled)
            {
                Debug.Log("[WasteWise] ARSession di-refresh.");
            }
            else
            {
                Debug.LogWarning("[WasteWise] ARSession tidak dapat dimulai ulang. Periksa status ARCore dan XR Plug-in Management.");
            }
        }

        yield return null;

        if (cameraManager != null)
        {
            cameraManager.enabled = true;
            Debug.Log("[WasteWise] ARCameraManager di-refresh agar tekstur ARCore terhubung ke URP Background Pass.");
        }
    }

    private void ResolveReferences()
    {
        if (cameraManager == null)
        {
            cameraManager = GetComponentInChildren<ARCameraManager>(true);
        }

        if (cameraManager == null)
        {
            cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
        }

        if (arSession == null)
        {
            arSession = GetComponentInChildren<ARSession>(true);
        }

        if (arSession == null)
        {
            arSession = FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
        }
    }
}
