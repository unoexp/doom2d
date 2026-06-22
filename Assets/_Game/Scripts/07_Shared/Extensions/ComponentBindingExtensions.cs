// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/Scripts/07_Shared/Extensions/ComponentBindingExtensions.cs
// BindComponents() 扩展方法，通过反射自动绑定 [Bind] 标记的字段。
// ══════════════════════════════════════════════════════════════════════
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 组件自动绑定扩展。
/// </summary>
public static class ComponentBindingExtensions
{
    // ══════════════════════════════════════════════════════
    // 绑定标记
    // ══════════════════════════════════════════════════════

    private static readonly Type BindAttrType = typeof(BindAttribute);

    // ══════════════════════════════════════════════════════
    // 公有 API
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 扫描当前 MonoBehaviour 所有带 [Bind] 的字段，自动从子节点获取对应组件。
    /// 在 Awake 中调用一次即可。
    /// </summary>
    public static void BindComponents(this MonoBehaviour target)
    {
        Type type = target.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            BindAttribute attr = field.GetCustomAttribute(BindAttrType) as BindAttribute;
            if (attr == null) continue;

            string childName = attr.Path ?? FieldNameToChildName(field.Name);
            object value = ResolveBinding(target.transform, childName, field.FieldType);

            if (value != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning(
                    $"[Bind] 绑定失败: {type.Name}.{field.Name} → \"{childName}\" ({field.FieldType.Name}) 未找到",
                    target);
            }
        }
    }

    /// <summary>
    /// 扫描任意对象所有带 [Bind] 的字段，从指定 Transform 的子节点获取对应组件。
    /// 适用于非 MonoBehaviour 的类（如 UIWindow）。
    /// </summary>
    public static void BindComponentsOn(this Transform root, object target)
    {
        Type type = target.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            BindAttribute attr = field.GetCustomAttribute(BindAttrType) as BindAttribute;
            if (attr == null) continue;

            string childName = attr.Path ?? FieldNameToChildName(field.Name);
            object value = ResolveBinding(root, childName, field.FieldType);

            if (value != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning(
                    $"[Bind] 绑定失败: {type.Name}.{field.Name} → \"{childName}\" ({field.FieldType.Name}) 未找到",
                    root);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>将 _camelCase 字段名转换为 PascalCase 子节点名</summary>
    private static string FieldNameToChildName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return fieldName;

        // 去掉前导下划线：_icon → Icon
        string name = fieldName.StartsWith("_") ? fieldName.Substring(1) : fieldName;
        if (name.Length == 0) return name;

        return char.ToUpper(name[0]) + name.Substring(1);
    }

    /// <summary>根据类型选择不同的解析策略</summary>
    private static object ResolveBinding(Transform parent, string childName, Type fieldType)
    {
        // 包含路径分隔符时使用 Transform.Find（支持 "A/B/C" 路径语法）
        if (childName.Contains('/'))
        {
            Transform node = parent.Find(childName);
            if (node == null) return null;
            if (fieldType == typeof(GameObject)) return node.gameObject;
            if (fieldType == typeof(Transform)) return node;
            if (typeof(Component).IsAssignableFrom(fieldType)) return node.GetComponent(fieldType);
            return null;
        }

        // GameObject
        if (fieldType == typeof(GameObject))
        {
            Transform node = parent.NodeByName(childName);
            return node != null ? node.gameObject : null;
        }

        // Transform
        if (fieldType == typeof(Transform))
        {
            return parent.NodeByName(childName);
        }

        // Component 子类
        if (typeof(Component).IsAssignableFrom(fieldType))
        {
            return parent.ComponentByName(childName, fieldType);
        }

        // 不支持的类型
        return null;
    }

    /// <summary>按类型获取组件（非泛型版）</summary>
    private static Component ComponentByName(this Transform parent, string name, Type componentType)
    {
        Transform node = parent.NodeByName(name);
        return node != null ? node.GetComponent(componentType) : null;
    }
}
