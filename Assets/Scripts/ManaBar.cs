using UnityEngine.UI;
using UnityEngine;

// public class ManaUI : MonoBehaviour
// {
//     [SerializeField] private Image manaFill; // the Filled image
//     [SerializeField] private float maxMana = 100f;

//     [SerializeField] private PlayerManager playerManager;
//     private float mana = 100;

//     public void SetMana(float value)
//     {   
//         mana = playerManager.CurrentMana;
//         mana = Mathf.Clamp(value, 0, mana);
//         manaFill.fillAmount = mana / maxMana;
//     }
// }

using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    void Start()
    {
        // Force initial sync (important)
        if (PlayerManager.Instance != null)
            OnManaChanged(
                PlayerManager.Instance.CurrentMana,
                PlayerManager.Instance.MaxMana
            );
    }

    void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnManaChanged.AddListener(OnManaChanged);
    }

    void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnManaChanged.RemoveListener(OnManaChanged);
    }

    private void OnManaChanged(int current, int max)
    {
        fillImage.fillAmount = (max <= 0) ? 0f : (float)current / max;
    }
}

