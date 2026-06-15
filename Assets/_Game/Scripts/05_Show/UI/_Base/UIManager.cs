// ══════════════════════════════════════════════════════════════════════
// 📁 Assets/_Game/05_Show/UI/_Base/UIManager.cs
// UI栈式管理器。管理所有UI面板的打开、关闭、栈式层级。
// ══════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI面板栈式管理器。
///
/// 核心职责：
///   · 注册/查找所有 UIPanel 实例
///   · 栈式管理面板层级（后开的在最上层）
///   · 自动处理焦点切换（Focus/Unfocus）
///   · 提供 CloseAll / CloseTop 等便捷接口
///
/// 设计说明：
///   · 继承 MonoSingleton，全局唯一
///   · 不直接创建面板，面板预先挂载在场景中并自注册
///   · HUD 等常驻面板不入栈，通过 ShowHUD/HideHUD 独立管理
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    // ══════════════════════════════════════════════════════
    // 面板注册表
    // ══════════════════════════════════════════════════════

    /// <summary>所有已注册的面板（panelId → panel）</summary>
    private readonly Dictionary<string, UIPanel> _panelRegistry
        = new Dictionary<string, UIPanel>();

    /// <summary>面板栈（栈顶为当前活跃面板）</summary>
    private readonly List<UIPanel> _panelStack = new List<UIPanel>();

    /// <summary>常驻HUD面板列表（不入栈）</summary>
    private readonly List<UIPanel> _hudPanels = new List<UIPanel>();

    // ══════════════════════════════════════════════════════
    // 属性
    // ══════════════════════════════════════════════════════

    /// <summary>当前栈顶面板</summary>
    public UIPanel TopPanel => _panelStack.Count > 0 ? _panelStack[_panelStack.Count - 1] : null;

    /// <summary>是否有任何面板处于打开状态</summary>
    public bool HasOpenPanel => _panelStack.Count > 0;

    /// <summary>打开的面板数量</summary>
    public int OpenPanelCount => _panelStack.Count;

    // ══════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.Register<UIManager>(this);
    }

    public override void Shutdown()
    {
        ServiceLocator.Unregister<UIManager>();
        base.Shutdown();
    }

    // ══════════════════════════════════════════════════════
    // 面板注册
    // ══════════════════════════════════════════════════════

    /// <summary>注册面板到管理器</summary>
    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        string id = panel.PanelId;

        if (_panelRegistry.ContainsKey(id))
        {
            Debug.LogWarning($"[UIManager] 面板已注册: {id}，跳过重复注册");
            return;
        }

        _panelRegistry[id] = panel;
    }

    /// <summary>注销面板</summary>
    public void UnregisterPanel(UIPanel panel)
    {
        if (panel == null) return;
        _panelRegistry.Remove(panel.PanelId);
        _panelStack.Remove(panel);
        _hudPanels.Remove(panel);
    }

    /// <summary>通过ID获取面板</summary>
    public T GetPanel<T>(string panelId) where T : UIPanel
    {
        if (_panelRegistry.TryGetValue(panelId, out var panel))
            return panel as T;
        return null;
    }

    // ══════════════════════════════════════════════════════
    // 栈式面板管理
    // ══════════════════════════════════════════════════════

    /// <summary>打开指定面板（压入栈顶）</summary>
    public void OpenPanel(string panelId)
    {
        if (!_panelRegistry.TryGetValue(panelId, out var panel))
        {
            Debug.LogWarning($"[UIManager] 未找到面板: {panelId}");
            return;
        }

        OpenPanel(panel);
    }

    /// <summary>打开指定面板（压入栈顶）</summary>
    public void OpenPanel(UIPanel panel)
    {
        if (panel == null) return;

        // 已在栈中则不重复打开
        if (_panelStack.Contains(panel)) return;

        // 当前栈顶失去焦点
        if (_panelStack.Count > 0)
        {
            _panelStack[_panelStack.Count - 1].Unfocus();
        }

        // 压入栈顶
        _panelStack.Add(panel);
        panel.Show();
        panel.Focus();

        // 发布面板打开事件
        EventBus.Publish(new UIPanelOpenedEvent { PanelId = panel.PanelId });
    }

    /// <summary>关闭栈顶面板</summary>
    public void CloseTopPanel()
    {
        if (_panelStack.Count == 0) return;

        var top = _panelStack[_panelStack.Count - 1];
        _panelStack.RemoveAt(_panelStack.Count - 1);
        top.Hide();

        // 新栈顶获得焦点
        if (_panelStack.Count > 0)
        {
            _panelStack[_panelStack.Count - 1].Focus();
        }

        EventBus.Publish(new UIPanelClosedEvent { PanelId = top.PanelId });
    }

    /// <summary>关闭指定面板</summary>
    public void ClosePanel(string panelId)
    {
        if (!_panelRegistry.TryGetValue(panelId, out var panel)) return;
        ClosePanel(panel);
    }

    /// <summary>关闭指定面板</summary>
    public void ClosePanel(UIPanel panel)
    {
        if (panel == null) return;

        int index = _panelStack.IndexOf(panel);
        if (index < 0) return;

        bool wasTop = index == _panelStack.Count - 1;
        _panelStack.RemoveAt(index);
        panel.Hide();

        // 如果关闭的是栈顶，新栈顶获得焦点
        if (wasTop && _panelStack.Count > 0)
        {
            _panelStack[_panelStack.Count - 1].Focus();
        }

        EventBus.Publish(new UIPanelClosedEvent { PanelId = panel.PanelId });
    }

    /// <summary>关闭所有面板</summary>
    public void CloseAllPanels()
    {
        // [PERF] 倒序关闭避免频繁移位
        for (int i = _panelStack.Count - 1; i >= 0; i--)
        {
            _panelStack[i].Hide();
        }
        _panelStack.Clear();

        EventBus.Publish(new UIAllPanelsClosedEvent());
    }

    // ══════════════════════════════════════════════════════
    // HUD 管理
    // ══════════════════════════════════════════════════════

    /// <summary>注册为HUD面板（常驻，不入栈）</summary>
    public void RegisterHUD(UIPanel hud)
    {
        if (hud == null || _hudPanels.Contains(hud)) return;
        _hudPanels.Add(hud);
    }

    /// <summary>显示所有HUD</summary>
    public void ShowAllHUD()
    {
        for (int i = 0; i < _hudPanels.Count; i++)
        {
            _hudPanels[i].Show();
        }
    }

    /// <summary>隐藏所有HUD</summary>
    public void HideAllHUD()
    {
        for (int i = 0; i < _hudPanels.Count; i++)
        {
            _hudPanels[i].Hide();
        }
    }

    /// <summary>切换指定面板（已开则关，已关则开）</summary>
    public void TogglePanel(string panelId)
    {
        if (!_panelRegistry.TryGetValue(panelId, out var panel)) return;

        if (panel.IsVisible)
            ClosePanel(panel);
        else
            OpenPanel(panel);
    }
}
