using System;
using UnityEngine;
using UnityEngine.UI;

namespace InfectionShot
{
    /// <summary>Mass bar, status line, hint and the end-of-round panel. Contains no rules.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private RectTransform playerFill;
        [SerializeField] private RectTransform shotFill;
        [SerializeField] private RectTransform criticalZone;
        [SerializeField] private Text status;
        [SerializeField] private GameObject hint;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitle;
        [SerializeField] private Text resultDetail;
        [SerializeField] private Button restartButton;

        public event Action RestartClicked;

        private void Awake()
        {
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
        }

        public void SetBar(float playerFraction, float shotFraction)
        {
            playerFill.anchorMax = new Vector2(Mathf.Clamp01(playerFraction), 1f);
            shotFill.anchorMin = new Vector2(Mathf.Clamp01(playerFraction), 0f);
            shotFill.anchorMax = new Vector2(Mathf.Clamp01(playerFraction + shotFraction), 1f);
        }

        public void SetCriticalFraction(float fraction)
        {
            criticalZone.anchorMin = new Vector2(0f, 0f);
            criticalZone.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        public void SetStatus(string text) => status.text = text;

        public void SetHintVisible(bool visible) => hint.SetActive(visible);

        public void ShowResult(string title, string detail)
        {
            resultTitle.text = title;
            resultDetail.text = detail;
            resultPanel.SetActive(true);
        }

        public void HideResult() => resultPanel.SetActive(false);
    }
}
