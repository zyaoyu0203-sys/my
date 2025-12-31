using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private VisualCardsHandler visualHandler;
    private Vector3 offset;

    [Header("Card Data")]
    public CardData cardData;
    [SerializeField] private Image cardImage;

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

        selected = !selected;

        SelectEvent.Invoke(this, selected);

        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;

        if (CardManager.Instance != null)
        {
            CardManager.Instance.RegisterCardSelection(this, selected);
        }
    }

    public void Deselect()
    {
        if (!selected)
            return;

        bool wasSelected = selected;
        selected = false;

        transform.localPosition = Vector3.zero;

        SelectEvent.Invoke(this, selected);

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
        // === 第一步：强制重置所有视觉状态 ===
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        
        // 杀掉所有DOTween动画
        DOTween.Kill(transform);
        if (cardVisual != null)
        {
            DOTween.Kill(cardVisual.transform);
            if (cardVisual.cardImage != null)
            {
                DOTween.Kill(cardVisual.cardImage);
            }
        }
        
        // 重置cardVisual的Image颜色（最重要！）
        if (cardVisual != null && cardVisual.cardImage != null)
        {
            cardVisual.cardImage.color = new Color(1f, 1f, 1f, 1f);  // 纯白，完全不透明
            cardVisual.cardImage.enabled = true;
        }
        
        // 重置Card自身的Image颜色
        if (cardImage != null)
        {
            cardImage.color = new Color(1f, 1f, 1f, 1f);
            cardImage.enabled = true;
        }
        
        // === 第二步：设置新数据 ===
        cardData = data;
        
        // 更新CardVisual的图片（主要显示组件）
        if (cardVisual != null && cardVisual.cardImage != null && data != null && data.sprite != null)
        {
            cardVisual.cardImage.sprite = data.sprite;
            cardVisual.cardImage.enabled = true;
            
            // 再次确保颜色正确
            cardVisual.cardImage.color = new Color(1f, 1f, 1f, 1f);
            
            // 应用shader效果
            ApplyShaderEdition(data.shaderEdition);
            
            Debug.Log($"CardVisual图片已更新: {data.cardName}，颜色已重置为白色，shader: {data.shaderEdition}");
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
            cardImage.color = new Color(1f, 1f, 1f, 1f);
        }

        Debug.Log($"卡牌数据已设置: {(data != null ? data.cardName : "null")}");
    }
    
    /// <summary>
    /// 应用shader效果到卡牌
    /// </summary>
    private void ApplyShaderEdition(ShaderEdition edition)
    {
        if (cardVisual == null || cardVisual.cardImage == null)
        {
            Debug.LogWarning("无法应用shader：CardVisual或cardImage为空");
            return;
        }
        
        Image image = cardVisual.cardImage;
        
        Material m = new Material(image.material);
        image.material = m;
        
        for (int i = 0; i < image.material.enabledKeywords.Length; i++)
        {
            image.material.DisableKeyword(image.material.enabledKeywords[i]);
        }
        
        string keywordName = "_EDITION_" + edition.ToString().ToUpper();
        image.material.EnableKeyword(keywordName);
        
        Debug.Log($"已应用shader效果: {keywordName}");
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

        if (cardVisual != null && cardVisual.cardImage != null)
        {
            cardVisual.cardImage.enabled = false;
        }

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