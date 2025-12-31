using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject dialoguePanel;
    public Image panelImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    
    [Header("角色引用")]
    public Material friendMaterial;
    public CharacterController friendCharacter;
    
    [Header("摄像机抖动")]
    public CameraShake cameraShake;
    
    [Header("设置")]
    public float typeSpeed = 0.05f;
    
    [Header("初始化延迟")]
    [Tooltip("等待卡牌系统初始化的时间（秒）")]
    public float cardSystemInitDelay = 1f;
    
    [Header("颜色设置")]
    public Color yourColor = Color.white;
    public Color bobbyColor = new Color(0.5f, 1f, 1f);
    
    [Header("需要隐藏的UI元素")]
    public GameObject[] hideObjects;
    public float fadeInDuration = 1.5f;
    
    [System.Serializable]
    public class Dialogue
    {
        public string speaker;
        public string text;
        public bool shakeBobby;
        public bool fadeInBobby;
    }
    
    private List<Dialogue> dialogues = new List<Dialogue>();
    private int currentLine = 0;
    private bool canContinue = false;
    private bool isTyping = false;
    private string fullText = "";
    private Coroutine typeCoroutine;
    
    // 对话状态标志
    private bool isDialogueActive = false;
    
    void Start()
    {
        CreateDialogues();
        
        // 初始化Bobby为透明
        if(friendMaterial != null)
        {
            Color c = friendMaterial.color;
            c.a = 0f;
            friendMaterial.color = c;
            Debug.Log("初始化：设置Bobby透明度为0");
        }
        
        // 隐藏对话框
        dialoguePanel.SetActive(false);
        
        // 使用CanvasGroup隐藏其他UI
        foreach(GameObject obj in hideObjects)
        {
            if(obj != null)
            {
                obj.SetActive(true);
                
                CanvasGroup group = obj.GetComponent<CanvasGroup>();
                if(group == null)
                {
                    group = obj.AddComponent<CanvasGroup>();
                }
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                
                Debug.Log($"已使用CanvasGroup隐藏UI: {obj.name}");
            }
        }
        
        // 等待卡牌系统初始化完成后再开始对话
        StartCoroutine(WaitForCardSystemThenStart());
    }
    
    IEnumerator WaitForCardSystemThenStart()
    {
        yield return null;
        yield return new WaitForSeconds(cardSystemInitDelay);
        Debug.Log($"等待{cardSystemInitDelay}秒后，卡牌系统初始化完成，开始播放睁眼动画和对话");
        StartCoroutine(StartWithWakeUp());
    }
    
    IEnumerator StartWithWakeUp()
    {
        if(cameraShake != null)
        {
            cameraShake.WakeUpSequence();
        }
        
        yield return new WaitForSeconds(3.5f);
        StartDialogue();
    }
    
    void CreateDialogues()
    {
        dialogues.Add(new Dialogue { speaker = "You", text = "I... where am I? Why does my head hurt so much..." });
        dialogues.Add(new Dialogue { speaker = "You", text = "......" });
        dialogues.Add(new Dialogue { speaker = "You", text = "I think I remember now..." });
        dialogues.Add(new Dialogue { speaker = "You", text = "Last night... I got beaten up... It was because I owed money from gambling..." });
        dialogues.Add(new Dialogue { speaker = "You", text = "Damn it, my luck has been so terrible lately. I didn't win a single game..." });
        dialogues.Add(new Dialogue { speaker = "You", text = "I've lost contact with family and friends... Now I can't pay back the money, I have to run away..." });
        
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "Hey bro, you're awake?", shakeBobby = true, fadeInBobby = true });
        
        dialogues.Add(new Dialogue { speaker = "You", text = "You... Bobby?!" });
        dialogues.Add(new Dialogue { speaker = "You", text = "Wait, aren't you my friend? What are you doing here?" });
        
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "Friend? What are you talking about? Did you hit your head too hard?", shakeBobby = true });
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "The graduation ceremony is about to start, dude.", shakeBobby = true });
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "You overslept, so I've been waiting here for you.", shakeBobby = true });
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "Oh, and guess what? Kiki is there too. You know, the girl you like.", shakeBobby = true });
        
        dialogues.Add(new Dialogue { speaker = "You", text = "Wait... Am I not in the real world anymore?" });
        dialogues.Add(new Dialogue { speaker = "You", text = "I want to go... but I can't move..." });
        dialogues.Add(new Dialogue { speaker = "You", text = "Poker cards...? Do I have to finish these games to..." });
    }
    
    void CreateVictoryDialogues()
    {
        dialogues.Clear();
        dialogues.Add(new Dialogue { speaker = "Bobby", text = "Let's go, the graduation ceremony is starting!", shakeBobby = true });
        dialogues.Add(new Dialogue { speaker = "You", text = "Wait... why can't I move?" });
        dialogues.Add(new Dialogue { speaker = "You", text = "Bobby! Don't leave me!" });
        Debug.Log($"胜利对话已创建，共{dialogues.Count}句");
    }
    
    void Update()
    {
        if(isDialogueActive && Input.GetMouseButtonDown(0))
        {
            if(isTyping)
            {
                if(typeCoroutine != null)
                {
                    StopCoroutine(typeCoroutine);
                }
                dialogueText.text = fullText;
                isTyping = false;
                canContinue = true;
            }
            else if(canContinue)
            {
                NextLine();
            }
        }
        
        if(Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("测试摄像机抖动！");
            if(cameraShake != null)
            {
                cameraShake.Shake();
            }
        }

        // 调试按键
        if(Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log($"[DialogueSystem状态] isDialogueActive={isDialogueActive}, currentLine={currentLine}, totalLines={dialogues.Count}");
        }
    }
    
    void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        isDialogueActive = true;
        currentLine = 0;
        Debug.Log("对话开始，已激活对话输入响应");
        ShowLine();
    }
    
    public void StartVictoryDialogue()
    {
        Debug.Log("=== StartVictoryDialogue 被调用 ===");
        CreateVictoryDialogues();
        
        dialoguePanel.SetActive(true);
        isDialogueActive = true;
        currentLine = 0;
        Debug.Log($"胜利对话开始，isDialogueActive={isDialogueActive}");
        ShowLine();
    }
    
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    void ShowLine()
    {
        Debug.Log($"ShowLine: currentLine={currentLine}, totalLines={dialogues.Count}");

        if(currentLine >= dialogues.Count)
        {
            Debug.Log("对话行数已达到总数，调用EndDialogue()");
            EndDialogue();
            return;
        }
        
        canContinue = false;
        Dialogue line = dialogues[currentLine];
        
        speakerNameText.text = line.speaker;
        if(line.speaker == "You")
        {
            speakerNameText.color = yourColor;
            dialogueText.color = yourColor;
        }
        else if(line.speaker == "Bobby")
        {
            speakerNameText.color = bobbyColor;
            dialogueText.color = bobbyColor;
        }
        
        if(line.fadeInBobby)
        {
            StartCoroutine(FadeInFriend());
            
            if(cameraShake != null)
            {
                Debug.Log("Bobby出现，触发摄像机抖动！");
                cameraShake.Shake();
            }
        }
        
        if(line.shakeBobby && friendCharacter != null)
        {
            Debug.Log("调用Bobby抖动！");
            friendCharacter.Talk();
        }
        
        if(currentLine == 7)
        {
            if(cameraShake != null)
            {
                Debug.Log("主角惊讶，触发摄像机抖动！");
                cameraShake.Shake();
            }
        }
        
        typeCoroutine = StartCoroutine(TypeText(line.text));
    }
    
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        fullText = text;
        dialogueText.text = "";
        
        foreach(char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        
        isTyping = false;
        canContinue = true;
    }
    
    IEnumerator FadeInFriend()
    {
        if(friendMaterial == null)
        {
            Debug.LogError("friendMaterial是空的！");
            yield break;
        }
        
        Debug.Log("=== 开始淡入Bobby ===");
        
        float duration = 2f;
        float elapsed = 0f;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(0f, 1f, t);
            
            Color c = friendMaterial.color;
            c.a = alpha;
            friendMaterial.color = c;
            
            yield return null;
        }
        
        Color finalColor = friendMaterial.color;
        finalColor.a = 1.0f;
        friendMaterial.color = finalColor;
        
        Debug.Log("=== Bobby淡入完成 ===");
    }
    
    IEnumerator FadeOutFriend()
    {
        if(friendMaterial == null)
        {
            Debug.LogError("friendMaterial是空的！");
            yield break;
        }
        
        Debug.Log("=== 开始淡出Bobby ===");
        
        float duration = 1.5f;
        float elapsed = 0f;
        float startAlpha = friendMaterial.color.a;
        
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            Color c = friendMaterial.color;
            c.a = alpha;
            friendMaterial.color = c;
            
            yield return null;
        }
        
        Color finalColor = friendMaterial.color;
        finalColor.a = 0f;
        friendMaterial.color = finalColor;
        
        Debug.Log("=== Bobby淡出完成 ===");
    }
    
    void NextLine()
    {
        currentLine++;
        Debug.Log($"NextLine: 移动到第{currentLine}行");
        ShowLine();
    }
    
    void EndDialogue()
    {
        Debug.Log("=== EndDialogue 被调用 ===");
        
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        
        Debug.Log($"对话结束！isDialogueActive已设为false, dialogues.Count={dialogues.Count}");
        
        // 如果当前对话是胜利对话（只有3句）
        if(dialogues.Count == 3)
        {
            Debug.Log("检测到胜利对话（3句），Bobby开始淡出");
            StartCoroutine(FadeOutFriend());
        }
        else
        {
            Debug.Log("正常游戏对话结束，UI开始淡入");
            StartCoroutine(FadeInUI());
        }
    }
    
    IEnumerator FadeInUI()
    {
        foreach(GameObject obj in hideObjects)
        {
            if(obj != null)
            {
                obj.SetActive(true);
                CanvasGroup group = obj.GetComponent<CanvasGroup>();
                if(group == null)
                {
                    group = obj.AddComponent<CanvasGroup>();
                }
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
        
        float elapsed = 0f;
        while(elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            
            foreach(GameObject obj in hideObjects)
            {
                if(obj != null)
                {
                    CanvasGroup group = obj.GetComponent<CanvasGroup>();
                    if(group != null)
                    {
                        group.alpha = alpha;
                    }
                }
            }
            
            yield return null;
        }
        
        foreach(GameObject obj in hideObjects)
        {
            if(obj != null)
            {
                CanvasGroup group = obj.GetComponent<CanvasGroup>();
                if(group != null)
                {
                    group.alpha = 1f;
                    group.interactable = true;
                    group.blocksRaycasts = true;
                }
            }
        }
        
        Debug.Log("UI淡入完成，已恢复交互性");
    }
}
