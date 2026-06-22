// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/Scripts/07_Shared/Extensions/GameObjectExtensions.cs
// GameObject 扩展方法。所有层均可使用。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// GameObject 扩展方法集。
/// </summary>
public static class GameObjectExtensions
{
    /// <summary>
    /// 获取指定类型的组件；若不存在则新建一个并返回。
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <returns>已存在的组件或新添加的组件</returns>
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }
}
