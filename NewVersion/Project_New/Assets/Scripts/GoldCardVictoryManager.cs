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

    [Header("Victory Animation")]
    public float cardScaleUp = 2f;
    public float cardMoveTime = 1f;
    public float cardScaleTime = 0.8f;
    public float glowDuration = 1.5f;

    [Header("Level Clear UI")]
    public TextMeshProUGUI levelClearText;
    public Image levelClearFlash;  // 新增：背景闪光
    public float levelClearFontSize = 120f;
    public Color levelClearColor = Color.yellow;
    public float levelClearDisplayTime = 2f;

    [Header("UI to Hide")]
    public GameObject[] uiObjectsToHide;

    [Header("Dialogue")]
    public float dialogueDelay = 1f;
    public DialogueSystem dialogueSystem;

    [Header("Scene Transition")]
    public string nextSceneName = "TestScene";
    public float sceneTransitionDelay = 2f;
    public bool autoTransition = true;

    [Header("Fade Effect")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private bool isVictoryTriggered = false;

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
        
        Debug.Log("GoldCardVictoryManager已启动！按V键测试胜利流程");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("测试：手动触发胜利流程");
            TestVictory();
        }
    }

    private void TestVictory()
    {
        List<Card> testCards = new List<Card>();
        Card[] allCards = FindObjectsOfType<Card>();
        
        for (int i = 0; i < Mathf.Min(3, allCards.Length); i++)
        {
            if (allCards[i] != null)
            {
                testCards.Add(allCards[i]);
            }
        }
        
        if (testCards.Count == 0)
        {
            Debug.Log("场景中没有卡牌，跳过动画测试");
            StartCoroutine(TestVictoryWithoutCards());
        }
        else
        {
            Debug.Log($"找到 {testCards.Count} 张卡牌，开始测试");
            TriggerVictory(testCards);
        }
    }

    private IEnumerator TestVictoryWithoutCards()
    {
        HideUIObjects();
        yield return StartCoroutine(ShowLevelClear());
        yield return new WaitForSeconds(dialogueDelay);
        
        if (dialogueSystem != null)
        {
            dialogueSystem.StartVictoryDialogue();
            while (dialogueSystem.IsDialogueActive())
            {
                yield return null;
            }
        }
        
        if (autoTransition)
        {
            yield return new WaitForSeconds(sceneTransitionDelay);
            TransitionToNextScene();
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
        StartCoroutine(VictorySequence(goldCards));
    }

    private IEnumerator VictorySequence(List<Card> goldCards)
    {
        yield return StartCoroutine(GoldCardAnimation(goldCards));
        HideUIObjects();
        yield return StartCoroutine(ShowLevelClear());
        yield return new WaitForSeconds(dialogueDelay);

        if (dialogueSystem != null)
        {
            dialogueSystem.StartVictoryDialogue();
            
            while (dialogueSystem.IsDialogueActive())
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("DialogueSystem未设置");
        }

        if (autoTransition)
        {
            yield return new WaitForSeconds(sceneTransitionDelay);
            TransitionToNextScene();
        }

        Debug.Log("=== 金牌胜利流程结束 ===");
    }

    private IEnumerator GoldCardAnimation(List<Card> goldCards)
    {
        Vector3 screenCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));

        float spacing = 2f;
        float totalWidth = (goldCards.Count - 1) * spacing;
        float startX = screenCenter.x - totalWidth / 2f;

        for (int i = 0; i < goldCards.Count; i++)
        {
            Card card = goldCards[i];
            if (card == null) continue;

            Vector3 targetPos = new Vector3(startX + i * spacing, screenCenter.y, screenCenter.z);
            card.transform.DOMove(targetPos, cardMoveTime).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.3f);

        foreach (var card in goldCards)
        {
            if (card != null)
            {
                card.transform.DOScale(Vector3.one * cardScaleUp, cardScaleTime).SetEase(Ease.OutBack);
            }
        }

        yield return new WaitForSeconds(cardScaleTime);
        yield return StartCoroutine(FlashEffect(goldCards));
    }

    private IEnumerator FlashEffect(List<Card> goldCards)
    {
        foreach (var card in goldCards)
        {
            if (card != null && card.cardVisual != null && card.cardVisual.cardImage != null)
            {
                StartCoroutine(PulseCard(card));
            }
        }

        yield return new WaitForSeconds(glowDuration);
    }

    private IEnumerator PulseCard(Card card)
    {
        Image cardImage = card.cardVisual.cardImage;
        Color originalColor = cardImage.color;
        
        float elapsed = 0f;
        while (elapsed < glowDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 3f, 1f);
            cardImage.color = Color.Lerp(originalColor, Color.white, t);
            yield return null;
        }
        
        cardImage.color = originalColor;
    }

    private IEnumerator ShowLevelClear()
    {
        if (levelClearText == null)
        {
            Debug.LogWarning("Level Clear Text未设置");
            yield break;
        }

        // === 获取RectTransform和保存初始位置 ===
        RectTransform rectTransform = levelClearText.rectTransform;
        Vector2 originalAnchoredPos = rectTransform.anchoredPosition;

        // === 背景闪光效果 ===
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

        // === 重置到初始状态 ===
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.zero;
        
        Sequence seq = DOTween.Sequence();
        
        // 淡入
        seq.Append(levelClearText.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
        
        // 弹性放大
        seq.Join(rectTransform.DOScale(Vector3.one * 1.2f, 0.5f).SetEase(Ease.OutBack));
        
        // 颜色闪烁
        seq.Append(levelClearText.DOColor(Color.white, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(levelClearColor, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(Color.white, 0.15f).SetEase(Ease.InOutQuad));
        seq.Append(levelClearText.DOColor(levelClearColor, 0.15f).SetEase(Ease.InOutQuad));
        
        // 轻微旋转
        rectTransform.DOLocalRotate(new Vector3(0, 0, 5), 0.5f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo);
        
        // 摄像机抖动
        if (dialogueSystem != null && dialogueSystem.cameraShake != null)
        {
            dialogueSystem.cameraShake.Shake();
        }

        yield return new WaitForSeconds(levelClearDisplayTime);

        // === 消失动画：向上飘+淡出 ===
        Sequence outSeq = DOTween.Sequence();
        outSeq.Append(levelClearText.DOFade(0f, 0.5f).SetEase(Ease.InQuad));
        outSeq.Join(rectTransform.DOAnchorPosY(originalAnchoredPos.y + 100f, 0.5f).SetEase(Ease.InQuad));
        outSeq.Join(rectTransform.DOScale(Vector3.one * 1.5f, 0.5f).SetEase(Ease.InQuad));
        
        if (levelClearFlash != null)
        {
            levelClearFlash.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        levelClearText.gameObject.SetActive(false);
        
        // === 完全重置 ===
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
        Debug.Log("开始淡出...");
        
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
        }
        
        Debug.Log($"正在切换到场景: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }
}