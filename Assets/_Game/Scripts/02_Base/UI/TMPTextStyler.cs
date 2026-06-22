// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/Scripts/02_Base/UI/TMPTextStyler.cs
// 在 Inspector 中调整 TextMeshPro 文本的描边（Outline）和阴影（Shadow/Underlay）参数。
// 自动获取同 GameObject 上的 TMP_Text，创建材质实例避免影响共享材质。
// ══════════════════════════════════════════════════════════════════════
using TMPro;
using UnityEngine;

/// <summary>
/// TMP 文字样式组件 — 无需手动编辑材质，在 Inspector 中直接调整描边和阴影。
///
/// 核心职责：
///   · 自动获取同 GameObject 的 TMP_Text 组件
///   · 创建材质实例，避免污染共享材质
///   · 在 OnValidate / Start 中应用样式
///
/// 使用方式：
///   将此组件挂载到任意带有 TMP_Text（TextMeshPro 或 TextMeshProUGUI）的 GameObject 上即可。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TMPTextStyler : MonoBehaviour
{
    // ══════════════════════════════════════════════════════
    // 描边（Outline）配置
    // ══════════════════════════════════════════════════════

    [Header("描边 (Outline)")]
    [Tooltip("是否启用描边")]
    [SerializeField] private bool _enableOutline = false;

    [Tooltip("描边颜色")]
    [SerializeField] private Color _outlineColor = Color.black;

    [Tooltip("描边宽度（0~1，值越大描边越粗）")]
    [Range(0f, 1f)]
    [SerializeField] private float _outlineWidth = 0.2f;

    // ══════════════════════════════════════════════════════
    // 阴影（Shadow / Underlay）配置
    // ══════════════════════════════════════════════════════

    [Header("阴影 (Shadow)")]
    [Tooltip("是否启用阴影")]
    [SerializeField] private bool _enableShadow = false;

    [Tooltip("阴影颜色")]
    [SerializeField] private Color _shadowColor = new Color(0f, 0f, 0f, 0.5f);

    [Tooltip("阴影偏移 X（负值左偏，正值右偏）")]
    [Range(-1f, 1f)]
    [SerializeField] private float _shadowOffsetX = -0.1f;

    [Tooltip("阴影偏移 Y（负值下偏，正值上偏）")]
    [Range(-1f, 1f)]
    [SerializeField] private float _shadowOffsetY = -0.1f;

    [Tooltip("阴影扩散大小（负值缩小，正值扩大）")]
    [Range(-1f, 1f)]
    [SerializeField] private float _shadowDilate = 0f;

    [Tooltip("阴影柔和度（0 锐利，1 柔和）")]
    [Range(0f, 1f)]
    [SerializeField] private float _shadowSoftness = 0f;

    // ══════════════════════════════════════════════════════
    // 运行时状态
    // ══════════════════════════════════════════════════════

    private TMP_Text _tmpText;
    private Material _instanceMaterial;
    private Material _lastSharedMaterial;

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>当前绑定的 TMP_Text</summary>
    public TMP_Text TmpText => _tmpText;

    /// <summary>材质实例（仅在启用描边或阴影时创建）</summary>
    public Material InstanceMaterial => _instanceMaterial;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void Awake()
    {
        CacheTmpText();
    }

    private void Start()
    {
        CacheTmpText();
        ApplyStyles();
    }

    private void OnEnable()
    {
        CacheTmpText();
        ApplyStyles();
    }

    private void OnDisable()
    {
        // 保留材质实例，避免重建开销；禁用时不做清理。
    }

    private void OnDestroy()
    {
        CleanupInstanceMaterial();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // OnValidate 在编辑器中修改属性时立即生效。
        // 使用 delayCall 确保此时组件已完全初始化。
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            CacheTmpText();
            ApplyStyles();
        };
    }
#endif

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>立即应用当前样式到材质</summary>
    public void ApplyStyles()
    {
        if (_tmpText == null) return;

        EnsureInstanceMaterial();

        if (_instanceMaterial == null) return;

        // ── 描边 ──
        if (_enableOutline)
        {
            _instanceMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            _instanceMaterial.SetColor(ShaderUtilities.ID_OutlineColor, _outlineColor);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, _outlineWidth);
        }
        else
        {
            _instanceMaterial.DisableKeyword(ShaderUtilities.Keyword_Outline);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        }

        // ── 阴影 ──
        if (_enableShadow)
        {
            _instanceMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            _instanceMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, _shadowColor);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, _shadowOffsetX);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, _shadowOffsetY);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, _shadowDilate);
            _instanceMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, _shadowSoftness);
        }
        else
        {
            _instanceMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            // 颜色 alpha 置零以彻底隐藏阴影
            _instanceMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0f));
        }

        // 更新 padding 以确保描边/阴影不裁剪文字
        _tmpText.havePropertiesChanged = true;
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    private void CacheTmpText()
    {
        if (_tmpText == null)
            _tmpText = GetComponent<TMP_Text>();
    }

    /// <summary>确保存在材质实例（与共享材质分离），避免修改其他 TMP 文本</summary>
    private void EnsureInstanceMaterial()
    {
        if (_tmpText == null) return;

        Material sharedMat = _tmpText.fontSharedMaterial;
        if (sharedMat == null) return;

        // 已有实例且共享材质未变，直接复用
        if (_instanceMaterial != null && sharedMat == _lastSharedMaterial)
            return;

        // 基于当前共享材质创建新实例
        CleanupInstanceMaterial();

        _instanceMaterial = new Material(sharedMat);
        _instanceMaterial.name = sharedMat.name + " (Styler Instance)";
        _lastSharedMaterial = sharedMat;

        _tmpText.fontSharedMaterial = _instanceMaterial;
    }

    private void CleanupInstanceMaterial()
    {
        if (_instanceMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(_instanceMaterial);
            else
                DestroyImmediate(_instanceMaterial);

            _instanceMaterial = null;
            _lastSharedMaterial = null;
        }
    }

    // ══════════════════════════════════════════════════════
    // 重置为默认值
    // ══════════════════════════════════════════════════════

    private void Reset()
    {
        _enableOutline = false;
        _outlineColor = Color.black;
        _outlineWidth = 0.2f;
        _enableShadow = false;
        _shadowColor = new Color(0f, 0f, 0f, 0.5f);
        _shadowOffsetX = -0.1f;
        _shadowOffsetY = -0.1f;
        _shadowDilate = 0f;
        _shadowSoftness = 0f;
    }
}
