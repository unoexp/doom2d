// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/Interfaces/ISceneBGMProvider.cs
// 场景BGM提供者接口。由各场景中挂载的 MonoBehaviour 实现。
// SceneLoadSystem 在加载场景后扫描此接口以触发 BGM 切换。
// ══════════════════════════════════════════════════════════════════════

/// <summary>
/// 场景BGM提供者接口。
///
/// 使用方式：
///   · 在场景中的任意 GameObject 上挂载实现此接口的 MonoBehaviour
///   · SceneLoadSystem 加载场景后扫描所有根 GameObject，找到实现后调用 AudioManager.Play(audioId)
///   · 若场景中无实现此接口的对象，则保留当前 BGM 不变
///   · 若 BGMAudioId 为 null 或空字符串，则停止当前 BGM
///
/// 示例：
///   public class MainMenuBGM : MonoBehaviour, ISceneBGMProvider
///   {
///       public string BGMAudioId => "bgm_main_menu";
///       public float BGMFadeDuration => 1.5f;
///   }
/// </summary>
public interface ISceneBGMProvider
{
    /// <summary>
    /// BGM 音频 ID（对应 AudioCatalog 中 Group=Music 的条目）。
    /// 返回 null 或空字符串表示此场景无专属 BGM（停止当前 BGM）。
    /// </summary>
    string BGMAudioId { get; }

    /// <summary>
    /// BGM 淡入淡出时长（秒）。
    /// 当前由 AudioManager 统一控制，此字段预留供后续精细控制。
    /// </summary>
    float BGMFadeDuration { get; }
}
