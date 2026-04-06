// 📁 02_Base/Interfaces/ICraftingSystem.cs
// 制作系统接口定义，供表现层通过ServiceLocator访问

using System.Collections.Generic;

/// <summary>
/// 制作系统接口，表现层通过此接口与业务层通信
/// 🏗️ 定义在02_Base层，03_Core实现，05_Show引用
/// </summary>
public interface ICraftingSystem
{
    List<RecipeDefinitionSO> GetUnlockedRecipes();
    RecipeDefinitionSO GetRecipe(string recipeId);
    CraftingResult Validate(string recipeId, bool nearWorkbench = false);
}
