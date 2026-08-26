using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class ProgressTracker : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text boxesText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image progressBarFill;

    [Header("Progress")]
    [Min(1)]
    [SerializeField] private int totalBoxes = 10;

    [Header("Audio Feedback")]
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip incorrectClip;
    [SerializeField] private AudioClip completionClip;

    [Header("Particle Feedback")]
    [SerializeField] private ParticleSystem successParticles;

    [Min(1)]
    [SerializeField] private int correctParticleCount = 20;

    [Min(1)]
    [SerializeField] private int completionParticleCount = 50;

    private readonly HashSet<GameObject> countedBoxes =
        new HashSet<GameObject>();

    private int successfullyLifted;
    private float elapsedTime;
    private bool timerRunning;
    private Coroutine feedbackRoutine;

    private void Start()
    {
        ResetProgress();
    }

    private void Update()
    {
        if (!timerRunning)
            return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    public void RegisterPlatformBox(Collider other)
    {
        if (!timerRunning || other == null)
            return;

        GameObject box = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!box.CompareTag("Box"))
            return;

        if (!countedBoxes.Add(box))
            return;

        successfullyLifted++;
        UpdateBoxesText();

        Debug.Log(
            $"Platform progress: {successfullyLifted}/{totalBoxes}"
        );

        if (successfullyLifted >= totalBoxes)
        {
            CompleteTraining();
        }
        else
        {
            PlaySuccessParticles(correctParticleCount);
            PlayFeedbackSound(correctClip);

            ShowTemporaryFeedback(
                "BOX DELIVERED!",
                new Color32(80, 255, 120, 255)
            );
        }
    }
    
    public void RegisterLift(SelectEnterEventArgs args)
    {
        if (!timerRunning)
            return;

        if (args == null || args.interactableObject == null)
            return;

        GameObject selectedObject =
            args.interactableObject.transform.gameObject;

        // Show incorrect feedback for a non-box interactable.
        if (!selectedObject.CompareTag("Box"))
        {
            PlayFeedbackSound(incorrectClip);

            ShowTemporaryFeedback(
                "INVALID OBJECT",
                new Color32(255, 80, 80, 255)
            );

            return;
        }

        // Do not count the same box more than once.
        if (!countedBoxes.Add(selectedObject))
        {
            PlayFeedbackSound(incorrectClip);

            ShowTemporaryFeedback(
                "BOX ALREADY COUNTED",
                new Color32(255, 190, 60, 255)
            );

            return;
        }

        successfullyLifted++;
        UpdateBoxesText();

        Debug.Log(
            $"Progress: {successfullyLifted}/{totalBoxes} boxes lifted."
        );

        if (successfullyLifted >= totalBoxes)
        {
            CompleteTraining();
        }
        else
        {
            PlaySuccessParticles(correctParticleCount);
            PlayFeedbackSound(correctClip);

            ShowTemporaryFeedback(
                "BOX REGISTERED!",
                new Color32(80, 255, 120, 255)
            );
        }
    }

    public void ResetProgress()
    {
        StopFeedbackRoutine();

        if (feedbackAudioSource != null)
            feedbackAudioSource.Stop();

        if (successParticles != null)
        {
            successParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        successfullyLifted = 0;
        elapsedTime = 0f;
        timerRunning = true;
        countedBoxes.Clear();

        if (statusText != null)
        {
            statusText.text = "TRAINING PROGRESS";
            statusText.color = Color.white;
        }

        UpdateBoxesText();
        UpdateTimerText();
    }

    private void CompleteTraining()
    {
        StopFeedbackRoutine();

        timerRunning = false;
        UpdateTimerText();

        PlaySuccessParticles(completionParticleCount);
        PlayFeedbackSound(completionClip);

        if (statusText != null)
        {
            statusText.text = "TRAINING COMPLETE!";
            statusText.color = new Color32(80, 255, 120, 255);
        }

        Debug.Log(
            $"Training completed in {elapsedTime:F1} seconds."
        );
    }

    private void ShowTemporaryFeedback(string message, Color color)
    {
        if (statusText == null)
            return;

        StopFeedbackRoutine();

        feedbackRoutine = StartCoroutine(
            FeedbackRoutine(message, color)
        );
    }

    private IEnumerator FeedbackRoutine(string message, Color color)
    {
        statusText.text = message;
        statusText.color = color;

        yield return new WaitForSeconds(1.5f);

        if (timerRunning)
        {
            statusText.text = "TRAINING PROGRESS";
            statusText.color = Color.white;
        }

        feedbackRoutine = null;
    }

    private void StopFeedbackRoutine()
    {
        if (feedbackRoutine == null)
            return;

        StopCoroutine(feedbackRoutine);
        feedbackRoutine = null;
    }

    private void PlayFeedbackSound(AudioClip clip)
    {
        if (feedbackAudioSource == null || clip == null)
            return;

        feedbackAudioSource.PlayOneShot(clip);
    }

    private void PlaySuccessParticles(int particleCount)
    {
        if (successParticles == null)
            return;

        successParticles.Emit(particleCount);
    }

    private void UpdateBoxesText()
    {
        if (boxesText != null)
        {
            boxesText.text =
                $"Boxes Lifted: {successfullyLifted}/{totalBoxes}";
        }

        if (progressBarFill != null)
        {
            float progress = totalBoxes > 0
                ? (float)successfullyLifted / totalBoxes
                : 0f;

            progressBarFill.fillAmount = Mathf.Clamp01(progress);
        }
    }

    private void UpdateTimerText()
    {
        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timerText != null)
        {
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    private void OnValidate()
    {
        totalBoxes = Mathf.Max(1, totalBoxes);
    }
}