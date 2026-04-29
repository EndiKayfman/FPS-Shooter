using UnityEngine;

public sealed class HudController : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Text timerText;
    [SerializeField] UnityEngine.UI.Text scoreText;
    [SerializeField] UnityEngine.UI.Text bannerText;

    public void BindOptional(UnityEngine.UI.Text timer, UnityEngine.UI.Text scores, UnityEngine.UI.Text banner)
    {
        timerText = timer;
        scoreText = scores;
        bannerText = banner;
    }

    public void ShowTimerSeconds(float remaining)
    {
        if (timerText == null) return;
        timerText.text = $"{Mathf.Max(0f, remaining):000}";
    }

    public void ShowScores(int alphaRoundsWon, int betaRoundsWon)
    {
        if (scoreText == null) return;
        scoreText.text = $"ALPHA {alphaRoundsWon}  —  {betaRoundsWon} BETA";
    }

    public void ShowBanner(string line)
    {
        if (bannerText == null) return;
        bannerText.enabled = true;
        bannerText.text = line;
    }

    public void ClearBanner()
    {
        if (bannerText == null) return;
        bannerText.text = "";
        bannerText.enabled = false;
    }

    /// <summary>Big full-screen-ish message during intermission.</summary>
    public void Announce(string line) => ShowBanner(line);
}
