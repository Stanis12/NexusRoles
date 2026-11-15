using System;
using UnityEngine;

public static class SemiFunc
{
    public enum emojiIcon
    {
        drone_heal = 0,
        drone_zero_gravity = 1,
        drone_indestructible = 2,
        drone_feather = 3,
        drone_torque = 4,
        drone_battery = 5,
        orb_heal = 6,
        orb_zero_gravity = 7,
        orb_indestructible = 8,
        orb_feather = 9,
        orb_torque = 10,
        orb_battery = 11,
        orb_magnet = 12,
        grenade_explosive = 13,
        grenade_stun = 14,
        weapon_baseball_bat = 15,
        weapon_sledgehammer = 16,
        weapon_frying_pan = 17,
        weapon_sword = 18,
        weapon_inflatable_hammer = 19,
        item_health_pack_S = 20,
        item_health_pack_M = 21,
        item_health_pack_L = 22,
        item_gun_handgun = 23,
        item_gun_shotgun = 24,
        item_gun_tranq = 25,
        item_valuable_tracker = 26,
        item_extraction_tracker = 27,
        item_grenade_human = 28,
        item_grenade_duct_taped = 29,
        item_rubber_duck = 30,
        item_mine_explosive = 31,
        item_grenade_shockwave = 32,
        item_mine_shockwave = 33,
        item_mine_stun = 34
    }

    public enum itemType
    {
        drone = 0,
        orb = 1,
        cart = 2,
        item_upgrade = 3,
        player_upgrade = 4,
        power_crystal = 5,
        grenade = 6,
        melee = 7,
        healthPack = 8,
        gun = 9,
        tracker = 10,
        mine = 11,
        pocket_cart = 12,
        tool = 13,
        vehicle = 14,
        launcher = 15
    }

    public enum itemVolume
    {
        small = 0,
        medium = 1,
        large = 2,
        large_wide = 3,
        power_crystal = 4,
        large_high = 5,
        upgrade = 6,
        healthPack = 7,
        large_plus = 8,
        vehicle = 9
    }

    public enum itemSecretShopType
    {
        none = 0,
        shop_attic = 1
    }
}

[CreateAssetMenu(menuName = "Nexus Roles/Localized Asset", fileName = "New Localized Asset")]
public class LocalizedAsset : ScriptableObject
{
}

[CreateAssetMenu(menuName = "Nexus Roles/Color Presets", fileName = "New Color Presets")]
public class ColorPresets : ScriptableObject
{
    public Color colorMain;
    public Color colorLight;
    public Color colorDark;
}

[Serializable]
public class PrefabRef : PrefabRef<GameObject>
{
}

[Serializable]
public class PrefabRef<T> where T : UnityEngine.Object
{
#pragma warning disable 0649
    [SerializeField] private string prefabName;
    [SerializeField] private string resourcePath;
    [SerializeField] private AssetBundle bundle;
#pragma warning restore 0649

    public string PrefabName => prefabName;
    public string ResourcePath => resourcePath;
    public AssetBundle Bundle => bundle;
}
