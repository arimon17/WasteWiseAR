using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIFadePanel : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.2f;

    private CanvasGroup group;
    private Coroutine routine;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }

        if (group == null)
        {
            return;
        }

        // Mulai dari transparan agar setiap kemunculan punya fade-in yang halus.
        group.alpha = 0f;
        Fade(1f);
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        Fade(1f);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        Fade(0f, () => gameObject.SetActive(false));
    }

    private void Fade(float target, Action onDone = null)
    {
        if (group == null)
        {
            onDone?.Invoke();
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(FadeRoutine(target, onDone));
    }

    private IEnumerator FadeRoutine(float target, Action onDone)
    {
        float start = group.alpha;
        float elapsed = 0f;
        SetInteraction(target > 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        group.alpha = target;
        SetInteraction(target > 0.5f);
        routine = null;
        onDone?.Invoke();
    }

    private void SetInteraction(bool active)
    {
        group.blocksRaycasts = active;
        group.interactable = active;
    }
}
