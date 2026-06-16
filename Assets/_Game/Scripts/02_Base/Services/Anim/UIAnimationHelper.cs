// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/Services/Anim/UIAnimationHelper.cs
// 共享 UI 动画工具类。提供 CanvasGroup / RectTransform 的基础动画。
// WindowManager 和 UIWindow 通过此工具类实现可复用的打开/关闭动画。
// ─────────────────────────────────────────────────────────────────────
// 动画引擎：DOTween（替换原协程方案）。
// 所有动画使用 SetUpdate(true) 忽略 timeScale 影响。
// ══════════════════════════════════════════════════════════════════════
using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// UI 动画辅助工具（静态方法，基于 DOTween 驱动）。
///
/// 核心职责：
///   · 对 CanvasGroup 执行淡入淡出动画
///   · 对 RectTransform 执行滑入滑出动画
///   · 对 RectTransform 执行缩放弹入弹退动画
///
/// 设计说明：
///   · 所有方法返回 void，通过 Action onComplete 回调通知完成
///   · 动画使用 SetUpdate(true)（不受 Time.timeScale 影响）
///   · 缓动曲线使用 DOTween 内置 Ease 枚举
/// </summary>
public static class UIAnimationHelper
{
    // ══════════════════════════════════════════════════════
    // 淡入淡出
    // ══════════════════════════════════════════════════════

    /// <summary>淡入动画（CanvasGroup alpha 0→1）</summary>
    public static void FadeIn(CanvasGroup canvasGroup, float duration,
        Action onComplete = null)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);

        canvasGroup.DOFade(1f, duration)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>淡出动画（CanvasGroup alpha 1→0）</summary>
    public static void FadeOut(CanvasGroup canvasGroup, float duration,
        Action onComplete = null)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = 1f;

        canvasGroup.DOFade(0f, duration)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // ══════════════════════════════════════════════════════
    // 滑入滑出
    // ══════════════════════════════════════════════════════

    /// <summary>从指定方向滑入（RectTransform anchoredPosition）</summary>
    public static void SlideIn(RectTransform rectTransform, Vector2 from,
        Vector2 to, float duration, Action onComplete = null)
    {
        if (rectTransform == null) return;
        rectTransform.anchoredPosition = from;
        rectTransform.gameObject.SetActive(true);

        rectTransform.DOAnchorPos(to, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>向指定方向滑出（RectTransform anchoredPosition）</summary>
    public static void SlideOut(RectTransform rectTransform, Vector2 from,
        Vector2 to, float duration, Action onComplete = null)
    {
        if (rectTransform == null) return;
        rectTransform.anchoredPosition = from;

        rectTransform.DOAnchorPos(to, duration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // ══════════════════════════════════════════════════════
    // 缩放弹入弹退
    // ══════════════════════════════════════════════════════

    /// <summary>缩放弹入动画（localScale 0→1，带超调弹性）</summary>
    public static void ScaleIn(RectTransform rectTransform, float duration,
        Action onComplete = null)
    {
        if (rectTransform == null) return;
        rectTransform.localScale = Vector3.zero;
        rectTransform.gameObject.SetActive(true);

        rectTransform.DOScale(1f, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>缩放弹退动画（localScale 1→0，带回缩弹性）</summary>
    public static void ScaleOut(RectTransform rectTransform, float duration,
        Action onComplete = null)
    {
        if (rectTransform == null) return;
        rectTransform.localScale = Vector3.one;

        rectTransform.DOScale(0f, duration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    // ══════════════════════════════════════════════════════
    // 便捷方法：根据 WindowAnimationType 执行动画
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 根据动画类型执行对应的打开动画。
    /// </summary>
    public static void PlayOpenAnimation(CanvasGroup canvasGroup,
        RectTransform rectTransform, WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        switch (type)
        {
            case WindowAnimationType.None:
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
                onComplete?.Invoke();
                break;

            case WindowAnimationType.FadeIn:
                FadeIn(canvasGroup, duration, onComplete);
                break;

            case WindowAnimationType.FadeOut:
                // 打开时不用 FadeOut，回退到 FadeIn
                FadeIn(canvasGroup, duration, onComplete);
                break;

            case WindowAnimationType.SlideFromRight:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                SlideIn(rectTransform, new Vector2(width, 0f), Vector2.zero,
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromLeft:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                SlideIn(rectTransform, new Vector2(-width, 0f), Vector2.zero,
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromTop:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                SlideIn(rectTransform, new Vector2(0f, height), Vector2.zero,
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromBottom:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                SlideIn(rectTransform, new Vector2(0f, -height), Vector2.zero,
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.ScaleIn:
                ScaleIn(rectTransform, duration, onComplete);
                break;

            case WindowAnimationType.ScaleOut:
                // 打开时不用 ScaleOut，回退到 ScaleIn
                ScaleIn(rectTransform, duration, onComplete);
                break;

            default:
                canvasGroup.alpha = 1f;
                canvasGroup.gameObject.SetActive(true);
                onComplete?.Invoke();
                break;
        }
    }

    /// <summary>
    /// 根据动画类型执行对应的关闭动画。
    /// </summary>
    public static void PlayCloseAnimation(CanvasGroup canvasGroup,
        RectTransform rectTransform, WindowAnimationType type, float duration,
        Action onComplete = null)
    {
        switch (type)
        {
            case WindowAnimationType.None:
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
                break;

            case WindowAnimationType.FadeIn:
                // 关闭时不用 FadeIn，回退到 FadeOut
                FadeOut(canvasGroup, duration, onComplete);
                break;

            case WindowAnimationType.FadeOut:
                FadeOut(canvasGroup, duration, onComplete);
                break;

            case WindowAnimationType.SlideFromRight:
                // 关闭时滑出到右边
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                SlideOut(rectTransform, Vector2.zero, new Vector2(width, 0f),
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromLeft:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
                SlideOut(rectTransform, Vector2.zero, new Vector2(-width, 0f),
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromTop:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                SlideOut(rectTransform, Vector2.zero, new Vector2(0f, height),
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.SlideFromBottom:
            {
                var canvasRect = rectTransform?.parent?.GetComponent<RectTransform>();
                float height = canvasRect != null ? canvasRect.rect.height : Screen.height;
                SlideOut(rectTransform, Vector2.zero, new Vector2(0f, -height),
                    duration, onComplete);
                break;
            }
            case WindowAnimationType.ScaleIn:
                // 关闭时不用 ScaleIn，回退到 ScaleOut
                ScaleOut(rectTransform, duration, onComplete);
                break;

            case WindowAnimationType.ScaleOut:
                ScaleOut(rectTransform, duration, onComplete);
                break;

            default:
                canvasGroup.alpha = 0f;
                canvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
                break;
        }
    }
}
