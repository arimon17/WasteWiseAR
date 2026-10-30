using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class WasteGameManager : MonoBehaviour
{
    [Serializable]
    private struct WasteDefinition
    {
        public string itemName;
        public TrashCategory category;
        [TextArea] public string explanation;
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static readonly Color ItemColor = new Color(0.82f, 0.78f, 0.68f);
    private static readonly Color CorrectColor = new Color(0f, 0.8f, 0.15f);
    private static readonly Color WrongColor = new Color(0.9f, 0.1f, 0.1f);

    private const float BlinkInterval = 0.15f;
    private const float MaxRayDistance = 10f;

    [Header("Sesi")]
    [SerializeField, Min(1)] private int sessionQuota = 8;
    [SerializeField, Min(0.1f)] private float feedbackDuration = 1f;

    [Header("Spawn")]
    [SerializeField] private Transform spawnAnchor;
    [SerializeField, Min(0.1f)] private float spawnDistance = 1.2f;
    [SerializeField, Min(0.01f)] private float itemScale = 0.15f;

    [Header("Referensi AR")]
    [SerializeField] private TrashBinSpawner binSpawner;
    [SerializeField] private Camera arCamera;

    [Header("Variasi Sampah (kosong = pakai daftar bawaan)")]
    [SerializeField] private WasteDefinition[] wasteDefinitions;

    public event Action<WasteItem, bool, string> OnAnswerEvaluated;
    public event Action<int, int> OnSessionCompleted;

    private readonly List<WasteDefinition> pool = new List<WasteDefinition>();
    private WasteItem activeItem;
    private WasteItem selectedItem;
    private int sortedCount;
    private int wrongCount;
    private int lastPoolIndex = -1;
    private bool sessionRunning;
    private bool sessionFinished;

    private Coroutine feedbackRoutine;
    private MeshRenderer[] feedbackRenderers;

    public bool IsSessionRunning => sessionRunning;
    public int SortedCount => sortedCount;
    public int WrongCount => wrongCount;
    public int SessionQuota => sessionQuota;
    public string ActiveItemName => activeItem != null ? activeItem.ItemName : string.Empty;

    private void Awake()
    {
        BuildPool();
        ResolveReferences();

        if (binSpawner == null)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager: TrashBinSpawner tidak ditemukan di scene.");
        }

        if (arCamera == null)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager: kamera AR tidak ditemukan; spawn dan tap tidak akan berjalan.");
        }
    }

    private void Update()
    {
        if (!sessionRunning && !sessionFinished && binSpawner != null && binSpawner.HasPlacedBins)
        {
            StartSession();
        }

        if (sessionRunning && TryGetTapPosition(out Vector2 screenPosition))
        {
            HandleTap(screenPosition);
        }
    }

    public void StartSession()
    {
        if (sessionRunning || sessionFinished)
        {
            return;
        }

        if (binSpawner == null || !binSpawner.HasPlacedBins)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager: tempat sampah belum ditempatkan. Tap lantai untuk menaruh 3 tempat sampah terlebih dahulu.");
            return;
        }

        // Satu sumber input saja: hentikan TrashBinSpawner agar tap tidak memindahkan tempat sampah.
        binSpawner.enabled = false;

        sessionRunning = true;
        sortedCount = 0;
        wrongCount = 0;
        SpawnNext();
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (arCamera == null)
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance))
        {
            return;
        }

        WasteItem item = hit.collider.GetComponentInParent<WasteItem>();

        if (item != null)
        {
            SelectItem(item);
            return;
        }

        if (binSpawner != null &&
            binSpawner.TryGetBinCategory(hit.collider.gameObject, out TrashBinSpawner.TrashBinCategory binCategory))
        {
            Evaluate(binCategory);
        }
    }

    private void SelectItem(WasteItem item)
    {
        selectedItem = item;
        Debug.Log($"[WasteWise] Sampah dipilih: {item.ItemName}. Sekarang tap tempat sampah tujuan.");
    }

    private void Evaluate(TrashBinSpawner.TrashBinCategory binCategory)
    {
        if (selectedItem == null)
        {
            Debug.Log("[WasteWise] Tap objek sampah dulu sebelum memilih tempat sampah.");
            return;
        }

        WasteItem item = selectedItem;
        selectedItem = null;

        bool correct = (int)item.CorrectCategory == (int)binCategory;
        GameObject bin = binSpawner.GetBin(binCategory);

        if (bin != null)
        {
            PlayFeedback(bin, correct);
        }

        string verdict = correct ? "BENAR" : "SALAH";
        Debug.Log($"[WasteWise] {verdict}: {item.ItemName} dimasukkan ke {binCategory}. {item.Explanation}");
        OnAnswerEvaluated?.Invoke(item, correct, item.Explanation);

        if (!correct)
        {
            wrongCount++;
            return;
        }

        sortedCount++;
        activeItem = null;
        Destroy(item.gameObject);

        if (sortedCount >= sessionQuota)
        {
            FinishSession();
        }
        else
        {
            SpawnNext();
        }
    }

    private void SpawnNext()
    {
        if (sortedCount >= sessionQuota)
        {
            FinishSession();
            return;
        }

        if (activeItem != null)
        {
            Destroy(activeItem.gameObject);
        }

        activeItem = CreateWasteItem(PickDefinition());
    }

    private WasteItem CreateWasteItem(WasteDefinition definition)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "WasteItem";
        go.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
        go.transform.localScale = Vector3.one * itemScale;

        WasteItem item = go.AddComponent<WasteItem>();
        item.Configure(definition.itemName, definition.category, definition.explanation);
        ApplyColor(go.GetComponent<MeshRenderer>(), ItemColor);

        return item;
    }

    private void FinishSession()
    {
        sessionRunning = false;
        sessionFinished = true;

        if (activeItem != null)
        {
            Destroy(activeItem.gameObject);
            activeItem = null;
        }

        selectedItem = null;

        OnSessionCompleted?.Invoke(sortedCount, sortedCount + wrongCount);
        Debug.Log($"[WasteWise] Sesi latihan selesai. Benar: {sortedCount}, Salah: {wrongCount}.");
    }

    private void PlayFeedback(GameObject bin, bool correct)
    {
        MeshRenderer[] renderers = bin.GetComponentsInChildren<MeshRenderer>(true);

        if (renderers.Length == 0)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            RestoreFeedbackRenderers();
        }

        feedbackRenderers = renderers;
        feedbackRoutine = StartCoroutine(BlinkRoutine(renderers, correct ? CorrectColor : WrongColor));
    }

    private IEnumerator BlinkRoutine(MeshRenderer[] renderers, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        float elapsed = 0f;
        bool on = true;

        while (elapsed < feedbackDuration)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (on)
                {
                    block.SetColor(BaseColorId, color);
                    block.SetColor(ColorId, color);
                    renderer.SetPropertyBlock(block);
                }
                else
                {
                    renderer.SetPropertyBlock(null);
                }
            }

            on = !on;
            yield return new WaitForSeconds(BlinkInterval);
            elapsed += BlinkInterval;
        }

        RestoreFeedbackRenderers();
        feedbackRoutine = null;
        feedbackRenderers = null;
    }

    private void RestoreFeedbackRenderers()
    {
        if (feedbackRenderers == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in feedbackRenderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }

        feedbackRenderers = null;
    }

    private WasteDefinition PickDefinition()
    {
        if (pool.Count <= 0)
        {
            return default;
        }

        if (pool.Count == 1)
        {
            lastPoolIndex = 0;
            return pool[0];
        }

        int index;

        do
        {
            index = UnityEngine.Random.Range(0, pool.Count);
        }
        while (index == lastPoolIndex);

        lastPoolIndex = index;
        return pool[index];
    }

    private void BuildPool()
    {
        pool.Clear();

        if (wasteDefinitions != null && wasteDefinitions.Length > 0)
        {
            pool.AddRange(wasteDefinitions);
            return;
        }

        pool.Add(new WasteDefinition
        {
            itemName = "Kulit Pisang",
            category = TrashCategory.Organik,
            explanation = "Kulit buah adalah sampah organik yang membusuk alami dan bisa dijadikan kompos."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Sisa Nasi",
            category = TrashCategory.Organik,
            explanation = "Sisa makanan termasuk organik; pisahkan agar bisa diolah menjadi kompos."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Bungkus Mi Instan",
            category = TrashCategory.Anorganik,
            explanation = "Bungkus plastik sulit terurai; kumpulkan dengan sampah anorganik untuk didaur ulang."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Botol Plastik",
            category = TrashCategory.Anorganik,
            explanation = "Botol plastik PET bernilai daur ulang; bilas lalu masukkan ke wadah anorganik."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Kardus",
            category = TrashCategory.Anorganik,
            explanation = "Kardus berbahan kertas dapat didaur ulang menjadi produk baru."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Kaleng Minuman",
            category = TrashCategory.Anorganik,
            explanation = "Kaleng aluminium bernilai jual dan mudah didaur ulang."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Baterai Bekas",
            category = TrashCategory.B3,
            explanation = "Baterai mengandung logam berat berbahaya; buang ke tempat sampah B3."
        });
        pool.Add(new WasteDefinition
        {
            itemName = "Lampu Neon Bekas",
            category = TrashCategory.B3,
            explanation = "Lampu neon mengandung merkuri sehingga termasuk limbah B3."
        });
    }

    private void ApplyColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnAnchor != null)
        {
            return spawnAnchor.position;
        }

        if (arCamera == null)
        {
            return transform.position + Vector3.up * 0.2f;
        }

        return arCamera.transform.position + arCamera.transform.forward * spawnDistance;
    }

    private Quaternion GetSpawnRotation()
    {
        if (arCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 direction = GetSpawnPosition() - arCamera.transform.position;

        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private void ResolveReferences()
    {
        if (binSpawner == null)
        {
            binSpawner = FindAnyObjectByType<TrashBinSpawner>();
        }

        if (arCamera != null)
        {
            return;
        }

        ARCameraManager cameraManager = FindAnyObjectByType<ARCameraManager>(FindObjectsInactive.Include);

        if (cameraManager != null)
        {
            arCamera = cameraManager.GetComponent<Camera>();

            if (arCamera == null)
            {
                arCamera = cameraManager.GetComponentInChildren<Camera>(true);
            }
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private bool TryGetTapPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            screenPosition = mouse.position.ReadValue();
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

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
#endif
        screenPosition = default;
        return false;
    }
}
