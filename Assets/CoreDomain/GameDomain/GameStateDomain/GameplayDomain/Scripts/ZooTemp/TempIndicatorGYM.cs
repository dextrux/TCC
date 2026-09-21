using TMPro;
using UnityEngine;

public class TempIndicatorGYM : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI statsText;
    private float sanity;
    private bool beingViewed;

    public static TempIndicatorGYM Instance;

    private void Awake() {
        Instance = this;
        UpdateText();
    }

    private void UpdateText() {
        statsText.text = "Sanidade: " + sanity.ToString("000") + "\n" +
            "Esta sendo visto: " + beingViewed + "\n";
    }

    public void SetSanity(float newSanity) {
        sanity = newSanity;
        UpdateText();
    }

    public void SetViewed(bool newViewed) {
        beingViewed = newViewed;
        UpdateText();
    }

}
