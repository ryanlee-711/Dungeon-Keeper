using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simplified player resource manager
/// - Mana regenerates over time
/// - Spend mana to: swap rooms, respawn monsters, buy upgrades
/// - Upgrades: Max Mana, Mana Regen, Monster Power
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Mana System")]
    [SerializeField] private int maxMana = 100;
    [SerializeField] private int currentMana = 100;
    [SerializeField] private float manaRegenRate = 5f; // Mana per second

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
    [SerializeField] private float monsterPowerIncreasePerLevel = 0.2f; // 20% increase per level

    [Header("Events")]
    public UnityEvent<int, int> OnManaChanged; // (current, max)
    public UnityEvent<string> OnNotEnoughMana; // Error message

    // Upgrade levels
    private int maxManaUpgradeLevel = 0;
    private int manaRegenUpgradeLevel = 0;
    private int monsterPowerUpgradeLevel = 0;

    public int CurrentMana => currentMana;
    public int MaxMana => maxMana;
    public float ManaRegenRate => manaRegenRate;

    public int MaxManaUpgradeLevel => maxManaUpgradeLevel;
    public int ManaRegenUpgradeLevel => manaRegenUpgradeLevel;
    public int MonsterPowerUpgradeLevel => monsterPowerUpgradeLevel;

    private float manaRegenBuffer = 0f;

    void Awake()
    {
        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (OnManaChanged == null) OnManaChanged = new UnityEvent<int, int>();
            if (OnNotEnoughMana == null) OnNotEnoughMana = new UnityEvent<string>();
        }

    }

    void Start()
    {
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    void Update()
    {
        // Regenerate mana
        if (currentMana < maxMana)
        {
            // float regenAmount = manaRegenRate * Time.deltaTime;
            // currentMana = Mathf.Min(currentMana + Mathf.FloorToInt(regenAmount), maxMana);
            // OnManaChanged?.Invoke(currentMana, maxMana);
            manaRegenBuffer += manaRegenRate * Time.deltaTime;
            int add = Mathf.FloorToInt(manaRegenBuffer);
            if (add > 0)
            {
                manaRegenBuffer -= add;
                currentMana = Mathf.Min(currentMana + add, maxMana);
                OnManaChanged?.Invoke(currentMana, maxMana);
            }
        }
    }

    #region Mana Management

    private bool SpendMana(int amount, string actionName = "action")
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }

        OnNotEnoughMana?.Invoke($"Not enough mana for {actionName}! Need {amount}, have {currentMana}");
        return false;
    }

    public void AddMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    #endregion

    #region Actions

    public bool CanSwapRoom()
    {
        return currentMana >= swapRoomCost;
    }

    public bool TrySpendMana(int amount, string actionName = "action")
    {
        return SpendMana(amount, actionName);
    }

    public bool TrySwapRoom()
    {
        if (SpendMana(swapRoomCost, "swap room"))
        {
            Debug.Log($"Room swapped! Mana: {currentMana}/{maxMana}");
            return true;
        }
        return false;
    }

    public bool CanRespawnMonster(Monster monster)
    {
        int cost = GetRespawnCost(monster);
        return currentMana >= cost;
    }

    public bool TryRespawnMonster(Monster monster)
    {
        int cost = GetRespawnCost(monster);

        if (SpendMana(cost, "respawn monster"))
        {
            Debug.Log($"Monster respawned! Mana: {currentMana}/{maxMana}");
            return true;
        }
        return false;
    }

    public int GetRespawnCost(Monster monster)
    {
        if (monster == null) return respawnMonsterBaseCost;

        // Cost scales with monster strength
        float strengthFactor = (monster.Health / 50f) + (monster.AttackPower / 20f);
        return Mathf.RoundToInt(respawnMonsterBaseCost * strengthFactor);
    }

    #endregion

    #region Upgrades

    // Max Mana Upgrade
    public int GetMaxManaUpgradeCost()
    {
        return Mathf.RoundToInt(maxManaUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, maxManaUpgradeLevel));
    }

    public bool CanUpgradeMaxMana()
    {
        int cost = GetMaxManaUpgradeCost();
        return currentMana >= cost;
    }

    public bool TryUpgradeMaxMana()
    {
        int cost = GetMaxManaUpgradeCost();

        if (SpendMana(cost, "upgrade max mana"))
        {
            maxManaUpgradeLevel++;
            maxMana += maxManaIncreasePerLevel;

            Debug.Log($"Max mana upgraded to level {maxManaUpgradeLevel}! New max: {maxMana}");
            OnManaChanged?.Invoke(currentMana, maxMana);
            return true;
        }
        return false;
    }

    // Mana Regen Upgrade
    public int GetManaRegenUpgradeCost()
    {
        return Mathf.RoundToInt(manaRegenUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, manaRegenUpgradeLevel));
    }

    public bool CanUpgradeManaRegen()
    {
        int cost = GetManaRegenUpgradeCost();
        return currentMana >= cost;
    }

    public bool TryUpgradeManaRegen()
    {
        int cost = GetManaRegenUpgradeCost();

        if (SpendMana(cost, "upgrade mana regen"))
        {
            manaRegenUpgradeLevel++;
            manaRegenRate += manaRegenIncreasePerLevel;

            Debug.Log($"Mana regen upgraded to level {manaRegenUpgradeLevel}! New rate: {manaRegenRate}/sec");
            return true;
        }
        return false;
    }

    // Monster Power Upgrade
    public int GetMonsterPowerUpgradeCost()
    {
        return Mathf.RoundToInt(monsterPowerUpgradeBaseCost * Mathf.Pow(upgradeCostMultiplier, monsterPowerUpgradeLevel));
    }

    public bool CanUpgradeMonsterPower()
    {
        int cost = GetMonsterPowerUpgradeCost();
        return currentMana >= cost;
    }

    public bool TryUpgradeMonsterPower()
    {
        int cost = GetMonsterPowerUpgradeCost();

        if (SpendMana(cost, "upgrade monster power"))
        {
            monsterPowerUpgradeLevel++;

            Debug.Log($"Monster power upgraded to level {monsterPowerUpgradeLevel}! Bonus: +{GetMonsterPowerMultiplier() * 100 - 100}%");
            return true;
        }
        return false;
    }

    public float GetMonsterPowerMultiplier()
    {
        return 1f + (monsterPowerUpgradeLevel * monsterPowerIncreasePerLevel);
    }

    #endregion

    #region Helpers

    public void RefillMana()
    {
        currentMana = maxMana;
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    #endregion
}