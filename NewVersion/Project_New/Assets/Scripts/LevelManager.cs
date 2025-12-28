using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡管理器 - 负责场景跳转和关卡选择页面的返回
/// </summary>
public class LevelManager : MonoBehaviour
{
    // 关卡选择场景的名称
    private const string LEVEL_SELECT_SCENE = "LevelSelect";
    
    // 单例实例
    private static LevelManager instance;
    
    void Awake()
    {
        // 简单的单例模式
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 通过场景名称加载关卡
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void LoadLevel(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("场景名称为空，无法加载场景！");
        }
    }
    
    /// <summary>
    /// 通过场景索引加载关卡
    /// </summary>
    /// <param name="sceneIndex">场景在Build Settings中的索引</param>
    public void LoadLevel(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError($"场景索引 {sceneIndex} 超出范围！");
        }
    }
    
    /// <summary>
    /// 返回到关卡选择页面（静态方法，可在任何场景调用）
    /// </summary>
    public static void ReturnToLevelSelect()
    {
        SceneManager.LoadScene(LEVEL_SELECT_SCENE);
    }
    
    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
