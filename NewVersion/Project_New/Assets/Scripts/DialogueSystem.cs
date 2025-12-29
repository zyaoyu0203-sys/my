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
    private bool isDialogueActive = false;  // 对话是否正在进行中
    private bool hasDialogueStarted = false; // 对话是否已经开始过
    
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
        
        // 使用CanvasGroup隐藏其他UI（保持对象激活，避免干扰卡牌系统初始化）
        foreach(GameObject obj in hideObjects)
        {
            if(obj != null)
            {
                // 确保对象是激活的
                obj.SetActive(true);
                
                // 使用CanvasGroup控制可见性和交互
                CanvasGroup group = obj.GetComponent<CanvasGroup>();
                if(group == null)
                {
                    group = obj.AddComponent<CanvasGroup>();
                }
                group.alpha = 0f;  // 完全透明
                group.interactable = false;  // 不可交互
                group.blocksRaycasts = false;  // 不阻挡射线
                
                Debug.Log($"已使用CanvasGroup隐藏UI: {obj.name}");
            }
        }
        
        // 等待卡牌系统初始化完成后再开始对话
        StartCoroutine(WaitForCardSystemThenStart());
    }
    
    IEnumerator WaitForCardSystemThenStart()
    {
        // 等待一帧，确保所有Start方法都执行完毕
        yield return null;
        
        // 再等待设定的时间，确保卡牌发放完成
        yield return new WaitForSeconds(cardSystemInitDelay);
        
        Debug.Log($"等待{cardSystemInitDelay}秒后，卡牌系统初始化完成，开始播放睁眼动画和对话");
        
        // 开始睁眼动画和对话
        StartCoroutine(StartWithWakeUp());
    }
    
    IEnumerator StartWithWakeUp()
    {
        // 播放睁眼+抬头动画
        if(cameraShake != null)
        {
            cameraShake.WakeUpSequence();
        }
        
        // 等待动画完成
        yield return new WaitForSeconds(3.5f);
        
        // 开始对话
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
    
    void Update()
    {
        // 只有在对话激活状态下才响应点击
        if(isDialogueActive && Input.GetMouseButtonDown(0))
        {
            if(isTyping)
            {
                // 只停止打字协程
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
        
        // 测试按键
        if(Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("测试摄像机抖动！");
            if(cameraShake != null)
            {
                cameraShake.Shake();
            }
        }
    }
    
    void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        isDialogueActive = true;  // 标记对话已激活
        hasDialogueStarted = true;
        currentLine = 0;
        Debug.Log("对话开始，已激活对话输入响应");
        ShowLine();
    }
    
    void ShowLine()
    {
        if(currentLine >= dialogues.Count)
        {
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
        Debug.Log("起始透明度: " + friendMaterial.color.a);
        
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
            
            if(Mathf.FloorToInt(elapsed * 2) != Mathf.FloorToInt((elapsed - Time.deltaTime) * 2))
            {
                Debug.Log("淡入进度: " + Mathf.RoundToInt(t * 100) + "%, Alpha: " + alpha);
            }
            
            yield return null;
        }
        
        Color finalColor = friendMaterial.color;
        finalColor.a = 1.0f;
        friendMaterial.color = finalColor;
        
        Debug.Log("=== 淡入完成 ===");
        Debug.Log("最终透明度: " + friendMaterial.color.a);
    }
    
    void NextLine()
    {
        currentLine++;
        ShowLine();
    }
    
    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;  // 取消对话激活状态，停止响应点击输入
        Debug.Log("对话结束！已停用对话输入响应");
        StartCoroutine(FadeInUI());
    }
    
    IEnumerator FadeInUI()
    {
        // 确保所有对象已激活并准备CanvasGroup
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
                // 暂时保持不可交互，直到淡入完成
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }
        
        // 淡入动画
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
        
        // 淡入完成后，恢复UI的完全可交互状态
        foreach(GameObject obj in hideObjects)
        {
            if(obj != null)
            {
                CanvasGroup group = obj.GetComponent<CanvasGroup>();
                if(group != null)
                {
                    group.alpha = 1f;
                    group.interactable = true;  // 恢复交互
                    group.blocksRaycasts = true;  // 恢复射线检测
                }
            }
        }
        
        Debug.Log("UI淡入完成，已恢复交互性");
    }
}
