using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Needed to load the game scene
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class SlideshowController : MonoBehaviour
{
    [Header("Configuration")]
    public Sprite[] slides;          // Drag all your intro images here
    public string nextSceneName;     // The name of the scene to load after the intro
    public float fadeDuration = 0.5f; // How fast the fade happens

    [Header("UI References")]
    public UnityEngine.UI.Image slideDisplay; // We added "UnityEngine.UI." before Image
    public UnityEngine.UI.Image fadeOverlay; // Assign 'FadeOverlay' here

    private int currentSlideIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        // Ensure the overlay is transparent at start
        fadeOverlay.color = new Color(0, 0, 0, 0);

        // Load the first slide immediately
        if (slides.Length > 0)
        {
            slideDisplay.sprite = slides[0];
        }
    }

    void Update()
    {
        // New Input System check for Left Mouse Button
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        // New Input System check for Space bar (simpler than checking 'any' key)
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (!isTransitioning && (mouseClicked || spacePressed))
        {
            StartCoroutine(PlayTransition());
        }
    }

    IEnumerator PlayTransition()
    {
        isTransitioning = true;

        // 1. Fade to Black (Alpha 0 -> 1)
        yield return StartCoroutine(FadeRoutine(0, 1));

        // 2. Prepare the next step
        currentSlideIndex++;

        // 3. Check if we have more slides
        if (currentSlideIndex < slides.Length)
        {
            // Swap the image while the screen is black
            slideDisplay.sprite = slides[currentSlideIndex];

            // Fade back to Clear (Alpha 1 -> 0)
            yield return StartCoroutine(FadeRoutine(1, 0));

            // Allow clicking again
            isTransitioning = false;
        }
        else
        {
            // No more slides? Load the actual game
            SceneManager.LoadScene(nextSceneName);
        }
    }

    // A generic helper to fade the overlay
    IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        Color c = fadeOverlay.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            fadeOverlay.color = new Color(c.r, c.g, c.b, newAlpha);
            yield return null; // Wait for next frame
        }

        // Ensure we end on the exact value
        fadeOverlay.color = new Color(c.r, c.g, c.b, endAlpha);
    }
}