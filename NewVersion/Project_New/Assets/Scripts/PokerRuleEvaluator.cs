using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerRuleEvaluator : MonoBehaviour
{
    public enum HandRank
    {
        Invalid = 0,
        HighCard = 1,
        OnePair = 2,
        TwoPair = 3,
        ThreeOfKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfKind = 8,
        StraightFlush = 9
    }

    /// <summary>
    /// 判断选中的卡牌是否能打出
    /// </summary>
    public static bool CanPlayCards(List<Card> cards)
    {
        if (cards == null || cards.Count < 2 || cards.Count > 5)
        {
            Debug.Log("卡牌数量不符合要求（必须2-5张）");
            return false;
        }

        // 过滤掉空卡或无数据的卡
        var validCards = cards.Where(c => c != null && c.cardData != null).ToList();
        if (validCards.Count != cards.Count)
        {
            Debug.LogWarning("存在无效卡牌");
            return false;
        }

        // 检查是否包含黑牌（黑牌无法打出）
        foreach (var card in validCards)
        {
            if (card.cardData.cardType == CardType.Black)
            {
                Debug.Log("包含黑牌，无法打出");
                return false;
            }
        }

        // 检查金牌胜利条件（必须恰好3张金牌）
        if (validCards.Count == 3 && validCards.All(c => c.cardData.cardType == CardType.Gold))
        {
            Debug.Log("金牌胜利条件达成！");
            return true;
        }

        // 根据牌数判定德扑规则
        HandRank rank = EvaluateHand(validCards);
        bool canPlay = rank != HandRank.Invalid;

        if (canPlay)
        {
            Debug.Log($"有效牌型: {rank}");
        }
        else
        {
            Debug.Log("无效牌型");
        }

        return canPlay;
    }

    /// <summary>
    /// 评估手牌牌型
    /// </summary>
    private static HandRank EvaluateHand(List<Card> cards)
    {
        int count = cards.Count;

        switch (count)
        {
            case 2:
                return EvaluateTwoCards(cards);
            case 3:
                return EvaluateThreeCards(cards);
            case 4:
                return EvaluateFourCards(cards);
            case 5:
                return EvaluateFiveCards(cards);
            default:
                return HandRank.Invalid;
        }
    }

    /// <summary>
    /// 评估2张牌：只能是对子
    /// </summary>
    private static HandRank EvaluateTwoCards(List<Card> cards)
    {
        if (cards[0].cardData.rank == cards[1].cardData.rank)
        {
            return HandRank.OnePair;
        }
        return HandRank.Invalid;
    }

    /// <summary>
    /// 评估3张牌：三条、顺子
    /// </summary>
    private static HandRank EvaluateThreeCards(List<Card> cards)
    {
        // 检查三条
        if (IsThreeOfKind(cards))
        {
            return HandRank.ThreeOfKind;
        }

        // 检查顺子
        if (IsStraight(cards))
        {
            return HandRank.Straight;
        }

        return HandRank.Invalid;
    }

    /// <summary>
    /// 评估4张牌：四条、顺子
    /// </summary>
    private static HandRank EvaluateFourCards(List<Card> cards)
    {
        // 检查四条
        if (IsFourOfKind(cards))
        {
            return HandRank.FourOfKind;
        }

        // 检查顺子
        if (IsStraight(cards))
        {
            return HandRank.Straight;
        }

        return HandRank.Invalid;
    }

    /// <summary>
    /// 评估5张牌：所有标准德扑牌型
    /// </summary>
    private static HandRank EvaluateFiveCards(List<Card> cards)
    {
        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(cards);

        // 同花顺
        if (isFlush && isStraight)
        {
            return HandRank.StraightFlush;
        }

        // 四条
        if (IsFourOfKind(cards))
        {
            return HandRank.FourOfKind;
        }

        // 葫芦
        if (IsFullHouse(cards))
        {
            return HandRank.FullHouse;
        }

        // 同花
        if (isFlush)
        {
            return HandRank.Flush;
        }

        // 顺子
        if (isStraight)
        {
            return HandRank.Straight;
        }

        // 三条
        if (IsThreeOfKind(cards))
        {
            return HandRank.ThreeOfKind;
        }

        // 两对
        if (IsTwoPair(cards))
        {
            return HandRank.TwoPair;
        }

        // 对子
        if (IsOnePair(cards))
        {
            return HandRank.OnePair;
        }

        // 单张（5张牌总是可以打出的，即使是杂牌）
        return HandRank.HighCard;
    }

    #region 牌型判定辅助方法

    /// <summary>
    /// 检查是否为顺子（连续的点数）
    /// </summary>
    private static bool IsStraight(List<Card> cards)
    {
        // 只检查基础卡牌（金牌和黑牌rank为0，不参与顺子判定）
        var basicCards = cards.Where(c => c.cardData.cardType == CardType.Basic).ToList();
        if (basicCards.Count != cards.Count)
            return false;

        var ranks = basicCards.Select(c => c.cardData.rank).OrderBy(r => r).ToList();

        // 检查是否连续
        for (int i = 0; i < ranks.Count - 1; i++)
        {
            if (ranks[i + 1] != ranks[i] + 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查是否为同花（相同花色）
    /// </summary>
    private static bool IsFlush(List<Card> cards)
    {
        // 只检查基础卡牌
        var basicCards = cards.Where(c => c.cardData.cardType == CardType.Basic).ToList();
        if (basicCards.Count != cards.Count)
            return false;

        Suit firstSuit = basicCards[0].cardData.suit;
        return basicCards.All(c => c.cardData.suit == firstSuit);
    }

    /// <summary>
    /// 检查是否为对子
    /// </summary>
    private static bool IsOnePair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.cardData.rank).ToList();
        return rankGroups.Any(g => g.Count() == 2);
    }

    /// <summary>
    /// 检查是否为两对
    /// </summary>
    private static bool IsTwoPair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.cardData.rank).Where(g => g.Count() == 2).ToList();
        return rankGroups.Count == 2;
    }

    /// <summary>
    /// 检查是否为三条
    /// </summary>
    private static bool IsThreeOfKind(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.cardData.rank).ToList();
        return rankGroups.Any(g => g.Count() == 3);
    }

    /// <summary>
    /// 检查是否为四条
    /// </summary>
    private static bool IsFourOfKind(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.cardData.rank).ToList();
        return rankGroups.Any(g => g.Count() == 4);
    }

    /// <summary>
    /// 检查是否为葫芦（三条+对子）
    /// </summary>
    private static bool IsFullHouse(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c.cardData.rank).ToList();
        bool hasThree = rankGroups.Any(g => g.Count() == 3);
        bool hasPair = rankGroups.Any(g => g.Count() == 2);
        return hasThree && hasPair;
    }

    #endregion
}
