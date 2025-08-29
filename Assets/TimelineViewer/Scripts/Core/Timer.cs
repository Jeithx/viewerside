using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private float elapsedTime;
    private bool playButtonClicked;

    private void Awake()
    {
        playButtonClicked = false;
        elapsedTime = 0f;
        Render();
    }

    private void Update()
    {
        if (!playButtonClicked) return;

        elapsedTime += Time.deltaTime;
        Render();
    }

    public void stopTimer() { playButtonClicked = false; }
    public void startTimer() { playButtonClicked = true; }

    public float getCurrentTime() => elapsedTime;

    public void SetCurrentTime(float seconds)
    {
        elapsedTime = Mathf.Max(0f, seconds);
        Render();
    }

    private void Render()
    {
        if (!timerText) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int hours = Mathf.FloorToInt(minutes / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);

        timerText.text = $"{hours:00}:{minutes:00}:{seconds:00}:{milliseconds:00}";
    }
}
