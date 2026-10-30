using UnityEngine;

[DisallowMultipleComponent]
public class Billboard : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;

    private void LateUpdate()
    {
        Transform cam = ResolveCamera();

        if (cam == null)
        {
            return;
        }

        // Samakan rotasi dengan kamera agar teks World Space selalu terbaca (tidak pernah terbalik).
        transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }

    private Transform ResolveCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        Camera main = Camera.main;

        if (main == null)
        {
            main = FindAnyObjectByType<Camera>();
        }

        if (main != null)
        {
            targetCamera = main.transform;
        }

        return targetCamera;
    }
}
