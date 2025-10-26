using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Drone))]
public class DroneFrameInterpolator : MonoBehaviour
{
    [Tooltip("Movement speed (units per second) for inter-frame transitions (frame >= 1).")]
    public float unitsPerSecond = 2f;

    [Tooltip("Easing curve for inter-frame transitions.")]
    public AnimationCurve ease = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    Coroutine moveRoutine;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public bool IsMoving => moveRoutine != null;

    public void Cancel()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    // Move by a delta vector smoothly; calls onComplete when finished.
    public void MoveByOffset(Vector3 offset, float? speedOverride, Action onComplete)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(offset, speedOverride ?? unitsPerSecond, onComplete));
    }

    IEnumerator MoveRoutine(Vector3 offset, float speed, Action onComplete)
    {
        Vector3 start = transform.position;
        Vector3 target = start + offset;

        float distance = offset.magnitude;
        float duration = distance <= 1e-4f ? 0f : distance / Mathf.Max(0.01f, speed);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
        }

        if (duration <= 0f)
        {
            transform.position = target;
            onComplete?.Invoke();
            moveRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = Mathf.Clamp01(ease.Evaluate(u));
            transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }

        transform.position = target;
        onComplete?.Invoke();
        moveRoutine = null;
    }
}
