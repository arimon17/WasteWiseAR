using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
using InputSystemTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

public class ARPlacementController : MonoBehaviour
{
    [Header("AR Placement")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private ARRaycastManager raycastManager;

    private readonly List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
    private GameObject spawnedObject;

    private void Awake()
    {
        if (raycastManager == null)
        {
            raycastManager = GetComponent<ARRaycastManager>();
        }

        if (raycastManager == null)
        {
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
        }
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Disable();
#endif
    }

    private void Update()
    {
        if (TryGetTouchBegan(out Vector2 screenPosition))
        {
            TryPlaceObject(screenPosition);
        }
    }

    private void TryPlaceObject(Vector2 screenPosition)
    {
        if (prefabToSpawn == null || raycastManager == null)
        {
            return;
        }

        if (!raycastManager.Raycast(screenPosition, hitResults, TrackableType.PlaneWithinPolygon) || hitResults.Count == 0)
        {
            return;
        }

        Pose hitPose = hitResults[0].pose;

        if (spawnedObject == null)
        {
            spawnedObject = Instantiate(prefabToSpawn, hitPose.position, hitPose.rotation);
        }
        else
        {
            spawnedObject.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
        }
    }

    private bool TryGetTouchBegan(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        foreach (InputSystemTouch touch in InputSystemTouch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                screenPosition = touch.screenPosition;
                return true;
            }
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
