using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitButton : MonoBehaviour {
    public string[] units = { };
    public int currentUnit = 0;

    void Start() {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        if (currentUnit >= units.Length - 1) {
            currentUnit = 0;
        } else {
            currentUnit++;
        }

        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
        label.text = GetUnit();
    }

    public string GetUnit() {
        return units[currentUnit];
    }
}
