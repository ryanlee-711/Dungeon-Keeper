using UnityEngine;

/// <summary>
/// Manages AI difficulty presets and allows dynamic difficulty adjustment
/// </summary>
public class AIDifficultyManager : MonoBehaviour
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    [SerializeField] private AdventurerAI adventurerAI;
    [SerializeField] private DifficultyLevel startingDifficulty = DifficultyLevel.Medium;

    void Start()
    {
        if (adventurerAI == null)
        {
            adventurerAI = AdventurerAI.Instance;
        }

        ApplyDifficulty(startingDifficulty);
    }

    public void ApplyDifficulty(DifficultyLevel level)
    {
        if (adventurerAI == null) return;

        switch (level)
        {
            case DifficultyLevel.Easy:
                ApplyEasySettings();
                break;

            case DifficultyLevel.Medium:
                ApplyMediumSettings();
                break;

            case DifficultyLevel.Hard:
                ApplyHardSettings();
                break;

            case DifficultyLevel.Expert:
                ApplyExpertSettings();
                break;
        }

        Debug.Log($"AI Difficulty set to: {level}");
    }

    private void ApplyEasySettings()
    {
        // AI is very exploitable
        SetAIField("replanningChance", 0.4f);        // Often doesn't notice room swaps
        SetAIField("pathCommitmentTurns", 5);        // Commits to path for 5 turns
        SetAIField("desperationHealthThreshold", 50); // Gets desperate early
        SetAIField("monsterDangerBias", 10f);        // Overestimates danger
        SetAIField("greediness", 0.3f);              // Often makes greedy choices
        SetAIField("trapDetectionRange", 1);         // Only sees traps when very close
    }

    private void ApplyMediumSettings()
    {
        // Balanced - good challenge but beatable
        SetAIField("replanningChance", 0.7f);
        SetAIField("pathCommitmentTurns", 3);
        SetAIField("desperationHealthThreshold", 30);
        SetAIField("monsterDangerBias", 0f);
        SetAIField("greediness", 0.15f);
        SetAIField("trapDetectionRange", 2);
    }

    private void ApplyHardSettings()
    {
        // Smart AI with few weaknesses
        SetAIField("replanningChance", 0.9f);
        SetAIField("pathCommitmentTurns", 2);
        SetAIField("desperationHealthThreshold", 20);
        SetAIField("monsterDangerBias", -5f);        // Slightly underestimates (more aggressive)
        SetAIField("greediness", 0.05f);
        SetAIField("trapDetectionRange", 3);
    }

    private void ApplyExpertSettings()
    {
        // Nearly perfect AI
        SetAIField("replanningChance", 1.0f);
        SetAIField("pathCommitmentTurns", 1);
        SetAIField("desperationHealthThreshold", 15);
        SetAIField("monsterDangerBias", -10f);
        SetAIField("greediness", 0.0f);
        SetAIField("trapDetectionRange", 4);
    }

    private void SetAIField(string fieldName, object value)
    {
        var field = adventurerAI.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(adventurerAI, value);
        }
        else
        {
            Debug.LogWarning($"Field {fieldName} not found on AdventurerAI");
        }
    }

    // Public methods for UI
    public void SetEasy() => ApplyDifficulty(DifficultyLevel.Easy);
    public void SetMedium() => ApplyDifficulty(DifficultyLevel.Medium);
    public void SetHard() => ApplyDifficulty(DifficultyLevel.Hard);
    public void SetExpert() => ApplyDifficulty(DifficultyLevel.Expert);
}