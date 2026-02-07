using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Upgrades : MonoBehaviour
{
    [SerializeField] private RoomManipulationSystem roomManipulator2;
    
    [Header("Action Costs")]
    [SerializeField] private int swapRoomCost = 10;
    [SerializeField] private int respawnMonsterBaseCost = 30; // Modified by monster strength

    [Header("Upgrade Costs")]
    [SerializeField] private int maxManaUpgradeBaseCost = 50;
    [SerializeField] private int manaRegenUpgradeBaseCost = 60;
    [SerializeField] private int monsterPowerUpgradeBaseCost = 75;
    [SerializeField] private float upgradeCostMultiplier = 1.5f; // Cost increases by 50% each level

    [Header("Upgrade Values")]
    [SerializeField] private int maxManaIncreasePerLevel = 20;
    [SerializeField] private float manaRegenIncreasePerLevel = 2f;

    private int maxManaUpgradeLevel = 0;
    private int manaRegenUpgradeLevel = 0;
    private int monsterPowerUpgradeLevel = 0;

    public int GetManaRegenUpgradeCost()
    {
        return Mathf.RoundToInt(manaRegenUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, manaRegenUpgradeLevel));
    }

    public bool CanUpgradeManaRegen()
    {
        int cost = GetManaRegenUpgradeCost();
        return roomManipulator2.currentMana >= cost;
    }

    public int GetMaxManaUpgradeCost()
    {
        return Mathf.RoundToInt(maxManaUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, maxManaUpgradeLevel));
    }

    public bool CanUpgradeMaxMana()
    {
        int cost = GetMaxManaUpgradeCost();
        return roomManipulator2.currentMana >= cost;
    }

    public int GetMonsterPowerUpgradeCost()
    {
        return Mathf.RoundToInt(monsterPowerUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, monsterPowerUpgradeLevel));
    }

    public bool CanUpgradeMonsterPower()
    {
        int cost = GetMonsterPowerUpgradeCost();
        return roomManipulator2.currentMana >= cost;
    }
    void Update()
    {
        if (roomManipulator2 == null) return;

        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (CanUpgradeManaRegen())
            {
                int cost = GetManaRegenUpgradeCost();

                roomManipulator2.currentMana -= cost;
                manaRegenUpgradeLevel++;
                roomManipulator2.manaRegenPerSecond += manaRegenIncreasePerLevel;
            }
        }

        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (CanUpgradeMaxMana())
            {
                int cost = GetMaxManaUpgradeCost();
                roomManipulator2.currentMana -= cost;
                maxManaUpgradeLevel++;
                roomManipulator2.maxMana += maxManaIncreasePerLevel;
            }
        }

        if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            if (CanUpgradeMonsterPower())
            {
                int cost = GetMonsterPowerUpgradeCost();
                roomManipulator2.currentMana -= cost;
                monsterPowerUpgradeLevel++;
            }
        }
    }
}

