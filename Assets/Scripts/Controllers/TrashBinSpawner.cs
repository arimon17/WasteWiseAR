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

    private readonly List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private readonly GameObject[] spawnedBins = new GameObject[BinCount];
    private readonly Transform[] binLids = new Transform[BinCount];

    // Label melayang di atas tong agar selalu terbaca dan tidak tertutup badan tong.
    private static readonly Vector3 BinLabelOffset = new Vector3(0f, 0.40f, 0f);
    private const float BinLabelFontSize = 5f;
    private const float TargetBinHeight = 0.32f; // tinggi tong dunia nyata (m)
    private const float MinBinSpacing = 0.35f; // jarak center-to-center antar tong (m)

    public bool HasPlacedBins => HasAllBins();

    private void Awake()
    {
        Instance = this;
        ForceLoadPrefabs();
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

    // Fallback runtime: isi referensi prefab yang kosong dari Resources (path relatif Assets/Resources).
    private void ForceLoadPrefabs()
    {
        if (organicBinPrefab == null) organicBinPrefab = Resources.Load<GameObject>("Prefabs/Bins/Bin_Organik");
        if (inorganicBinPrefab == null) inorganicBinPrefab = Resources.Load<GameObject>("Prefabs/Bins/Bin_Anorganik");
        if (b3BinPrefab == null) b3BinPrefab = Resources.Load<GameObject>("Prefabs/Bins/Bin_B3");
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
            NormalizeBinHeight(spawnedBins[i]);
            EnsureKinematicBody(spawnedBins[i]);
            binLids[i] = spawnedBins[i].transform.Find("Lid");
            AddBinLabel(spawnedBins[i], category);
        }
    }

    public Transform GetLid(TrashBinCategory category)
    {
        int index = (int)category;
        return index >= 0 && index < binLids.Length ? binLids[index] : null;
    }

    private static void AddBinLabel(GameObject bin, TrashBinCategory category)
    {
        FloatingLabel.Create(
            bin.transform,
            GetLabelText(category),
            BinLabelOffset,
            BinLabelFontSize,
            GetLabelColor(category));
    }

    private static Color GetLabelColor(TrashBinCategory category)
    {
        switch (category)
        {
            case TrashBinCategory.Organik:
                return new Color(0.62f, 1f, 0.66f);
            case TrashBinCategory.Anorganik:
                return new Color(1f, 0.94f, 0.55f);
            default:
                return new Color(1f, 0.62f, 0.62f);
        }
    }

    private static string GetLabelText(TrashBinCategory category)
    {
        switch (category)
        {
            case TrashBinCategory.Organik:
                return "ORGANIK";
            case TrashBinCategory.Anorganik:
                return "ANORGANIK";
            default:
                return "B3";
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
            binLids[i] = null;
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

    // Rigidbody kinematic agar trigger mulut tong tetap mendeteksi sampah tanpa tong ikut fisika.
    private static void EnsureKinematicBody(GameObject bin)
    {
        Rigidbody body = bin.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = bin.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
    }

    // Prefab lama bisa lebih tinggi dari 0.32 m; skala root agar label & mulut tong konsisten.
    private static void NormalizeBinHeight(GameObject bin)
    {
        Renderer[] renderers = bin.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        if (bounds.size.y > 0.0001f)
        {
            bin.transform.localScale *= TargetBinHeight / bounds.size.y;
        }
    }

    private Vector3 GetBinPosition(Vector3 center, Vector3 rowRight, int index)
    {
        float offset = (index - 1) * MinBinSpacing;
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

    // Kunci hanya sumbu Y: tong tegak lurus lantai dan mulut menghadap kamera AR.
    private Quaternion GetFacingRotation(Vector3 binPosition)
    {
        Transform viewCamera = Camera.main != null ? Camera.main.transform : arCamera;

        if (viewCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 lookPos = viewCamera.position - binPosition;
        lookPos.y = 0f;

        if (lookPos.sqrMagnitude < 0.0001f)
        {
            lookPos = -GetCameraForward();
        }

        return Quaternion.LookRotation(lookPos.normalized, Vector3.up);
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
