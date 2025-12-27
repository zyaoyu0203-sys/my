
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private VisualCardsHandler visualHandler;
    private Vector3 offset;

    [Header("Card Data")]
    public CardData cardData;                   // 卡牌数据引用
    [SerializeField] private Image cardImage;   // 卡面图片组件（用于显示卡牌sprite）

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab;
    [HideInInspector] public CardVisual cardVisual;

    [Header("States")]
    public bool isHovering;
    public bool isDragging;
    [HideInInspector] public bool wasDragged;

    [Header("Events")]
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;
    // Unity事件

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        if (!instantiateVisual)
            return;

        visualHandler = FindObjectOfType<VisualCardsHandler>();
        cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this);
    }

    void Update()
    {
        ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;

        selected = !selected;// true->false

        SelectEvent.Invoke(this, selected);

        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;

        // 通知 CardManager 选中状态变化
        if (CardManager.Instance != null)
        {
            CardManager.Instance.RegisterCardSelection(this, selected);
        }
    }

    public void Deselect()
    {
        if (!selected)
            return;

        // 保存之前的状态
        bool wasSelected = selected;
        selected = false;

        // 重置位置
        transform.localPosition = Vector3.zero;

        // 触发事件
        SelectEvent.Invoke(this, selected);

        // 通知 CardManager
        if (CardManager.Instance != null)
        {
            CardManager.Instance.RegisterCardSelection(this, false);
        }
    }


    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    /// <summary>
    /// 设置卡牌数据并更新视觉效果
    /// </summary>
    public void SetCardData(CardData data)
    {
        cardData = data;
        
        // 更新CardVisual的图片（主要显示组件）
        if (cardVisual != null && cardVisual.cardImage != null && data != null && data.sprite != null)
        {
            cardVisual.cardImage.sprite = data.sprite;
            cardVisual.cardImage.enabled = true;
            Debug.Log($"CardVisual图片已更新: {data.cardName}");
        }
        else if (cardVisual == null)
        {
            Debug.LogWarning($"CardVisual为空！无法显示卡牌图片");
        }
        else if (cardVisual.cardImage == null)
        {
            Debug.LogError($"CardVisual.cardImage为空！请在CardVisual Prefab中设置cardImage字段");
        }

        // 如果Card本身有cardImage组件，也更新它（作为备用）
        if (cardImage != null && data != null && data.sprite != null)
        {
            cardImage.sprite = data.sprite;
            cardImage.enabled = true;
        }

        Debug.Log($"卡牌数据已设置: {(data != null ? data.cardName : "null")}");
    }

    /// <summary>
    /// 清空卡牌数据
    /// </summary>
    public void ClearCard()
    {
        cardData = null;
        
        if (cardImage != null)
        {
            cardImage.enabled = false;
        }

        // 同时清空CardVisual的图片
        if (cardVisual != null && cardVisual.cardImage != null)
        {
            cardVisual.cardImage.enabled = false;
        }

        // 取消选中状态
        if (selected)
        {
            Deselect();
        }

        Debug.Log("卡牌数据已清空");
    }

    /// <summary>
    /// 检查卡牌是否为空（无数据）
    /// </summary>
    public bool IsEmpty()
    {
        return cardData == null;
    }

    private void OnDestroy()
    {
        if(cardVisual != null)
        Destroy(cardVisual.gameObject);
    }
}
