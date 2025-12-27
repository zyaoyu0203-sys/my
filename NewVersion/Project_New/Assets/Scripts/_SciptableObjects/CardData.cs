using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Card Identity")]
    public int id;                          // 0-39 唯一编号
    public CardType cardType;               // Basic/Black/Gold
    public Suit suit;                       // Heart/Diamond/Club/Spade/None
    public int rank;                        // 1-9（黑牌金牌为0）

    [Header("Visual")]
    public Sprite sprite;                   // 卡面图

    [Header("Rules")]
    public bool canBeDiscarded = true;      // 黑牌为false
    public bool canBePlayed = true;         // 黑牌为false

    [Header("Display Name")]
    public string cardName;                 // 用于调试显示
}

public enum CardType 
{ 
    Basic = 0,      // 基础卡牌
    Black = 1,      // 黑牌
    Gold = 2        // 金牌
}

public enum Suit 
{ 
    Heart = 0,      // 红心
    Diamond = 1,    // 方块
    Club = 2,       // 梅花
    Spade = 3,      // 黑桃
    None = 4        // 无花色（黑牌、金牌）
}
