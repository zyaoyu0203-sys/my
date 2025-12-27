using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
/// <summary>
/// 卡牌数据生成工具 - 批量创建40张CardData资源
/// </summary>
public class CardDataGenerator : EditorWindow
{
    private string savePath = "Assets/Scriptables/CardData";
    private bool autoAssignSprites = true;

    [MenuItem("Tools/卡牌系统/生成所有CardData资源")]
    public static void ShowWindow()
    {
        GetWindow<CardDataGenerator>("CardData生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("批量生成40张CardData资源", EditorStyles.boldLabel);
        GUILayout.Space(10);

        savePath = EditorGUILayout.TextField("保存路径:", savePath);
        autoAssignSprites = EditorGUILayout.Toggle("自动匹配Sprite:", autoAssignSprites);

        GUILayout.Space(10);

        if (GUILayout.Button("生成所有CardData (40张)", GUILayout.Height(40)))
        {
            GenerateAllCardData();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "此工具将生成：\n" +
            "- 36张基础牌（红心、方块、梅花、黑桃各1-9）\n" +
            "- 1张黑牌\n" +
            "- 3张金牌\n\n" +
            "如果勾选'自动匹配Sprite'，将尝试从Assets/CardSprites/文件夹中匹配对应的图片。",
            MessageType.Info
        );
    }

    private void GenerateAllCardData()
    {
        // 确保目录存在
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        int generatedCount = 0;
        int id = 0;

        // 1. 生成36张基础牌
        Suit[] suits = { Suit.Heart, Suit.Diamond, Suit.Club, Suit.Spade };
        string[] suitNames = { "Heart", "Diamond", "Club", "Spade" };
        string[] suitNamesCN = { "红心", "方块", "梅花", "黑桃" };

        for (int s = 0; s < suits.Length; s++)
        {
            for (int rank = 1; rank <= 9; rank++)
            {
                CardData card = CreateCardData(
                    id: id,
                    cardType: CardType.Basic,
                    suit: suits[s],
                    rank: rank,
                    fileName: $"{suitNames[s]}_{rank}",
                    cardName: $"{suitNamesCN[s]}{rank}",
                    canBeDiscarded: true,
                    canBePlayed: true,
                    spritePattern: $"{suitNames[s]} ({rank})"
                );

                if (card != null)
                {
                    generatedCount++;
                }

                id++;
            }
        }

        // 2. 生成1张黑牌
        CardData blackCard = CreateCardData(
            id: id,
            cardType: CardType.Black,
            suit: Suit.None,
            rank: 0,
            fileName: "Black_Card",
            cardName: "黑牌",
            canBeDiscarded: false,
            canBePlayed: false,
            spritePattern: "depression"  // 根据您的文件列表
        );

        if (blackCard != null)
        {
            generatedCount++;
        }
        id++;

        // 3. 生成3张金牌
        string[] goldNames = { "hope", "courage", "friendship" };
        string[] goldNamesCN = { "希望金牌", "勇气金牌", "友谊金牌" };

        for (int i = 0; i < 3; i++)
        {
            CardData goldCard = CreateCardData(
                id: id,
                cardType: CardType.Gold,
                suit: Suit.None,
                rank: 0,
                fileName: $"Gold_Card_{i + 1}",
                cardName: goldNamesCN[i],
                canBeDiscarded: true,
                canBePlayed: true,
                spritePattern: goldNames[i]
            );

            if (goldCard != null)
            {
                generatedCount++;
            }
            id++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("生成完成", $"成功生成 {generatedCount} 个CardData资源！\n保存路径: {savePath}", "确定");
        Debug.Log($"CardData生成完成！共生成 {generatedCount} 个资源文件");
    }

    private CardData CreateCardData(
        int id,
        CardType cardType,
        Suit suit,
        int rank,
        string fileName,
        string cardName,
        bool canBeDiscarded,
        bool canBePlayed,
        string spritePattern = ""
    )
    {
        string assetPath = $"{savePath}/{fileName}.asset";

        // 检查是否已存在
        CardData existingCard = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
        if (existingCard != null)
        {
            Debug.LogWarning($"CardData已存在，跳过: {assetPath}");
            return null;
        }

        // 创建新的CardData
        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.id = id;
        card.cardType = cardType;
        card.suit = suit;
        card.rank = rank;
        card.cardName = cardName;
        card.canBeDiscarded = canBeDiscarded;
        card.canBePlayed = canBePlayed;

        // 尝试自动匹配Sprite
        if (autoAssignSprites && !string.IsNullOrEmpty(spritePattern))
        {
            Sprite sprite = FindSpriteByPattern(spritePattern);
            if (sprite != null)
            {
                card.sprite = sprite;
                Debug.Log($"为 {cardName} 自动匹配到Sprite: {sprite.name}");
            }
            else
            {
                Debug.LogWarning($"未找到匹配的Sprite: {spritePattern}");
            }
        }

        // 保存资源
        AssetDatabase.CreateAsset(card, assetPath);
        Debug.Log($"生成CardData: {assetPath}");

        return card;
    }

    private Sprite FindSpriteByPattern(string pattern)
    {
        // 搜索Assets/CardSprites/目录
        string[] guids = AssetDatabase.FindAssets($"{pattern} t:Sprite", new[] { "Assets/CardSprites" });

        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite;
        }

        return null;
    }
}
#endif
