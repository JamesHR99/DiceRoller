using UnityEngine;

public static class CombatOrderHelper
{
    public static bool PlayerGoesFirst(DiceActionType playerAction, DiceActionType enemyAction)
    {
        int playerAgility = GetPlayerAgilityForAction(playerAction);
        int enemyAgility = GetEnemyAgilityForAction(enemyAction);

        Debug.Log($"Combat Order - Player: {playerAgility} vs Enemy: {enemyAgility}");

        return playerAgility >= enemyAgility;
    }

    public static int GetPlayerAgilityForAction(DiceActionType actionType)
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogWarning("PlayerManager.Instance is null!");
            return 0;
        }

        return PlayerManager.Instance.CalculateAgilityForAction(actionType);
    }

    public static int GetEnemyAgilityForAction(DiceActionType actionType)
    {
        if (EnemyManager.Instance == null)
        {
            Debug.LogWarning("EnemyManager.Instance is null!");
            return 0;
        }

        return EnemyManager.Instance.CalculateAgilityForAction(actionType);
    }

    public static string GetAgilityBreakdown(bool isPlayer, DiceActionType actionType)
    {
        if (isPlayer)
        {
            if (PlayerManager.Instance == null || PlayerEquipment.Instance == null)
                return "Player data unavailable";

            int baseAgility = 10;
            int weaponBonus = 0;
            int armorBonus = 0;
            int actionBonus = 0;

            if (PlayerEquipment.Instance.selectedCharacterClass != null)
                baseAgility = PlayerEquipment.Instance.selectedCharacterClass.baseAgility;

            EquipmentItemSO weapon = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Weapon);
            EquipmentItemSO armor = PlayerEquipment.Instance.GetEquippedItem(EquipmentSlot.Armor);

            if (weapon != null) weaponBonus = weapon.agilityBonus;
            if (armor != null) armorBonus = armor.agilityBonus;

            if (SlotMachine.Instance != null && SlotMachine.Instance.allActionConfigs != null)
            {
                foreach (var config in SlotMachine.Instance.allActionConfigs)
                {
                    if (config.actionType == actionType)
                    {
                        actionBonus = config.agility;
                        break;
                    }
                }
            }

            int total = baseAgility + weaponBonus + armorBonus + actionBonus;
            return $"Player ({total}): Base {baseAgility} + Weapon {weaponBonus:+#;-#;0} + Armor {armorBonus:+#;-#;0} + {actionType} {actionBonus:+#;-#;0}";
        }
        else
        {
            if (EnemyManager.Instance == null)
                return "Enemy data unavailable";

            int baseAgility = EnemyManager.Instance.baseAgility;
            int weaponBonus = 0;
            int armorBonus = 0;
            int actionBonus = 0;

            if (EnemyManager.Instance.weapon != null) weaponBonus = EnemyManager.Instance.weapon.agilityBonus;
            if (EnemyManager.Instance.armor != null) armorBonus = EnemyManager.Instance.armor.agilityBonus;

            if (SlotMachine.Instance != null && SlotMachine.Instance.allActionConfigs != null)
            {
                foreach (var config in SlotMachine.Instance.allActionConfigs)
                {
                    if (config.actionType == actionType)
                    {
                        actionBonus = config.agility;
                        break;
                    }
                }
            }

            int total = baseAgility + weaponBonus + armorBonus + actionBonus;
            return $"Enemy ({total}): Base {baseAgility} + Weapon {weaponBonus:+#;-#;0} + Armor {armorBonus:+#;-#;0} + {actionType} {actionBonus:+#;-#;0}";
        }
    }
}
