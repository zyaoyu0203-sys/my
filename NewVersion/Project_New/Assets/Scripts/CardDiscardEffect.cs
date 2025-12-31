using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 独立的弃牌效果管理器
/// 负责原地缩小消失效果
/// </summary>
public class CardDiscardEffect : MonoBehaviour
{
    public static CardDiscardEffect Instance { get; private set; }

    [Header("Discard Effect")]
    [Tooltip("缩小消失的时间（秒）")]
    public float shrinkDuration = 0.5f;

    [Tooltip("是否启用旋转效果")]
    public bool enableRotation = true;

    [Tooltip("旋转角度")]
    public float rotationAmount = 360f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        Debug.Log("CardDiscardEffect初始化成功！");
    }

    /// <summary>
    /// 播放弃牌效果
    /// </summary>
    /// <param name="cards">要弃掉的卡牌列表</param>
    /// <param name="onComplete">完成后的回调</param>
    public void PlayDiscardEffect(List<Card> cards, System.Action onComplete = null)
    {
        StartCoroutine(DiscardSequence(cards, onComplete));
    }

    private IEnumerator DiscardSequence(List<Card> cards, System.Action onComplete)
    {
        Debug.Log($"=== 开始弃牌效果，卡牌数量: {cards.Count} ===");

        // 同时缩小所有卡牌
        foreach (var card in cards)
        {
            if (card != null)
            {
                StartCoroutine(ShrinkCard(card));
            }
        }

        // 等待缩小完成
        yield return new WaitForSeconds(shrinkDuration);

        // 重置所有卡牌的视觉状态
        foreach (var card in cards)
        {
            if (card != null)
            {
                ResetCardVisuals(card);
            }
        }

        Debug.Log("=== 弃牌效果完成 ===");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 缩小单张卡牌
    /// </summary>
    private IEnumerator ShrinkCard(Card card)
    {
        if (card == null || card.cardVisual == null)
        {
            Debug.LogWarning("卡牌或cardVisual是null");
            yield break;
        }

        Transform cardTransform = card.transform;
        Image cardImage = card.cardVisual.cardImage;

        if (cardImage == null)
        {
            Debug.LogWarning("cardImage是null");
            yield break;
        }

        Debug.Log($"开始缩小卡牌: {card.name}");

        // 杀掉现有动画
        DOTween.Kill(cardTransform);

        // 缩小动画
        cardTransform.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack);

        // 旋转动画（可选）
        if (enableRotation)
        {
            cardTransform.DORotate(new Vector3(0, 0, rotationAmount), shrinkDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad);
        }

        // 淡出动画
        if (cardImage != null)
        {
            cardImage.DOFade(0f, shrinkDuration).SetEase(Ease.InQuad);
        }

        yield return new WaitForSeconds(shrinkDuration);

        Debug.Log($"卡牌 {card.name} 缩小完成");
    }

    /// <summary>
    /// 重置卡牌视觉状态
    /// </summary>
    private void ResetCardVisuals(Card card)
    {
        if (card == null) return;

        Debug.Log($"重置卡牌视觉: {card.name}");

        // 重置Transform
        card.transform.localScale = Vector3.one;
        card.transform.rotation = Quaternion.identity;

        // 重置Image颜色
        if (card.cardVisual != null && card.cardVisual.cardImage != null)
        {
            card.cardVisual.cardImage.color = Color.white;
        }
    }
}
