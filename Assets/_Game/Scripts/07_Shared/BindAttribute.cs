// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/Scripts/07_Shared/BindAttribute.cs
// [Bind] 属性，标记需要自动绑定的子节点组件字段。
// 配合 BindComponents() 使用，在 UIPanel.Awake 中自动完成绑定。
// ══════════════════════════════════════════════════════════════════════
using System;

/// <summary>
/// 标记字段为自动绑定目标。
///
/// 无参：[Bind] — 使用字段名（去掉前导下划线）作为子节点名，如 _icon → "Icon"
/// 有参：[Bind("Header/Title")] — 使用显式路径查找子节点
///
/// 支持类型：任意 Component 子类、Transform、GameObject。
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class BindAttribute : Attribute
{
    /// <summary>绑定的子节点路径。null 表示使用字段名推断。</summary>
    public string Path { get; }

    /// <summary>使用字段名推断路径</summary>
    public BindAttribute()
    {
        Path = null;
    }

    /// <summary>使用显式路径</summary>
    /// <param name="path">子节点名或 "A/B/C" 格式的相对路径</param>
    public BindAttribute(string path)
    {
        Path = path;
    }
}
