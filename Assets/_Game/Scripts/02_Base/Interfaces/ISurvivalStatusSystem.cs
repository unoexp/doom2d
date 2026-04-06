// 📁 02_Base/Interfaces/ISurvivalStatusSystem.cs
// 生存状态系统接口定义，供表现层通过ServiceLocator访问

/// <summary>
/// 生存状态系统接口，表现层通过此接口与业务层通信
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface ISurvivalStatusSystem
{
    float GetValue(SurvivalAttributeType type);
    float GetMaxValue(SurvivalAttributeType type);
}
