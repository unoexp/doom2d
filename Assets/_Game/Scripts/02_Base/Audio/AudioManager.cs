// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/02_Base/Audio/AudioManager.cs
// 音频管理器。统一管理 BGM、音效、环境音的播放和音量控制。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;
// [MIGRATED] AudioEntry 类型已从 SO 迁移至 POCO AudioEntryData
using AudioEntry = AudioEntryData;

/// <summary>
/// 中央音频管理系统。
///
/// 核心特性：
///   · 分组音量控制（Master/Music/SFX/Ambient/UI/Voice）
///   · BGM 淡入淡出切换
///   · SFX 对象池化播放（避免 AudioSource 泛滥）
///   · 通过 MonoSingleton + ServiceLocator 双重访问
/// </summary>
public sealed class AudioManager : MonoSingleton<AudioManager>
{
    // ══════════════════════════════════════════════════════
    // 配置
    // ══════════════════════════════════════════════════════

    // [MIGRATED] 从 AudioCatalogData (JSON) 加载音频目录
    public AudioCatalogData[] Catalogs { get; set; }

    public AudioSource MusicSource { get; set; }
    public AudioSource AmbientSource { get; set; }

    public int SfxPoolSize { get; set; } = 8;

    public float DefaultMasterVolume { get; set; } = 1f;
    public float DefaultMusicVolume { get; set; } = 0.7f;
    public float DefaultSfxVolume { get; set; } = 1f;
    public float DefaultAmbientVolume { get; set; } = 0.5f;

    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>各组音量</summary>
    private readonly Dictionary<AudioGroup, float> _volumes = new Dictionary<AudioGroup, float>();

    /// <summary>SFX AudioSource 池</summary>
    private readonly List<AudioSource> _sfxPool = new List<AudioSource>();

    /// <summary>音效ID → 音效条目（快速查找）</summary>
    private readonly Dictionary<string, AudioEntry> _entryMap = new Dictionary<string, AudioEntry>();

    /// <summary>BGM 淡入淡出状态</summary>
    private AudioClip _pendingMusic;
    private float _fadeTimer;
    private float _fadeDuration;
    private bool _isFading;
    private bool _isFadingOut;

    // ══════════════════════════════════════════════════════
    // 初始化
    // ══════════════════════════════════════════════════════

    protected override void OnInitialize()
    {
        // [AppMain 重构] 仅注册 ServiceLocator（无配置依赖）
        ServiceLocator.Register<AudioManager>(this);
    }

    /// <summary>配置注入后的完整初始化（ISystem）</summary>
    public override void Initialize()
    {
        InitSingleton();

        // 初始化音量
        _volumes[AudioGroup.Master] = DefaultMasterVolume;
        _volumes[AudioGroup.Music] = DefaultMusicVolume;
        _volumes[AudioGroup.SFX] = DefaultSfxVolume;
        _volumes[AudioGroup.Ambient] = DefaultAmbientVolume;
        _volumes[AudioGroup.UI] = 1f;
        _volumes[AudioGroup.Voice] = 1f;

        // 自动创建音频源
        if (MusicSource == null)
        {
            MusicSource = gameObject.AddComponent<AudioSource>();
            MusicSource.loop = true;
            MusicSource.playOnAwake = false;
        }

        if (AmbientSource == null)
        {
            AmbientSource = gameObject.AddComponent<AudioSource>();
            AmbientSource.loop = true;
            AmbientSource.playOnAwake = false;
        }

        // 预热 SFX 池
        for (int i = 0; i < SfxPoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _sfxPool.Add(source);
        }

        // 加载音效目录
        LoadCatalogs();

        Debug.Log($"[AudioManager] 完整初始化完成");
    }

    /// <summary>系统关闭清理（ISystem）</summary>
    public override void Shutdown()
    {
        ServiceLocator.Unregister<AudioManager>();
        Debug.Log("[AudioManager] 已关闭");
        base.Shutdown();
    }

