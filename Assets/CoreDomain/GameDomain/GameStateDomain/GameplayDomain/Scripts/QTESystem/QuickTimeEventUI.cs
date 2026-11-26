using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickTimeEventUI : MonoBehaviour
{
    [Header("Referências da interface")]
    [SerializeField] private GameObject qtePanel;
    [SerializeField] private TMP_Text qteText;
    [SerializeField] private Slider timerSlider;

    [Header("Configuração")]
    [SerializeField] private float resultDisplayTime = 2f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        qtePanel.SetActive(false);
    }

    public void ShowLetter(string letter)
    {
        StopHideCoroutine();

        qtePanel.SetActive(true);
        qteText.text = letter;

        if (timerSlider != null)
            timerSlider.value = 1f;
    }

    public void UpdateTimer(float normalizedTime)
    {
        if (timerSlider != null)
            timerSlider.value = Mathf.Clamp01(normalizedTime);
    }

    public void ShowResult(string message)
    {
        qtePanel.SetActive(true);
        qteText.text = message;

        if (timerSlider != null)
            timerSlider.value = 0f;

        StopHideCoroutine();
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(resultDisplayTime);

        qtePanel.SetActive(false);
        hideCoroutine = null;
    }

    private void StopHideCoroutine()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }

    public void HideQTE()
    {
        StopHideCoroutine();
        qtePanel.SetActive(false);
    }
}