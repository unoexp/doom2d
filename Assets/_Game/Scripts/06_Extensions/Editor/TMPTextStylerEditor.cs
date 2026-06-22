// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/Scripts/06_Extensions/Editor/TMPTextStylerEditor.cs
// TMPTextStyler 组件的自定义 Inspector，提供描边和阴影参数的直观调整面板。
// ══════════════════════════════════════════════════════════════════════
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TMPTextStyler 自定义 Inspector。
/// 将描边和阴影参数分组显示，并在参数变化时立即应用到 TMP 文本。
/// </summary>
[CustomEditor(typeof(TMPTextStyler))]
public class TMPTextStylerEditor : Editor
{
    // ══════════════════════════════════════════════════════
    // 序列化属性
    // ══════════════════════════════════════════════════════

    private SerializedProperty _enableOutline;
    private SerializedProperty _outlineColor;
    private SerializedProperty _outlineWidth;

    private SerializedProperty _enableShadow;
    private SerializedProperty _shadowColor;
    private SerializedProperty _shadowOffsetX;
    private SerializedProperty _shadowOffsetY;
    private SerializedProperty _shadowDilate;
    private SerializedProperty _shadowSoftness;

    // ══════════════════════════════════════════════════════
    // 布局常量
    // ══════════════════════════════════════════════════════

    private static readonly Color _headerColor = new Color(0.35f, 0.55f, 0.75f, 1f);

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    private void OnEnable()
    {
        _enableOutline  = serializedObject.FindProperty("_enableOutline");
        _outlineColor   = serializedObject.FindProperty("_outlineColor");
        _outlineWidth   = serializedObject.FindProperty("_outlineWidth");

        _enableShadow   = serializedObject.FindProperty("_enableShadow");
        _shadowColor    = serializedObject.FindProperty("_shadowColor");
        _shadowOffsetX  = serializedObject.FindProperty("_shadowOffsetX");
        _shadowOffsetY  = serializedObject.FindProperty("_shadowOffsetY");
        _shadowDilate   = serializedObject.FindProperty("_shadowDilate");
        _shadowSoftness = serializedObject.FindProperty("_shadowSoftness");
    }

    // ══════════════════════════════════════════════════════
    // Inspector 绘制
    // ══════════════════════════════════════════════════════

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TMPTextStyler styler = (TMPTextStyler)target;

        // ── TMP_Text 信息 ──
        DrawTmpTextInfo(styler);

        EditorGUILayout.Space(6);

        // ── 描边 ──
        DrawOutlineSection();

        EditorGUILayout.Space(4);

        // ── 阴影 ──
        DrawShadowSection();

        EditorGUILayout.Space(6);

        // ── 快捷操作 ──
        DrawQuickActions(styler);

        serializedObject.ApplyModifiedProperties();
    }

    // ══════════════════════════════════════════════════════
    // 子区域绘制
    // ══════════════════════════════════════════════════════

    private void DrawTmpTextInfo(TMPTextStyler styler)
    {
        TMP_Text tmp = styler.TmpText;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("TMP 文本信息", EditorStyles.boldLabel);

        if (tmp != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("组件类型", tmp.GetType().Name);
            EditorGUILayout.LabelField("文本内容",
                tmp.text.Length > 60 ? tmp.text.Substring(0, 57) + "..." : tmp.text);
            EditorGUILayout.LabelField("字体资源",
                tmp.font != null ? tmp.font.name : "(无)");
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox(
                "GameObject 上未找到 TMP_Text 组件。请在此 GameObject 上添加 TextMeshPro 或 TextMeshProUGUI。",
                MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawOutlineSection()
    {
        // 标题栏
        Rect headerRect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(headerRect, new Color(0.25f, 0.25f, 0.25f, 1f));
        EditorGUI.LabelField(headerRect, "  描边 (Outline)", EditorStyles.whiteLargeLabel);

        EditorGUILayout.Space(2);

        // 启用开关
        EditorGUILayout.PropertyField(_enableOutline, new GUIContent("启用描边"));

        if (_enableOutline.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_outlineColor, new GUIContent("描边颜色"));
            EditorGUILayout.PropertyField(_outlineWidth, new GUIContent("描边宽度"));

            // 预览条
            Rect previewRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(previewRect, _outlineColor.colorValue);

            EditorGUI.indentLevel--;
        }
    }

    private void DrawShadowSection()
    {
        // 标题栏
        Rect headerRect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(headerRect, new Color(0.25f, 0.25f, 0.25f, 1f));
        EditorGUI.LabelField(headerRect, "  阴影 (Shadow)", EditorStyles.whiteLargeLabel);

        EditorGUILayout.Space(2);

        // 启用开关
        EditorGUILayout.PropertyField(_enableShadow, new GUIContent("启用阴影"));

        if (_enableShadow.boolValue)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_shadowColor, new GUIContent("阴影颜色"));

            EditorGUILayout.LabelField("阴影偏移", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_shadowOffsetX, new GUIContent("水平偏移 X"));
            EditorGUILayout.PropertyField(_shadowOffsetY, new GUIContent("垂直偏移 Y"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(_shadowDilate, new GUIContent("扩散大小"));
            EditorGUILayout.PropertyField(_shadowSoftness, new GUIContent("柔和度"));

            // 预览条
            Rect previewRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(previewRect, _shadowColor.colorValue);

            EditorGUI.indentLevel--;
        }
    }

    private void DrawQuickActions(TMPTextStyler styler)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // 一键应用按钮
        GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
        if (GUILayout.Button("立即应用", GUILayout.Height(24)))
        {
            styler.ApplyStyles();
            EditorUtility.SetDirty(styler);
        }

        GUI.backgroundColor = Color.white;

        // 重置默认值
        if (GUILayout.Button("重置默认值", GUILayout.Height(24)))
        {
            Undo.RecordObject(styler, "Reset TMPTextStyler");

            _enableOutline.boolValue = false;
            _outlineColor.colorValue = Color.black;
            _outlineWidth.floatValue = 0.2f;

            _enableShadow.boolValue = false;
            _shadowColor.colorValue = new Color(0f, 0f, 0f, 0.5f);
            _shadowOffsetX.floatValue = -0.1f;
            _shadowOffsetY.floatValue = -0.1f;
            _shadowDilate.floatValue = 0f;
            _shadowSoftness.floatValue = 0f;

            serializedObject.ApplyModifiedProperties();
            styler.ApplyStyles();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}
