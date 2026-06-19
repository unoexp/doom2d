// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/03_Core/Scene/SceneLoadSystem.cs
// 场景加载核心系统。管理场景切换、加载进度广播、BGM 过渡。
// ══════════════════════════════════════════════════════════════════════
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景加载核心系统。
///
/// 核心职责：
///   · 异步场景加载/卸载（封装 UnityEngine.SceneManagement.SceneManager）
///   · 完整场景切换（加载目标 → 卸载当前，含 Loading 状态过渡）
///   · 加载进度广播（发布 LoadingStarted/Progress/Completed 事件给 UI 层）
///   · 场景专属 BGM 切换（扫描 ISceneBGMProvider）
///   · 跟踪当前活跃的内容场景
///
/// 使用方式：
///   · AppMain 在 CreateCoreSystems 中通过 CreateSystem 创建
///   · 其他系统通过 ServiceLocator.Get&lt;SceneLoadSystem&gt;() 获取
///   · 调用 SwitchToScene("Gameplay") 进行完整场景切换
/// </summary>
public class SceneLoadSystem : MonoSingleton<SceneLoadSystem>
{
    // ══════════════════════════════════════════════════════
    // 字段
    // ══════════════════════════════════════════════════════

    /// <summary>当前内容场景名称（不含 Gui 等持久场景）</summary>
    private string _currentContentScene;

    /// <summary>当前正在进行的场景切换协程（用于取消或状态查询）</summary>
    private Coroutine _switchCoroutine;

    /// <summary>是否正在进行场景切换</summary>
    public bool IsSwitching => _switchCoroutine != null;

    /// <summary>当前内容场景名称（只读）</summary>
    public string CurrentContentScene => _currentContentScene;

    // ══════════════════════════════════════════════════════
    // 生命周期（ISystem，继承自 MonoSingleton）
    // ══════════════════════════════════════════════════════

    protected override void OnInitialize()
    {
        // 仅注册 ServiceLocator（无配置依赖）
        ServiceLocator.Register<SceneLoadSystem>(this);
        Debug.Log("[SceneLoadSystem] 已注册到 ServiceLocator");
    }

    public override void Initialize()
    {
        base.Initialize();

        // 配置注入后的初始化（当前骨架版本无配置依赖）
        Debug.Log("[SceneLoadSystem] 初始化完成");
    }

    public override void Shutdown()
    {
        // 取消进行中的切换
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
        }