    private void Update()
    {
        if (_isFading)
            UpdateFade();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 音量控制
    // ══════════════════════════════════════════════════════

    /// <summary>设置指定组的音量</summary>
    public void SetVolume(AudioGroup group, float volume)
    {
        _volumes[group] = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    /// <summary>获取指定组的音量</summary>
    public float GetVolume(AudioGroup group)
    {
        return _volumes.TryGetValue(group, out float vol) ? vol : 1f;
    }

    /// <summary>获取最终音量（组音量 × Master）</summary>
    public float GetEffectiveVolume(AudioGroup group)
    {
        float master = _volumes.TryGetValue(AudioGroup.Master, out float m) ? m : 1f;
        float groupVol = _volumes.TryGetValue(group, out float g) ? g : 1f;
        return master * groupVol;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— BGM
    // ══════════════════════════════════════════════════════

    /// <summary>播放背景音乐（带淡入淡出）</summary>
    public void PlayMusic(AudioClip clip, float fadeDuration = 1f)
    {
        if (clip == null) return;

        if (MusicSource.isPlaying && fadeDuration > 0f)
        {
            // 先淡出当前曲目，再淡入新曲目
            _pendingMusic = clip;
            _fadeDuration = fadeDuration;
            _fadeTimer = 0f;
            _isFading = true;
            _isFadingOut = true;
        }
        else
        {
            MusicSource.clip = clip;
            MusicSource.volume = GetEffectiveVolume(AudioGroup.Music);
            MusicSource.Play();
        }
    }

    /// <summary>停止背景音乐</summary>
    public void StopMusic(float fadeDuration = 1f)
    {
        if (fadeDuration > 0f && MusicSource.isPlaying)
        {
            _pendingMusic = null;
            _fadeDuration = fadeDuration;
            _fadeTimer = 0f;
            _isFading = true;
            _isFadingOut = true;
        }
        else
        {
            MusicSource.Stop();
        }
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— SFX
    // ══════════════════════════════════════════════════════

    /// <summary>播放一次性音效</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        var source = GetAvailableSfxSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = GetEffectiveVolume(AudioGroup.SFX) * volumeScale;
        source.Play();
    }

    /// <summary>在指定位置播放3D音效（2D游戏中用于左右声道定位）</summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, GetEffectiveVolume(AudioGroup.SFX) * volumeScale);
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 环境音
    // ══════════════════════════════════════════════════════

    /// <summary>播放环境音（循环）</summary>
    public void PlayAmbient(AudioClip clip)
    {
        if (clip == null) return;
        AmbientSource.clip = clip;
        AmbientSource.volume = GetEffectiveVolume(AudioGroup.Ambient);
        AmbientSource.Play();
    }

    /// <summary>停止环境音</summary>
    public void StopAmbient()
    {
        AmbientSource.Stop();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 通过ID播放
    // ══════════════════════════════════════════════════════

    /// <summary>通过音效ID播放（自动判断分组）</summary>
    /// <param name="audioId">音效目录中注册的ID</param>
    public void Play(string audioId)
    {
        if (!_entryMap.TryGetValue(audioId, out var entry))
        {
            Debug.LogWarning($"[AudioManager] 未注册的音效ID: {audioId}");
            return;
        }

        var clip = GetRandomClip(entry);
        if (clip == null) return;

        float volumeScale = entry.VolumeScale > 0f ? entry.VolumeScale : 1f;

        switch (entry.Group)
        {
            case AudioGroup.Music:
                PlayMusic(clip);
                break;
            case AudioGroup.Ambient:
                PlayAmbient(clip);
                break;
            default:
                PlaySFXWithPitch(clip, volumeScale, entry.PitchMin, entry.PitchMax);
                break;
        }
    }

    /// <summary>通过音效ID在指定位置播放</summary>
    public void PlayAtPosition(string audioId, Vector3 position)
    {
        if (!_entryMap.TryGetValue(audioId, out var entry))
        {
            Debug.LogWarning($"[AudioManager] 未注册的音效ID: {audioId}");
            return;
        }

        var clip = GetRandomClip(entry);
        if (clip == null) return;

        float volumeScale = entry.VolumeScale > 0f ? entry.VolumeScale : 1f;
        PlaySFXAtPosition(clip, position, volumeScale);
    }

    /// <summary>检查音效ID是否已注册</summary>
    public bool HasAudio(string audioId)
    {
        return _entryMap.ContainsKey(audioId);
    }

    // ══════════════════════════════════════════════════════
    // 内部方法
    // ══════════════════════════════════════════════════════

    /// <summary>加载所有音效目录（从 ClipPaths 字符串加载 AudioClip 资源）</summary>
    private void LoadCatalogs()
    {
        if (Catalogs == null) return;

        var resourceManager = ServiceLocator.Get<ResourceManager>();

        for (int i = 0; i < Catalogs.Length; i++)
        {
            var catalog = Catalogs[i];
            if (catalog == null || catalog.Entries == null) continue;

            for (int j = 0; j < catalog.Entries.Length; j++)
            {
                var entry = catalog.Entries[j];
                if (string.IsNullOrEmpty(entry.AudioId)) continue;
                if (entry.ClipPaths == null || entry.ClipPaths.Length == 0) continue;

                // 从 ClipPaths 加载 AudioClip 资源（运行时缓存）
                var clips = new AudioClip[entry.ClipPaths.Length];
                bool hasAnyClip = false;
                for (int k = 0; k < entry.ClipPaths.Length; k++)
                {
                    if (!string.IsNullOrEmpty(entry.ClipPaths[k]) && resourceManager != null)
                    {
                        clips[k] = resourceManager.Load<AudioClip>(entry.ClipPaths[k]);
                        if (clips[k] != null) hasAnyClip = true;
                    }
                }

                if (!hasAnyClip) continue;

                entry.Clips = clips;
                // 写回数组（entry 是 struct 值拷贝）
                catalog.Entries[j] = entry;

                _entryMap[entry.AudioId] = entry;
            }
        }

        Debug.Log($"[AudioManager] 已加载 {_entryMap.Count} 条音效");
    }

    /// <summary>从条目中随机选取一个 AudioClip</summary>
    private static AudioClip GetRandomClip(AudioEntry entry)
    {
        if (entry.Clips == null || entry.Clips.Length == 0) return null;
        if (entry.Clips.Length == 1) return entry.Clips[0];
        return entry.Clips[Random.Range(0, entry.Clips.Length)];
    }

    /// <summary>播放带音高变化的音效</summary>
    private void PlaySFXWithPitch(AudioClip clip, float volumeScale, float pitchMin, float pitchMax)
    {
        if (clip == null) return;

        var source = GetAvailableSfxSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = GetEffectiveVolume(AudioGroup.SFX) * volumeScale;
        source.pitch = pitchMin < pitchMax ? Random.Range(pitchMin, pitchMax) : 1f;
        source.Play();
    }

    private void ApplyVolumes()
    {
        MusicSource.volume = GetEffectiveVolume(AudioGroup.Music);
        AmbientSource.volume = GetEffectiveVolume(AudioGroup.Ambient);
    }

    /// <summary>从池中获取空闲的 AudioSource</summary>
    private AudioSource GetAvailableSfxSource()
    {
        // [PERF] 直接遍历，池大小固定且很小
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            if (!_sfxPool[i].isPlaying)
                return _sfxPool[i];
        }

        // 所有都在播放，抢占最早的
        return _sfxPool[0];
    }

    /// <summary>BGM 淡入淡出更新</summary>
    private void UpdateFade()
    {
        _fadeTimer += Time.unscaledDeltaTime;
        float halfDuration = _fadeDuration * 0.5f;

        if (_isFadingOut)
        {
            float t = Mathf.Clamp01(_fadeTimer / halfDuration);
            MusicSource.volume = Mathf.Lerp(GetEffectiveVolume(AudioGroup.Music), 0f, t);

            if (_fadeTimer >= halfDuration)
            {
                MusicSource.Stop();

                if (_pendingMusic != null)
                {
                    MusicSource.clip = _pendingMusic;
                    MusicSource.Play();
                    _pendingMusic = null;
                    _isFadingOut = false;
                    _fadeTimer = 0f;
                }
                else
                {
                    _isFading = false;
                }
            }
        }
        else
        {
            // 淡入
            float t = Mathf.Clamp01(_fadeTimer / halfDuration);
            MusicSource.volume = Mathf.Lerp(0f, GetEffectiveVolume(AudioGroup.Music), t);

            if (_fadeTimer >= halfDuration)
                _isFading = false;
        }
    }
}
