using UnityEngine;
using System.Collections;

public class CardSelectable3D : MonoBehaviour
{
    [SerializeField] private bool isSelected;
    [SerializeField] private bool isHovering;

    [Header("Move")]
    [SerializeField] private Transform visual;
    [SerializeField] private float liftDistance = 0.2f;
    [SerializeField] private float moveSpeed = 12f;

    [Header("Play Card")]
    [SerializeField] private Transform tableArea; // 桌面区域（可选，如果为空则自动创建）
    [SerializeField] private Vector3 tableOffset = new Vector3(0, 0, 2f); // 桌面位置偏移
    [SerializeField] private float playDuration = 0.5f; // 打出动画时长
    [SerializeField] private float playHeight = 0.5f; // 打出时的飞行高度
    private bool isOnTable = false; // 是否已在桌面
    private Transform originalParent; // 原始父对象

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSfx;
    [SerializeField] private AudioClip deselectSfx;

    private Vector3 startPos;
    private Vector3 targetPos;
    private Camera mainCamera;

    void Awake()
    {
        if (visual == null) visual = transform;
        startPos = visual.localPosition;
        targetPos = startPos;
        mainCamera = Camera.main;
        originalParent = transform.parent; // 保存原始父对象

        // 自动检查并创建 CardManager
        if (CardManager.Instance == null)
        {
            GameObject managerObj = new GameObject("CardManager");
            managerObj.AddComponent<CardManager>();
            Debug.Log("CardSelectable: 已自动创建 CardManager 单例！");
        }
    }

    void Update()
    {
        // 檢測滑鼠點擊
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject && !isOnTable) // 已在桌面的卡牌不能再选中
                {
                    SetSelected(!isSelected);
                }
            }
        }

        visual.localPosition = Vector3.Lerp(visual.localPosition, targetPos, Time.deltaTime * moveSpeed);
    }

    public void Toggle()
    {
        SetSelected(!isSelected);
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;

        targetPos = isSelected ? startPos + Vector3.up * liftDistance : startPos;
        PlaySfx(isSelected ? selectSfx : deselectSfx);
        
        // 通知管理器
        if (CardManager.Instance != null)
        {
            if (isSelected)
            {
                CardManager.Instance.AddSelectedCard(this);
            }
            else
            {
                CardManager.Instance.RemoveSelectedCard(this);
            }
        }
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        audioSource.PlayOneShot(clip);
    }

    // 打出卡牌到桌面（公共方法，供按钮调用）
    public void PlayCard()
    {
        Debug.Log($"PlayCard 被调用！卡牌: {gameObject.name}, isSelected: {isSelected}, isOnTable: {isOnTable}");
        
        if (!isSelected || isOnTable)
        {
            Debug.Log("卡牌未选中或已在桌面，无法打出");
            return; // 必须先选中，且未在桌面
        }
        
        Debug.Log("开始打出卡牌！");
        isSelected = false;
        isOnTable = true;
        
        // 如果没有指定桌面区域，自动创建一个
        if (tableArea == null)
        {
            GameObject tableObj = GameObject.Find("TableArea");
            if (tableObj == null)
            {
                tableObj = new GameObject("TableArea");
            }
            tableArea = tableObj.transform;
        }
        
        // 计算桌面目标位置
        Vector3 tablePosition = transform.position + tableOffset;
        
        // 开始移动到桌面
        StartCoroutine(MoveToTable(tablePosition));
        
        // 播放音效
        PlaySfx(selectSfx);
    }

    // 移动到桌面的协程（带抛物线效果）
    private IEnumerator MoveToTable(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < playDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / playDuration;
            
            // 基础线性移动
            Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, t);
            
            // 添加抛物线高度效果
            float height = Mathf.Sin(t * Mathf.PI) * playHeight;
            currentPos.y += height;
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        // 抛物线动画结束后，直接让卡牌消失
        gameObject.SetActive(false);
        
        // 或者如果想完全删除，可以用这行代替上面那行：
        // Destroy(gameObject);
    }
}
