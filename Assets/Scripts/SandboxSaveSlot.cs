using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>存档槽双击接收器。槽本身是 RawImage，不是 Button。</summary>
public class SandboxSaveSlot : MonoBehaviour, IPointerClickHandler {
    public SandboxSaveEntry entry;
    public Action<SandboxSaveEntry> onDoubleClick;

    private float _lastClickTime = -10f;

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        float now = Time.unscaledTime;
        if (now - _lastClickTime <= 0.35f) {
            _lastClickTime = -10f;
            onDoubleClick?.Invoke(entry);
        } else {
            _lastClickTime = now;
        }
    }
}
