using UnityEngine;
using UnityEngine.UI;

public class ManaBarUI : MonoBehaviour
{
    [SerializeField] private RoomManipulationSystem roomManipulator;
    [SerializeField] private Image fillImage;

    void Update()
    {
        if (roomManipulator == null || fillImage == null) return;

        fillImage.fillAmount =
            (float)roomManipulator.currentMana / roomManipulator.maxMana;
    }
}
