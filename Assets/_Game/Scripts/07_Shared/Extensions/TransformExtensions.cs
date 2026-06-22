// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/07_Shared/Extensions/TransformExtensions.cs
// Transform 扩展方法。所有层均可使用。
// ══════════════════════════════════════════════════════════════════════
using UnityEngine;

/// <summary>
/// Transform 扩展方法集。
/// </summary>
public static class TransformExtensions
{
    /// <summary>销毁所有子物体</summary>
    public static void DestroyAllChildren(this Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    /// <summary>重置本地变换</summary>
    public static void ResetLocal(this Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    /// <summary>获取指定类型的组件；若不存在则新建一个并返回。</summary>
    public static T GetOrAddComponent<T>(this Transform t) where T : Component
    {
        return t.gameObject.GetOrAddComponent<T>();
    }

    // ══════════════════════════════════════════════════════
    // 子节点查找
    // ══════════════════════════════════════════════════════

    /// <summary>递归查找指定名称的子 Transform（深度优先）。未找到返回 null。</summary>
    public static Transform NodeByName(this Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;

            Transform found = child.NodeByName(name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>在指定名称的子节点上获取组件 T。未找到返回 null。</summary>
    public static T ComponentByName<T>(this Transform parent, string name) where T : Component
    {
        Transform node = parent.NodeByName(name);
        return node != null ? node.GetComponent<T>() : null;
    }

    /// <summary>在指定名称的子节点上获取所有组件 T。</summary>
    public static T[] ComponentsByName<T>(this Transform parent, string name) where T : Component
    {
        Transform node = parent.NodeByName(name);
        return node != null ? node.GetComponents<T>() : new T[0];
    }

    /// <summary>从指定路径的子节点获取组件 T。路径格式同 Transform.Find（如 "A/B/C"）。</summary>
    public static T GetComponent<T>(this Transform parent, string childPath) where T : Component
    {
        Transform node = parent.Find(childPath);
        return node != null ? node.GetComponent<T>() : null;
    }

    /// <summary>从指定路径的子节点获取所有组件 T。</summary>
    public static T[] GetComponents<T>(this Transform parent, string childPath) where T : Component
    {
        Transform node = parent.Find(childPath);
        return node != null ? node.GetComponents<T>() : new T[0];
    }

    /// <summary>设置 X 坐标（世界坐标）</summary>
    public static void SetPositionX(this Transform t, float x)
    {
        var pos = t.position;
        pos.x = x;
        t.position = pos;
    }

    /// <summary>设置 Y 坐标（世界坐标）</summary>
    public static void SetPositionY(this Transform t, float y)
    {
        var pos = t.position;
        pos.y = y;
        t.position = pos;
    }

    /// <summary>获取 2D 平面上到目标的距离</summary>
    public static float Distance2D(this Transform t, Transform other)
    {
        return Vector2.Distance(t.position, other.position);
    }

    /// <summary>获取 2D 平面上到目标的方向（归一化）</summary>
    public static Vector2 Direction2D(this Transform t, Transform target)
    {
        return ((Vector2)(target.position - t.position)).normalized;
    }

    /// <summary>面向目标（2D，通过翻转 localScale.x）</summary>
    public static void FaceTarget2D(this Transform t, Transform target)
    {
        if (target == null) return;
        float dir = target.position.x - t.position.x;
        if (Mathf.Abs(dir) < 0.01f) return;

        Vector3 scale = t.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir > 0 ? 1 : -1);
        t.localScale = scale;
    }
}
