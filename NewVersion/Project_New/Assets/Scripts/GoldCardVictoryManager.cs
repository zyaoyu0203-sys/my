using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class GoldCardVictoryManager : MonoBehaviour
{
    public static GoldCardVictoryManager Instance { get; private set; }

    [Header("Level Clear UI")]
    public TextMeshProUGUI levelClearText;
    public Image levelClearFlash;
    public float levelClearFontSize = 120f;
    public Color levelClearColor = Color.yellow;
    public float levelClearDisplayTime = 2f;

    [Header("UI to Hide")]
    public GameObject[] uiObjectsToHide;

    [Header("Dialogue")]
    public float dialogueDelay = 1f;
    public DialogueSystem dialogueSystem;

    [Header("Item Choice")]
    public GameObject itemChoicePanel;
    public Image item1Image;
    public Image item2Image;
    public Button item1Button;
    public Button item2Button;
    public Sprite item1Sprite;
    public Sprite item2Sprite;
    public float itemFadeDuration = 0.5f;

    [Header("Scene Transition")]
    public string nextSceneName = "TestScene";
    public float sceneTransitionDelay = 1f;
    public bool autoTransition = true;

    [Header("Fade Effect")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private bool isVictoryTriggered = false;
    private int selectedItemIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (levelClearText != null)
        {
            levelClearText.gameObject.SetActive(false);
        }
        
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        if (itemChoicePanel != null)
        {
            itemChoicePanel.SetActive(false);
        }

        // 绑定按钮事件
        if (item1Button != null)
        {
            item1Button.onClick.AddListener(() => OnItemSelected(0));
            Debug.Log("Item1Button事件已绑定");
        }
        else
        {
            Debug.LogWarning("Item1Button未设置！");
        }

        if (item2Button != null)
        {
            item2Button.onClick.AddListener(() => OnItemSelected(1));
            Debug.Log("Item2Button事件已绑定");
        }
        else
        {
            Debug.LogWarning("Item2Button未设置！");
        }
        
        Debug.Log("GoldCardVictoryManager已启动！按V键测试胜利流程");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("=== 按下V键，测试胜利流程 ===");
            TriggerVictory(new List<Card>());
        }
    }

    public void TriggerVictory(List<Card> goldCards)
    {
        if (isVictoryTriggered)
        {
            Debug.LogWarning("胜利流程已触发，忽略重复调用");
            return;
        }

        isVictoryTriggered = true;
        Debug.Log("=== 金牌胜利流程开始 ===");
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        // 1. 隐藏UI
        Debug.Log("【步骤1】隐藏UI");
        HideUIObjects();

        // 2. 显示Level Clear
        Debug.Log("【步骤2】显示Level Clear");
        yield return StartCoroutine(ShowLevelClear());
        Debug.Log("【步骤2】Level Clear显示完成");

        // 3. 等待
        Debug.Log($"【步骤3】等待 {dialogueDelay} 秒");
        yield return new WaitForSeconds(dialogueDelay);
        Debug.Log("【步骤3】等待完成");

        // 4. 触发胜利对话
        Debug.Log("【步骤4】开始触发胜利对话");
        if (dialogueSystem != null)
        {
            Debug.Log("DialogueSystem存在，调用StartVictoryDialogue()");
            dialogueSystem.StartVictoryDialogue();
            
            Debug.Log("等待对话结束...");
            int waitCount = 0;
            while (dialogueSystem.IsDialogueActive())
            {
                waitCount++;
                if (waitCount % 60 == 0) // 每60帧（约1秒）输出一次
                {
                    Debug.Log($"仍在等待对话结束... (等待了约{waitCount/60}秒)");
                }
                yield return null;
            }
            Debug.Log("【步骤4】对话已结束！");
        }
        else
        {
            Debug.LogError("DialogueSystem未设置！跳过对话");
        }

        // 5. 显示道具选择界面
        Debug.Log("【步骤5】显示道具选择界面");
        yield return StartCoroutine(ShowItemChoice());
        Debug.Log("【步骤5】道具界面显示完成");

        // 6. 等待玩家选择
        Debug.Log("【步骤6】等待玩家选择道具...");
        while (selectedItemIndex == -1)
        {
            yield return null;
        }

        Debug.Log($"【步骤6】玩家选择了道具 {selectedItemIndex}");

        // 7. 场景切换
        if (autoTransition)
        {
            Debug.Log($"【步骤7】等待 {sceneTransitionDelay} 秒后切换场景");
            yield return new WaitForSeconds(sceneTransitionDelay);
            TransitionToNextScene();
        }

        Debug.Log("=== 金牌胜利流程结束 ===");
    }

    private IEnumerator ShowItemChoice()
    {
        Debug.Log(">>> ShowItemChoice 开始");

        if (itemChoicePanel == null)
        {
            Debug.LogError("ItemChoicePanel是null！自动选择第一个道具");
            selectedItemIndex = 0;
            yield break;
        }

        Debug.Log($"ItemChoicePanel: {itemChoicePanel.name}");

        // 设置道具图片
        if (item1Image != null && item1Sprite != null)
        {
            item1Image.sprite = item1Sprite;
            item1Image.color = new Color(1, 1, 1, 0);
            Debug.Log("Item1图片已设置为透明");
        }
        else
        {
            Debug.LogWarning($"Item1设置失败 - Image: {item1Image != null}, Sprite: {item1Sprite != null}");
        }

        if (item2Image != null && item2Sprite != null)
        {
            item2Image.sprite = item2Sprite;
            item2Image.color = new Color(1, 1, 1, 0);
            Debug.Log("Item2图片已设置为透明");
        }
        else
        {
            Debug.LogWarning($"Item2设置失败 - Image: {item2Image != null}, Sprite: {item2Sprite != null}");
        }

        // 显示面板
        itemChoicePanel.SetActive(true);
        Debug.Log("ItemChoicePanel已激活");

        // 道具淡入
        if (item1Image != null)
        {
            Debug.Log($"Item1开始淡入 (持续{itemFadeDuration}秒)");
            item1Image.DOFade(1f, itemFadeDuration).SetEase(Ease.OutQuad);
        }

        if (item2Image != null)
        {
            Debug.Log($"Item2开始淡入 (持续{itemFadeDuration}秒)");
            item2Image.DOFade(1f, itemFadeDuration).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(itemFadeDuration);
        Debug.Log(">>> ShowItemChoice 完成，道具已完全显示");
    }

    private void OnItemSelected(int index)
    {
        Debug.Log($"!!! OnItemSelected 被调用: index={index}");

        if (selectedItemIndex != -1)
        {
            Debug.LogWarning("已经选择过道具，忽略重复点击");
            return;
        }

        selectedItemIndex = index;
        Debug.Log($"选择了道具 {index}，开始播放选择动画");

        StartCoroutine(ItemSelectedAnimation(index));
    }

    private IEnumerator ItemSelectedAnimation(int selectedIndex)
    {
        Debug.Log($">>> ItemSelectedAnimation 开始 (选择了道具{selectedIndex})");

        // 禁用按钮
        if (item1Button != null) item1Button.interactable = false;
        if (item2Button != null) item2Button.interactable = false;
        Debug.Log("按钮已禁用");

        // 未选择的道具淡出
        Image unselectedImage = selectedIndex == 0 ? item2Image : item1Image;
        
        if (unselectedImage != null)
        {
            Debug.Log($"未选择的道具开始淡出 (持续{itemFadeDuration}秒)");
            unselectedImage.DOFade(0f, itemFadeDuration).SetEase(Ease.InQuad);
        }

        // 面板背景淡出
        Image panelBg = itemChoicePanel.GetComponent<Image>();
        if (panelBg != null)
        {
            Debug.Log("面板背景开始淡出");
            panelBg.DOFade(0f, itemFadeDuration).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(itemFadeDuration);
        Debug.Log("未选择道具和背景淡出完成");

        // 选中的道具也淡出
        Image selectedImage = selectedIndex == 0 ? item1Image : item2Image;
        if (selectedImage != null)
        {
            Debug.Log($"选中的道具开始淡出 (持续{itemFadeDuration}秒)");
            selectedImage.DOFade(0f, itemFadeDuration).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(itemFadeDuration);
        Debug.Log("选中道具淡出完成");

        // 隐藏面板
        itemChoicePanel.SetActive(false);
        Debug.Log("ItemChoicePanel已隐藏");

        Debug.Log(">>> ItemSelectedAnimation 完成");
    }

    private IEnumerator ShowLevelClear()
    {
        if (levelClearText == null)
        {
            Debug.LogWarning("Level Clear Text未设置");
            yield break;
        }

        RectTransform rectTransform = levelClearText.rectTransform;
        Vector2 originalAnchoredPos = rectTransform.anchoredPosition;

        // 背景闪光效果
        if (levelClearFlash != null)
        {
            levelClearFlash.gameObject.SetActive(true);
            Color flashColor = levelClearFlash.color;
            flashColor.a = 0;
            levelClearFlash.color = flashColor;
            
            Sequence flashSeq = DOTween.Sequence();
            flashSeq.Append(levelClearFlash.DOFade(0.8f, 0.1f));
            flashSeq.Append(levelClearFlash.DOFade(0f, 0.3f));
        }

        levelClearText.gameObject.SetActive(true);
        levelClearText.text = "LEVEL CLEAR";
        levelClearText.fontSize = levelClearFontSize;
        levelClearText.color = levelClearColor;
        levelClearText.alpha = 0;

        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.zero;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(levelClearText.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(rectTransform.DOScale(Vector3.one * 1.2f, 0.5f).SetEase(Ease.OutBack));
        seq.Append(levelClearText.DOColor(Color.white, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(levelClearColor, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(Color.white, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(levelClearColor, 0.15f).SetEase(Ease.InOutQuad));
        
        rectTransform.DOLocalRotate(new Vector3(0, 0, 5), 0.5f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo);
        
        if (dialogueSystem != null && dialogueSystem.cameraShake != null)
        {
            dialogueSystem.cameraShake.Shake();
        }

        yield return new WaitForSeconds(levelClearDisplayTime);

        Sequence outSeq = DOTween.Sequence();
        outSeq.Append(levelClearText.DOFade(0f, 0.5f).SetEase(Ease.InQuad));
        outSeq.Join(rectTransform.DOScale(Vector3.one * 1.5f, 0.5f).SetEase(Ease.InQuad));
        
        if (levelClearFlash != null)
        {
            levelClearFlash.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        levelClearText.gameObject.SetActive(false);
        
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }

    private void HideUIObjects()
    {
        foreach (GameObject obj in uiObjectsToHide)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        Debug.Log($"已隐藏 {uiObjectsToHide.Length} 个UI对象");
    }

    public void TransitionToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("下一个场景名称未设置！");
            return;
        }

        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        Debug.Log("开始淡出到黑屏...");
        
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            
            fadeCanvasGroup.alpha = 1f;
            Debug.Log("黑屏淡出完成");
        }
        
        Debug.Log($"正在切换到场景: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    public int GetSelectedItem()
    {
        return selectedItemIndex;
    }
}