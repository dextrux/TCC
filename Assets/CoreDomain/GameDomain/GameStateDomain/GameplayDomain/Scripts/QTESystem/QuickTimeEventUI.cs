
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickTimeEventUI : MonoBehaviour
{
    [Header("Referências da interface")]
    [SerializeField] private GameObject qtePanel;
    [SerializeField] private TMP_Text qteText;
    [SerializeField] private Slider timerSlider;

    private void Awake()
    {
        qtePanel.SetActive(false);
    }

    public void ShowLetter(string letter)
    {
        qtePanel.SetActive(true);
        qteText.text = letter;

        if (timerSlider != null)
            timerSlider.value = 1f;
    }

    public void UpdateTimer(float normalizedTime)
    {
        if (timerSlider != null)
        {
            timerSlider.value =
                Mathf.Clamp01(normalizedTime);
        }
    }

    public void ShowResult(string message)
    {
        qtePanel.SetActive(true);
        qteText.text = message;

        if (timerSlider != null)
            timerSlider.value = 0f;
    }

    public void HideQTE()
    {
        qtePanel.SetActive(false);
    }
}
