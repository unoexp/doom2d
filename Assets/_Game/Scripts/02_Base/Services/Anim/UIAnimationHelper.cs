// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/Services/Anim/UIAnimationHelper.cs
// 共享 UI 动画协程工具类。提供 CanvasGroup / RectTransform 的基础动画。
// WindowManager 和 UIWindow 通过此工具类实现可复用的打开/关闭动画。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// UI 动画辅助工具（静态方法，纯协程驱动，零 GC 分配）。
///
/// 核心职责：
///   · 对 CanvasGroup 执行淡入淡出动画
///   · 对 RectTransform 执行滑入滑出动画
///   · 对 RectTransform 执行缩放弹入弹退动画
///
/// 设计说明：
///   · 所有方法返回 IEnumerator，供 StartCoroutine 使用
///   · 动画使用 Time.unscaledDeltaTime（不受 Time.timeScale 影响）
///   · 弹性曲线使用 easeOutBack / easeInBack 公式
/// </summary>
public static class UIAnimationHelper
{
    /// <summary>弹入动画的超调量（scale 越过 1 的幅度）</summary>
    private const float EASE_OVERSHOOT = 0.4f;

    // ══════════════════════════════════════════════════════
    // 淡入淡出
    // ══════════════════════════════════════════════════════

    /// <summary>淡入动画（CanvasGroup alpha 0→1）</summary>
    public static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration,
        Action onComplete = null)
    {
        if (canvasGroup == null) yield break;
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        onComplete?.Invoke();
    }

    /// <summary>淡出动画（CanvasGroup alpha 1→0）</summary>
    public static IEnumerator FadeOut(CanvasGroup canvasGroup, float duration,
        Action onComplete = null)
    {
        if (canvasGroup == null) yield break;
        canvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        onComplete?.Invoke();
    }

    // ══════════════════════════════════════════════════════
    // 滑入滑出
    // ══════════════════════════════════════════════════════

    /// <summary>从指定方向滑入（RectTransform anchoredPosition）</summary>
    public static IEnumerator SlideIn(RectTransform rectTransform, Vector2 from,
        Vector2 to, float duration, Action onComplete = null)
    {
        if (rectTransform == null) yield break;
        rectTransform.anchoredPosition = from;
        rectTransform.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }
        rectTransform.anchoredPosition = to;
        onComplete?.Invoke();
    }

    /// <summary>向指定方向滑出（RectTransform anchoredPosition）</summary>
    public static IEnumerator SlideOut(RectTransform rectTransform, Vector2 from,
        Vector2 to, float duration, Action onComplete = null)
    {
        if (rectTransform == null) yield break;
        rectTransform.anchoredPosition = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInCubic(Mathf.Clamp01(elapsed / duration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }
        rectTransform.anchoredPosition = to;
        onComplete?.Invoke();
    }

    // ══════════════════════════════════════════════════════
    // 缩放弹入弹退
    // ══════════════════════════════════════════════════════

    /// <summary>缩放弹入动画（localScale 0→1，带超调弹性）</summary>
    public static IEnumerator ScaleIn(RectTransform rectTransform, float duration,
        Action onComplete = null)
    {
        if (rectTransform == null) yield break;
        rectTransform.localScale = Vector3.zero;
        rectTransform.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));
            rectTransform.localScale = Vector3.one * t;
            yield return null;
        }
        rectTransform.localScale = Vector3.one;
        onComplete?.Invoke();
    }

    /// <summary>缩放弹退动画（localScale 1→0，带回缩弹性）</summary>
    public static IEnumerator ScaleOut(RectTransform rectTransform, float duration,
        Action onComplete = null)
    {
        if (rectTransform == null) yield break;
        rectTransform.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInBack(Mathf.Clamp01(elapsed / duration));
            rectTransform.localScale = Vector3.one * (1f - t);
            yield return null;
        }
        rectTransform.localScale = Vector3.zero;
        onComplete?.Invoke();
    }

    // ══════════════════════════════════════════════════════
    // 缓动函数（全部 static，无闭包分配）
    // ══════════════════════════════════════════════════════

    private static float EaseOutCubic(float t)
    {
        float t1 = 1f - t;
        return 1f - t1 * t1 * t1;
    }

    private static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    private static float EaseOutBack(float t)
    {
        float t1 = 1f - t;
        return 1f + EASE_OVERSHOOT * (1f - (t1 * t1 * t1 * t1));
    }

    private static float EaseInBack(float t)
    {
        return EASE_OVERSHOOT * t * t * t * t;
    }

    // ══════════════════════════════════════════════════════
    // 便捷方法：根据 WindowAnimationType 执行动画
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 根据动画类型执行对应的打开动画。
    /// 返回 IEnumerator 供 StartCoroutine 使用。
    /// </summary>
    public static IEnumerator PlayOpenAnimation(CanvasGroup canvasGroup,
        RectTransform rectTransform, WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        switch (type)
        {
            case WindowAnimationType.None:
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
                onComplete?.Invoke();
                yield break;

            case WindowAnimationType.FadeIn:
                yield return FadeIn(canvasGroup, duration, onComplete);
                yield break;

            case WindowAnimationType.FadeOut:
                // 打开时不用 FadeOut，回退到 FadeIn
                yield return FadeIn(canvasGroup, duration, onComplete);
                yield break;

            case WindowAnimationType.SlideFromRight:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                yield return SlideIn(rectTransform, new Vector2(width, 0f), Vector2.zero,
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromLeft:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                yield return SlideIn(rectTransform, new Vector2(-width, 0f), Vector2.zero,
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromTop:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                yield return SlideIn(rectTransform, new Vector2(0f, height), Vector2.zero,
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromBottom:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                yield return SlideIn(rectTransform, new Vector2(0f, -height), Vector2.zero,
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.ScaleIn:
                yield return ScaleIn(rectTransform, duration, onComplete);
                yield break;

            case WindowAnimationType.ScaleOut:
                // 打开时不用 ScaleOut，回退到 ScaleIn
                yield return ScaleIn(rectTransform, duration, onComplete);
                yield break;

            default:
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
                onComplete?.Invoke();
                yield break;
        }
    }

    /// <summary>
    /// 根据动画类型执行对应的关闭动画。
    /// 返回 IEnumerator 供 StartCoroutine 使用。
    /// </summary>
    public static IEnumerator PlayCloseAnimation(CanvasGroup canvasGroup,
        RectTransform rectTransform, WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        switch (type)
        {
            case WindowAnimationType.None:
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
                yield break;

            case WindowAnimationType.FadeIn:
                // 关闭时不用 FadeIn，回退到 FadeOut
                yield return FadeOut(canvasGroup, duration, onComplete);
                yield break;

            case WindowAnimationType.FadeOut:
                yield return FadeOut(canvasGroup, duration, onComplete);
                yield break;

            case WindowAnimationType.SlideFromRight:
                // 关闭时滑出到右边
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                yield return SlideOut(rectTransform, Vector2.zero, new Vector2(width, 0f),
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromLeft:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                yield return SlideOut(rectTransform, Vector2.zero, new Vector2(-width, 0f),
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromTop:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                yield return SlideOut(rectTransform, Vector2.zero, new Vector2(0f, height),
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.SlideFromBottom:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                yield return SlideOut(rectTransform, Vector2.zero, new Vector2(0f, -height),
                    duration, onComplete);
                yield break;
            }
            case WindowAnimationType.ScaleIn:
                // 关闭时不用 ScaleIn，回退到 ScaleOut
                yield return ScaleOut(rectTransform, duration, onComplete);
                yield break;

            case WindowAnimationType.ScaleOut:
                yield return ScaleOut(rectTransform, duration, onComplete);
                yield break;

            default:
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
                yield break;
        }
    }
}
