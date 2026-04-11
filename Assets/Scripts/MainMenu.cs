using UnityEngine;

public class MainMenu : MonoBehaviour {
    public void ButtonOnClick_Sim() {
        // TODO
    }

    public void ButtonOnClick_Quit() {
#if UNITY_EDITOR // 是否在Unity编辑器里？
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}