        ServiceLocator.Unregister<SceneLoadSystem>();
        Debug.Log("[SceneLoadSystem] 已关闭");

        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 场景加载 / 卸载
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 异步加载场景。
    /// 返回 UnityEngine.AsyncOperation，调用方可 yield return 等待完成。
    /// 自动发布 SceneLoadStarted/Progress/Completed 事件。
    /// </summary>
    /// <param name="sceneName">场景名称（必须已添加到 Build Settings）</param>
    /// <param name="mode">加载模式（默认 Additive）</param>
    public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
    {
        if (!ValidateSceneName(sceneName)) return null;

        Debug.Log($"[SceneLoadSystem] 开始异步加载场景：{sceneName}（模式：{mode}）");

        EventBus.Publish(new SceneLoadStartedEvent { SceneName = sceneName });

        var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneLoadSystem] 场景加载失败：{sceneName}（LoadSceneAsync 返回 null）");
            return null;
        }

        // 启动协程跟踪加载进度
        StartCoroutine(TrackLoadProgress(asyncOp, sceneName));

        return asyncOp;
    }

    /// <summary>
    /// 异步卸载场景。
    /// 自动发布 SceneUnloadStarted/Completed 事件。
    /// </summary>
    /// <param name="sceneName">要卸载的场景名称</param>
    public AsyncOperation UnloadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadSystem] UnloadSceneAsync: sceneName 为空");
            return null;
        }

        // 防止卸载持久场景
        if (GameConst.PERSISTENT_SCENES.Contains(sceneName))
        {
            Debug.LogWarning($"[SceneLoadSystem] 拒绝卸载持久场景：{sceneName}");
            return null;
        }

        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogWarning($"[SceneLoadSystem] 场景未加载，无需卸载：{sceneName}");
            return null;
        }

        Debug.Log($"[SceneLoadSystem] 开始异步卸载场景：{sceneName}");

        EventBus.Publish(new SceneUnloadStartedEvent { SceneName = sceneName });

        var asyncOp = SceneManager.UnloadSceneAsync(sceneName);
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneLoadSystem] 场景卸载失败：{sceneName}（UnloadSceneAsync 返回 null）");
            return null;
        }

        // 卸载完成后发布事件
        asyncOp.completed += _ =>
        {
            Debug.Log($"[SceneLoadSystem] 场景卸载完成：{sceneName}");
            EventBus.Publish(new SceneUnloadCompletedEvent { SceneName = sceneName });
        };

        return asyncOp;
    }

    // ══════════════════════════════════════════════════════
    // 公有 API —— 场景切换
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 完整场景切换：加载目标场景 → 卸载当前内容场景。
    /// 自动处理 Loading 状态过渡、BGM 切换、进度广播。
    /// </summary>
    /// <param name="targetSceneName">目标场景名称</param>
    public void SwitchToScene(string targetSceneName)
    {
        if (!ValidateSceneName(targetSceneName)) return;

        // 防重入
        if (IsSwitching)
        {
            Debug.LogWarning($"[SceneLoadSystem] 已有场景切换正在进行中，忽略 SwitchToScene({targetSceneName})");
            return;
        }

        // 防止切换到持久场景
        if (GameConst.PERSISTENT_SCENES.Contains(targetSceneName))
        {
            Debug.LogError($"[SceneLoadSystem] 不能通过 SwitchToScene 切换持久场景：{targetSceneName}");
            return;
        }

        _switchCoroutine = StartCoroutine(SwitchToSceneRoutine(targetSceneName));
    }

    // ══════════════════════════════════════════════════════
    // 内部方法 —— 进度跟踪
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 跟踪单个场景的异步加载进度，每帧发布 SceneLoadProgressEvent。
    /// </summary>
    private IEnumerator TrackLoadProgress(AsyncOperation asyncOp, string sceneName)
    {
        while (!asyncOp.isDone)
        {
            float progress = Mathf.Clamp01(asyncOp.progress); // Unity: 0-0.9 为加载，0.9-1.0 为激活
            EventBus.Publish(new SceneLoadProgressEvent
            {
                SceneName = sceneName,
                Progress = progress
            });
            yield return null;
        }

        Debug.Log($"[SceneLoadSystem] 场景加载完成：{sceneName}");
        EventBus.Publish(new SceneLoadCompletedEvent { SceneName = sceneName });
    }

    // ══════════════════════════════════════════════════════
    // 内部方法 —— 场景切换协程
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 场景切换协程。完整流程：
    ///   1. 发布切换开始事件 + Loading 开始事件
    ///   2. 切换到 Loading 状态
    ///   3. 加载目标场景（跟踪进度）
    ///   4. 设置活跃场景 + 切换 BGM
    ///   5. 卸载旧场景
    ///   6. 发布 Loading 完成事件 + 切换到 GamePlay 状态
    /// </summary>
    private IEnumerator SwitchToSceneRoutine(string targetSceneName)
    {
        Debug.Log($"[SceneLoadSystem] ═══ 开始场景切换：{_currentContentScene ?? "(none)"} → {targetSceneName} ═══");

        // ── Step 1: 发布切换开始事件 ──
        EventBus.Publish(new SceneSwitchStartedEvent
        {
            FromScene = _currentContentScene,
            ToScene = targetSceneName
        });

        // ── Step 2: 发布 Loading 开始事件（供 UI 层显示加载画面）──
        EventBus.Publish(new LoadingStartedEvent
        {
            HintText = string.IsNullOrEmpty(_currentContentScene)
                ? $"正在进入 {targetSceneName}..."
                : $"正在前往 {targetSceneName}..."
        });

        // ── Step 3: 切换到 Loading 状态 ──
        if (ServiceLocator.TryGet<GameStateManager>(out var gsm))
        {
            gsm.ChangeState(GameState.Loading);
        }
        else
        {
            Debug.LogError("[SceneLoadSystem] GameStateManager 未注册，无法切换状态");
        }

        // ── Step 4: 加载目标场景 ──
        EventBus.Publish(new LoadingProgressEvent
        {
            Progress = 0f,
            StepDescription = "加载场景资源..."
        });

        var loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        if (loadOp == null)
        {
            Debug.LogError($"[SceneLoadSystem] 场景加载失败：{targetSceneName}");
            HandleSwitchFailure(targetSceneName);
            yield break;
        }

        // 跟踪加载进度（同时发布 SceneLoadProgress + LoadingProgress）
        while (!loadOp.isDone)
        {
            float progress = Mathf.Clamp01(loadOp.progress);
            EventBus.Publish(new SceneLoadProgressEvent
            {
                SceneName = targetSceneName,
                Progress = progress
            });
            EventBus.Publish(new LoadingProgressEvent
            {
                Progress = progress * 0.7f,  // 加载占 70% 进度
                StepDescription = "加载场景资源..."
            });
            yield return null;
        }

        EventBus.Publish(new SceneLoadCompletedEvent { SceneName = targetSceneName });

        // ── Step 5: 设置活跃场景 ──
        var loadedScene = SceneManager.GetSceneByName(targetSceneName);
        if (loadedScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedScene);
            Debug.Log($"[SceneLoadSystem] 活跃场景已设置为：{targetSceneName}");
        }

        // ── Step 6: BGM 切换 ──
        TransitionBGMForScene(loadedScene);

        // ── Step 7: 卸载旧场景 ──
        if (!string.IsNullOrEmpty(_currentContentScene))
        {
            EventBus.Publish(new LoadingProgressEvent
            {
                Progress = 0.8f,
                StepDescription = "卸载旧场景..."
            });

            var unloadOp = UnloadSceneAsync(_currentContentScene);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
            }
        }

        // ── Step 8: 更新当前场景记录 ──
        _currentContentScene = targetSceneName;

        // ── Step 9: 完成 Loading ──
        EventBus.Publish(new LoadingProgressEvent
        {
            Progress = 1f,
            StepDescription = "完成"
        });

        EventBus.Publish(new LoadingCompletedEvent());

        // ── Step 10: 切换到 GamePlay 状态 ──
        if (ServiceLocator.TryGet<GameStateManager>(out var gsm2))
        {
            gsm2.ChangeState(GameState.GamePlay);
        }

        _switchCoroutine = null;
        Debug.Log($"[SceneLoadSystem] ═══ 场景切换完成：{targetSceneName} ═══");
    }

    // ══════════════════════════════════════════════════════
    // 内部方法 —— BGM 管理
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 扫描场景根对象，查找 ISceneBGMProvider 实现并执行 BGM 切换。
    /// 若场景中存在多个实现，仅使用第一个找到的。
    /// </summary>
    private void TransitionBGMForScene(Scene scene)
    {
        if (!scene.isLoaded) return;

        var provider = FindBGMProvider(scene);
        if (provider != null)
        {
            TransitionBGM(provider.BGMAudioId);
        }
        else
        {
            Debug.Log($"[SceneLoadSystem] 场景 {scene.name} 未找到 ISceneBGMProvider，保留当前 BGM");
        }
    }

    /// <summary>
    /// 扫描场景根对象，查找 ISceneBGMProvider 实现。
    /// </summary>
    private ISceneBGMProvider FindBGMProvider(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        int foundCount = 0;
        ISceneBGMProvider firstFound = null;

        for (int i = 0; i < roots.Length; i++)
        {
            // 在根 GameObject 及其子对象上查找
            var providers = roots[i].GetComponentsInChildren<ISceneBGMProvider>();
            for (int j = 0; j < providers.Length; j++)
            {
                if (firstFound == null)
                    firstFound = providers[j];
                foundCount++;
            }
        }

        if (foundCount > 1)
        {
            Debug.LogWarning(
                $"[SceneLoadSystem] 场景 {scene.name} 中发现 {foundCount} 个 ISceneBGMProvider，" +
                $"仅使用第一个：{firstFound?.BGMAudioId}"
            );
        }

        return firstFound;
    }

    /// <summary>
    /// 执行 BGM 切换：通过 AudioManager 播放新 BGM。
    /// 若 audioId 为 null 或空，则停止当前 BGM。
    /// </summary>
    private void TransitionBGM(string audioId)
    {
        if (!ServiceLocator.TryGet<AudioManager>(out var audioManager))
        {
            Debug.LogWarning("[SceneLoadSystem] AudioManager 未注册，跳过 BGM 切换");
            return;
        }

        if (string.IsNullOrEmpty(audioId))
        {
            Debug.Log("[SceneLoadSystem] BGM audioId 为空，停止当前 BGM");
            audioManager.StopMusic();
        }
        else
        {
            Debug.Log($"[SceneLoadSystem] 切换 BGM：{audioId}");
            audioManager.Play(audioId);
        }
    }

    // ══════════════════════════════════════════════════════
    // 内部方法 —— 校验与错误处理
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// 验证场景名称是否有效（非空、非持久场景）。
    /// </summary>
    private bool ValidateSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoadSystem] 场景名称不能为空");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 场景切换失败时的清理处理。
    /// 尝试恢复到之前的 GameState。
    /// </summary>
    private void HandleSwitchFailure(string targetSceneName)
    {
        Debug.LogError($"[SceneLoadSystem] 场景切换失败：{targetSceneName}");

        EventBus.Publish(new LoadingProgressEvent
        {
            Progress = 1f,
            StepDescription = "加载失败"
        });

        EventBus.Publish(new LoadingCompletedEvent());

        // 尝试恢复到 GamePlay 状态
        if (ServiceLocator.TryGet<GameStateManager>(out var gsm))
        {
            gsm.ChangeState(GameState.GamePlay);
        }

        _switchCoroutine = null;
    }
}
