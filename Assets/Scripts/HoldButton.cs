using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 按住不放的按钮：把“当前是否按住”暴露给逻辑，用来做连续调节（时间流速等）。
///
/// 只处理指针按下/抬起/移出：
/// - 按住时 <see cref="IsHeld"/> 为 true；
/// - 指针移出按钮或松开都会立即复位，不会出现“松手了还在加速”；
/// - 组件被禁用/销毁时也会复位。
/// </summary>
public class HoldButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler {

    public bool IsHeld { get; private set; }

    /// <summary>按住状态变化时触发（true = 按下，false = 松开/移出）。</summary>
    public event Action<bool> HeldChanged;

    public void OnPointerDown(PointerEventData eventData) {
        SetHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData) {
        SetHeld(false);
    }

    public void OnPointerExit(PointerEventData eventData) {
        SetHeld(false);
    }

    /// <summary>按住不放拖出去又拖回来时重新生效。</summary>
    public void OnPointerEnter(PointerEventData eventData) {
        if (Input.GetMouseButton(0)) SetHeld(true);
    }

    private void OnDisable() {
        SetHeld(false);
    }

    private void SetHeld(bool held) {
        if (IsHeld == held) return;
        IsHeld = held;
        HeldChanged?.Invoke(held);
    }
}
