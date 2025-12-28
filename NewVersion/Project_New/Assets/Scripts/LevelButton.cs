using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡按钮 - 挂载到UI按钮上，点击后跳转到指定场景
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("目标场景设置")]
    [Tooltip("要跳转的目标场景名称")]
    [SerializeField] private string targetSceneName;
    
    private Button button;
    private LevelManager levelManager;
    
    void Awake()
    {
        // 获取Button组件
        button = GetComponent<Button>();
        
        // 添加点击事件监听
        button.onClick.AddListener(OnButtonClick);
    }
    
    void Start()
    {
        // 查找LevelManager实例
        levelManager = FindObjectOfType<LevelManager>();
        
        if (levelManager == null)
        {
            Debug.LogWarning("场景中未找到LevelManager，请确保场景中有LevelManager对象！");
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClick()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"按钮 {gameObject.name} 未设置目标场景名称！");
            return;
        }
        
        if (levelManager != null)
        {
            levelManager.LoadLevel(targetSceneName);
        }
        else
        {
            // 如果找不到LevelManager实例，直接使用SceneManager加载
            Debug.LogWarning("使用备用方法加载场景");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }
    
    /// <summary>
    /// 在Inspector中设置目标场景名称（可选）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
    }
    
    void OnDestroy()
    {
        // 移除点击事件监听，防止内存泄漏
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
