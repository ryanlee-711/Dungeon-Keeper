using UnityEngine;
using UnityEngine.InputSystem;

public class CameraWASDController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool shiftToSprint = true;
    [SerializeField] private float sprintMultiplier = 2f;

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float h = 0f;
        float v = 0f;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed) h += 1f;
        if (kb.sKey.isPressed) v -= 1f;
        if (kb.wKey.isPressed) v += 1f;

        Vector3 move = new Vector3(h, v, 0f);
        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = moveSpeed;
        if (shiftToSprint && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
            speed *= sprintMultiplier;

        transform.position += move * speed * dt;
    }
}
