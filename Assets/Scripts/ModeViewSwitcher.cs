using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ModeViewSwitcher : MonoBehaviour {
    [Header("图片")]
    public Image modeViewImage;          // ModeView 的 Image 组件
    public Sprite mainSequenceSprite;    // MainSequenceIcon
    public Sprite whiteDwarfSprite;      // WhiteDwarfIcon
    public Sprite otherStarSprite;       // OtherStarConceptPic

    [Header("ButtonStart")]
    public Button buttonStart;           // ButtonStart 按钮

    [Header("场景名")]
    public string StellarEvolutionPreparationScene = "StellarEvolutionPreparationScene";
    public string WhiteDwarfEvolutionPreparationScene = "WhiteDwarfEvolutionPreparationScene";
    public string OtherStarCalculationPreparationScene = "OtherStarCalculationPreparationScene";

    void Start() {
        if (modeViewImage != null && mainSequenceSprite != null)
            modeViewImage.sprite = mainSequenceSprite;

        SetStartTarget(StellarEvolutionPreparationScene);
    }

    public void ShowMainSequence() {
        modeViewImage.sprite = mainSequenceSprite;
        SetStartTarget(StellarEvolutionPreparationScene);
    }

    public void ShowWhiteDwarf() {
        modeViewImage.sprite = whiteDwarfSprite;
        SetStartTarget(WhiteDwarfEvolutionPreparationScene);
    }

    public void ShowOtherStar() {
        Debug.Log($"modeViewImage={(modeViewImage == null ? "NULL" : "OK")}, " +
                  $"otherStarSprite={(otherStarSprite == null ? "NULL" : "OK")}");
        if (modeViewImage != null && otherStarSprite != null)
            modeViewImage.sprite = otherStarSprite;
        SetStartTarget(OtherStarCalculationPreparationScene);
    }

    void SetStartTarget(string sceneName) {
        if (buttonStart == null) return;

        buttonStart.onClick.RemoveAllListeners();
        buttonStart.onClick.AddListener(() => SceneManager.LoadScene(sceneName));
    }

    public void ReturnToMainMenu() {
        SceneManager.LoadScene(0);
    }
}