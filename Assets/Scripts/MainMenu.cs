using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
    public void ButtonOnClick_StellarEvolution() {
        SceneManager.LoadScene("EvolutionChoose");
    }

    public void ButtonOnClick_CreateUniverse() {
        LoadingScreen.LoadScene("SandboxScene");
    }
    public void ButtonOnClick_Settings() {
        SceneManager.LoadScene("Settings");
    }

    public void ButtonOnClick_Website() {
        Application.OpenURL("https://upb.m82.org");
    }
    public void ButtonOnClick_Quit() {
#if UNITY_EDITOR // 是否在Unity编辑器里？
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}