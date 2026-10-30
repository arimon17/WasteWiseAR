using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class TrashBinSpawner : MonoBehaviour
{
    public enum TrashBinCategory
    {
        Organik = 0,
        Anorganik = 1,
        B3 = 2
    }

    private const int BinCount = 3;

    public static TrashBinSpawner Instance { get; private set; }

    [Header("Bin Prefabs")]
    [SerializeField] private GameObject organicBinPrefab;
    [SerializeField] private GameObject inorganicBinPrefab;
    [SerializeField] private GameObject b3BinPrefab;

    [Header("AR References")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Transform arCamera;

    [Header("Placement")]
    [SerializeField, Min(0f)] private float horizontalOffset = 0.35f;

    private readonly List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private readonly GameObject[] spawnedBins = new GameObject[BinCount];

    public bool HasPlacedBins => HasAllBins();

    private void Awake()
    {
        Instance = this;
        ResolveReferences();

        if (!HasAllPrefabs())
        {
            Debug.LogWarning("[WasteWise] TrashBinSpawner: referensi prefab Organik, Anorganik, dan B3 harus diisi lengkap.");
        }

        if (raycastManager == null)
        {
            Debug.LogWarning("[WasteWise] TrashBinSpawner: ARRaycastManager tidak ditemukan. Pastikan komponen AR Raycast Manager terpasang pada XR Origin.");
        }

        if (arCamera == null)
        {
            Debug.LogWarning("[WasteWise] TrashBinSpawner: kamera AR tidak ditemukan. Rotasi tempat sampah akan memakai arah default.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (TryGetTouchBegan(out Vector2 screenPosition))
        {
            TryPlaceBins(screenPosition);
        }
    }

    public GameObject GetBin(TrashBinCategory category)
    {
        int index = (int)category;

        if (index < 0 || index >= spawnedBins.Length)
        {
            return null;
        }

        return spawnedBins[index];
    }

    public bool TryGetBinCategory(GameObject hitObject, out TrashBinCategory category)
    {
        Transform current = hitObject != null ? hitObject.transform : null;

        while (current != null)
        {
            for (int i = 0; i < spawnedBins.Length; i++)
            {
                if (spawnedBins[i] != null && current.gameObject == spawnedBins[i])
                {
                    category = (TrashBinCategory)i;
                    return true;
                }
            }

            current = current.parent;
        }

        category = default;
        return false;
    }

    private void TryPlaceBins(Vector2 screenPosition)
    {
        if (raycastManager == null || !HasAllPrefabs())
        {
            return;
        }

        if (!raycastManager.Raycast(screenPosition, hitResults, TrackableType.PlaneWithinPolygon) || hitResults.Count == 0)
        {
            return;
        }

        Pose hitPose = hitResults[0].pose;

        if (HasAllBins())
        {
            MoveBins(hitPose);
            Debug.Log("[WasteWise] Tiga tempat sampah dipindahkan ke titik tap baru.");
        }
        else
        {
            DestroySpawnedBins();
            SpawnBins(hitPose);
            Debug.Log("[WasteWise] Tiga tempat sampah AR berhasil ditempatkan.");
        }
    }

    private void SpawnBins(Pose hitPose)
    {
        Vector3 rowRight = GetRowRight();

        for (int i = 0; i < spawnedBins.Length; i++)
        {
            TrashBinCategory category = (TrashBinCategory)i;
            Vector3 position = GetBinPosition(hitPose.position, rowRight, i);
            Quaternion rotation = GetFacingRotation(position);

            spawnedBins[i] = Instantiate(GetPrefab(category), position, rotation);
            spawnedBins[i].name = GetBinName(category);
        }
    }

    private void MoveBins(Pose hitPose)
    {
        Vector3 rowRight = GetRowRight();

        for (int i = 0; i < spawnedBins.Length; i++)
        {
            Vector3 position = GetBinPosition(hitPose.position, rowRight, i);
            Quaternion rotation = GetFacingRotation(position);
            spawnedBins[i].transform.SetPositionAndRotation(position, rotation);
        }
    }

    private void DestroySpawnedBins()
    {
        for (int i = 0; i < spawnedBins.Length; i++)
        {
            if (spawnedBins[i] != null)
            {
                Destroy(spawnedBins[i]);
            }

            spawnedBins[i] = null;
        }
    }

    private bool HasAllBins()
    {
        for (int i = 0; i < spawnedBins.Length; i++)
        {
            if (spawnedBins[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAllPrefabs()
    {
        return organicBinPrefab != null && inorganicBinPrefab != null && b3BinPrefab != null;
    }

    private GameObject GetPrefab(TrashBinCategory category)
    {
        switch (category)
        {
            case TrashBinCategory.Organik:
                return organicBinPrefab;
            case TrashBinCategory.Anorganik:
                return inorganicBinPrefab;
            case TrashBinCategory.B3:
                return b3BinPrefab;
            default:
                return null;
        }
    }

    private static string GetBinName(TrashBinCategory category)
    {
        return $"TrashBin_{category}";
    }

    private Vector3 GetBinPosition(Vector3 center, Vector3 rowRight, int index)
    {
        float offset = (index - 1) * horizontalOffset;
        return center + rowRight * offset;
    }

    private Vector3 GetRowRight()
    {
        Vector3 cameraForward = GetCameraForward();
        Vector3 rowRight = Vector3.Cross(Vector3.up, cameraForward);

        return rowRight.sqrMagnitude > 0.0001f ? rowRight.normalized : Vector3.right;
    }

    private Vector3 GetCameraForward()
    {
        if (arCamera == null)
        {
            return Vector3.forward;
        }

        Vector3 forward = arCamera.forward;
        forward.y = 0f;

        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private Quaternion GetFacingRotation(Vector3 binPosition)
    {
        if (arCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 direction = arCamera.position - binPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -GetCameraForward();
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void ResolveReferences()
    {
        if (raycastManager == null)
        {
            raycastManager = GetComponent<ARRaycastManager>();
        }

        if (raycastManager == null)
        {
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
        }

        if (arCamera == null)
        {
            ARCameraManager cameraManager = GetComponentInChildren<ARCameraManager>(true);

            if (cameraManager == null)
            {
                cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);
            }

            if (cameraManager != null)
            {
                arCamera = cameraManager.transform;
            }
        }

        if (arCamera == null && Camera.main != null)
        {
            arCamera = Camera.main.transform;
        }
    }

    private bool TryGetTouchBegan(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);

            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }
#endif
        screenPosition = default;
        return false;
    }
}
