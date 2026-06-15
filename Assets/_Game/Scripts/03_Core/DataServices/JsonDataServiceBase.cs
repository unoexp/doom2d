// 📁 03_Core/DataServices/JsonDataServiceBase.cs
// JSON 数据服务非泛型基类。Unity 无法直接 AddComponent 泛型 MonoBehaviour，
// 因此抽出此非泛型抽象基类，提供 LoadAsync 协程签名供 DataLoaderSystem 调用。

using System.Collections;
using UnityEngine;

/// <summary>
/// JSON 数据服务抽象基类（非泛型）。
/// 所有 JsonDataService&lt;T&gt; 继承此类，使 DataLoaderSystem 可以
/// 通过统一类型引用并调用 LoadAsync。
/// </summary>
public abstract class JsonDataServiceBase : MonoBehaviour, ISystem
{
    /// <summary>对应的 JSON 文件名（不含路径，如 "items.json"）</summary>
    public abstract string DataFileName { get; }

    /// <summary>数据是否已加载完成</summary>
    public virtual bool IsLoaded { get; protected set; }

    /// <summary>异步加载 JSON 数据并解析</summary>
    public abstract IEnumerator LoadAsync();

    /// <summary>配置注入后的初始化（ISystem）</summary>
    public virtual void Initialize() { }

    /// <summary>系统关闭清理（ISystem）</summary>
    public virtual void Shutdown() { }
}
