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

    private static readonly Color CorrectColor = new Color(0f, 0.8f, 0.15f);
    private static readonly Color WrongColor = new Color(0.9f, 0.1f, 0.1f);

    private const float BlinkInterval = 0.15f;
    private const float MaxRayDistance = 10f;
    private const float DropRadius = 0.30f; // jarak horizontal ke tempat sampah agar dianggap di atasnya
    private const float ReturnSpeed = 8f;  // laju Lerp kembali ke titik melayang awal
    private const float LidSpeed = 6f;     // laju animasi buka/tutup penutup tempat sampah
    private const float BinMouthHeight = 0.25f;  // tinggi mulut tong (1 tong ~0.20m) + celah di atasnya
    private const float SpawnFloorOffset = 0.05f; // sampah duduk sejajar lantai AR (floorY + 0.05m)
    private const float SpawnClearanceFromBins = 0.45f; // jarak spawn di depan barisan tong

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
    private int sortedCount;
    private int wrongCount;
    private int lastPoolIndex = -1;
    private bool sessionRunning;
    private bool sessionFinished;

    // Drag & drop
    private WasteItem movingItem;   // item yang sedang di-drag atau dikembalikan
    private bool dragging;
    private bool returning;
    private Plane dragPlane;
    private Vector3 itemHome;       // titik melayang awal item aktif
    private TrashBinSpawner.TrashBinCategory? hoveredBin;
    private readonly float[] lidOpen = new float[3];

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

        if (sessionRunning)
        {
            HandlePointer();
        }

        AnimateLids();
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
        movingItem = null;
        dragging = false;
        returning = false;
        hoveredBin = null;
        SpawnNext();
    }

    private void HandlePointer()
    {
        PointerState state = GetPointerState(out Vector2 screenPosition);

        if (returning)
        {
            if (state == PointerState.Down && TryPickItem(screenPosition, out WasteItem regrabbed))
            {
                BeginDrag(regrabbed);
                return;
            }

            StepReturn();
            return;
        }

        if (!dragging)
        {
            if (state == PointerState.Down && TryPickItem(screenPosition, out WasteItem picked))
            {
                BeginDrag(picked);
            }

            return;
        }

        if (state == PointerState.Held || state == PointerState.Down)
        {
            MoveDragged(screenPosition);
        }
        else if (state == PointerState.Up || state == PointerState.None)
        {
            DropDragged();
        }
    }

    private bool TryPickItem(Vector2 screenPosition, out WasteItem item)
    {
        item = null;

        if (arCamera == null)
        {
            return false;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance))
        {
            return false;
        }

        item = hit.collider.GetComponentInParent<WasteItem>();
        return item != null;
    }

    private void BeginDrag(WasteItem item)
    {
        movingItem = item;
        dragging = true;
        returning = false;
        // Bidang horizontal di ketinggian item agar gerakan jari terpetakan ke area lantai AR.
        dragPlane = new Plane(Vector3.up, item.transform.position);
        Debug.Log($"[WasteWise] Sampah diangkat: {item.ItemName}. Geser ke tempat sampah lalu lepas.");
    }

    private void MoveDragged(Vector2 screenPosition)
    {
        if (movingItem == null || arCamera == null)
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (dragPlane.Raycast(ray, out float distance))
        {
            movingItem.transform.position = ray.GetPoint(distance);
        }

        hoveredBin = FindBinNear(movingItem.transform.position);

        // Y diatur terpisah dari bidang drag: naik halus ke tinggi mulut tong agar tidak tenggelam.
        float targetY = dragPlane.distance;

        if (hoveredBin.HasValue && binSpawner != null)
        {
            GameObject hovered = binSpawner.GetBin(hoveredBin.Value);

            if (hovered != null)
            {
                targetY = hovered.transform.position.y + BinMouthHeight;
            }
        }

        Vector3 position = movingItem.transform.position;
        position.y = Mathf.MoveTowards(position.y, targetY, Time.deltaTime * ReturnSpeed);
        movingItem.transform.position = position;
    }

    private TrashBinSpawner.TrashBinCategory? FindBinNear(Vector3 position)
    {
        if (binSpawner == null)
        {
            return null;
        }

        TrashBinSpawner.TrashBinCategory? nearest = null;
        float best = DropRadius;

        for (int i = 0; i < lidOpen.Length; i++)
        {
            TrashBinSpawner.TrashBinCategory category = (TrashBinSpawner.TrashBinCategory)i;
            GameObject bin = binSpawner.GetBin(category);

            if (bin == null)
            {
                continue;
            }

            Vector3 delta = bin.transform.position - position;
            delta.y = 0f; // jarak horizontal (item melayang di atas mulut tempat sampah)
            float distance = delta.magnitude;

            if (distance < best)
            {
                best = distance;
                nearest = category;
            }
        }

        return nearest;
    }

    private void DropDragged()
    {
        dragging = false;

        if (movingItem == null)
        {
            hoveredBin = null;
            return;
        }

        if (hoveredBin.HasValue && binSpawner != null)
        {
            TrashBinSpawner.TrashBinCategory bin = hoveredBin.Value;
            hoveredBin = null;
            Evaluate(movingItem, bin);
            return;
        }

        // Lepas di luar tempat sampah: kembalikan ke titik melayang semula via Lerp.
        returning = true;
    }

    private void StepReturn()
    {
        if (movingItem == null)
        {
            returning = false;
            return;
        }

        Transform target = movingItem.transform;
        target.position = Vector3.Lerp(target.position, itemHome, Time.deltaTime * ReturnSpeed);

        if (Vector3.Distance(target.position, itemHome) < 0.01f)
        {
            target.position = itemHome;
            returning = false;
        }
    }

    private void AnimateLids()
    {
        if (binSpawner == null)
        {
            return;
        }

        for (int i = 0; i < lidOpen.Length; i++)
        {
            TrashBinSpawner.TrashBinCategory category = (TrashBinSpawner.TrashBinCategory)i;
            Transform lid = binSpawner.GetLid(category);

            if (lid == null)
            {
                continue;
            }

            bool open = dragging && hoveredBin.HasValue && hoveredBin.Value == category;
            lidOpen[i] = Mathf.MoveTowards(lidOpen[i], open ? 1f : 0f, Time.deltaTime * LidSpeed);
            lid.localRotation = Quaternion.Euler(-45f * lidOpen[i], 0f, 0f);
        }
    }

    private void Evaluate(WasteItem item, TrashBinSpawner.TrashBinCategory binCategory)
    {
        if (item == null)
        {
            movingItem = null;
            return;
        }

        bool correct = (int)item.CorrectCategory == (int)binCategory;
        GameObject bin = binSpawner != null ? binSpawner.GetBin(binCategory) : null;

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
            // Salah: sampah kembali melayang agar bisa dicoba lagi.
            returning = true;
            return;
        }

        sortedCount++;

        if (activeItem == item)
        {
            activeItem = null;
        }

        movingItem = null;
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
        itemHome = activeItem.transform.position;
        movingItem = null;
        dragging = false;
        returning = false;
        hoveredBin = null;
    }

    private WasteItem CreateWasteItem(WasteDefinition definition)
    {
        GameObject go = new GameObject("WasteItem");
        go.transform.SetPositionAndRotation(GetSpawnPosition(), GetSpawnRotation());
        go.transform.localScale = Vector3.one * itemScale;

        // Collider root menggantikan collider kubus placeholder sebagai target tap.
        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.05f, 0f);
        collider.size = new Vector3(0.95f, 0.95f, 0.95f);

        WasteItem item = go.AddComponent<WasteItem>();
        item.Configure(definition.itemName, definition.category, definition.explanation);

        WasteModelBuilder.Build(go.transform, definition.itemName, definition.category);

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

        movingItem = null;
        dragging = false;
        returning = false;
        hoveredBin = null;

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

    private Vector3 GetSpawnPosition()
    {
        Vector3 position;
        GameObject referenceBin = binSpawner != null
            ? binSpawner.GetBin(TrashBinSpawner.TrashBinCategory.Anorganik)
            : null;

        if (referenceBin != null)
        {
            // Sejajar di depan barisan tong, menghadap kamera.
            position = referenceBin.transform.position - GetHorizontalForward() * SpawnClearanceFromBins;
        }
        else if (spawnAnchor != null)
        {
            position = spawnAnchor.position;
        }
        else if (arCamera == null)
        {
            position = transform.position;
        }
        else
        {
            position = arCamera.transform.position + arCamera.transform.forward * spawnDistance;
        }

        // Dudukkan di dataran lantai AR (setinggi alas tong), bukan melayang di atas tong.
        position.y = GetFloorHeight() + SpawnFloorOffset;
        return position;
    }

    private float GetFloorHeight()
    {
        if (binSpawner != null)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject bin = binSpawner.GetBin((TrashBinSpawner.TrashBinCategory)i);

                if (bin != null)
                {
                    return bin.transform.position.y;
                }
            }
        }

        if (spawnAnchor != null)
        {
            return spawnAnchor.position.y;
        }

        return arCamera != null ? arCamera.transform.position.y : transform.position.y;
    }

    private Vector3 GetHorizontalForward()
    {
        if (arCamera == null)
        {
            return Vector3.forward;
        }

        Vector3 forward = arCamera.transform.forward;
        forward.y = 0f;

        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private Quaternion GetSpawnRotation()
    {
        if (arCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 direction = GetSpawnPosition() - arCamera.transform.position;
        direction.y = 0f; // sampah di lantai: hadapkan mendatar ke kamera, jangan menunduk.

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

    private enum PointerState
    {
        None,
        Down,
        Held,
        Up
    }

    private static PointerState GetPointerState(out Vector2 position)
    {
        position = default;

#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                position = touch.position.ReadValue();
                return PointerState.Down;
            }

            if (touch.press.isPressed)
            {
                position = touch.position.ReadValue();
                return PointerState.Held;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                position = touch.position.ReadValue();
                return PointerState.Up;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return PointerState.Down;
            }

            if (mouse.leftButton.isPressed)
            {
                position = mouse.position.ReadValue();
                return PointerState.Held;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                position = mouse.position.ReadValue();
                return PointerState.Up;
            }
        }
#else
        if (Input.touchCount > 0)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);
            position = touch.position;

            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                return PointerState.Down;
            }

            if (touch.phase == UnityEngine.TouchPhase.Moved || touch.phase == UnityEngine.TouchPhase.Stationary)
            {
                return PointerState.Held;
            }

            if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
            {
                return PointerState.Up;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return PointerState.Down;
        }
        else if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return PointerState.Held;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            position = Input.mousePosition;
            return PointerState.Up;
        }
#endif
        return PointerState.None;
    }
}
