using UnityEngine;

[CreateAssetMenu(menuName = "Nexus Roles/Item", fileName = "New Item")]
public class Item : ScriptableObject
{
    public bool disabled;
    public string itemName;
    public LocalizedAsset itemNameLocalized;
    public string description;
    public SemiFunc.itemType itemType;
    public SemiFunc.emojiIcon emojiIcon;
    public SemiFunc.itemVolume itemVolume;
    public SemiFunc.itemSecretShopType itemSecretShopType;
    public ColorPresets colorPreset;
    public PrefabRef prefab;
    public Value value;
    public int maxAmount;
    public int maxAmountInShop;
    public int minPlayerCount;
    public bool maxPurchase;
    public int maxPurchaseAmount;
    public Quaternion spawnRotationOffset;
    public bool physicalItem;
}
