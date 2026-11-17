using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPB.Localization
{
    /// <summary>
    /// 语言面板：Language_Panel 上的两个按钮（Button_zh_cn / Button_en_us）。
    /// 当前语言对应的按钮显示为纯白色，另一个保持原来的半透明灰色。
    /// 选择结果交给 LanguageManager 保存（PlayerPrefs）。
    /// </summary>
    [DisallowMultipleComponent]
    public class LanguagePanel : MonoBehaviour
    {
        [Header("按钮（留空则按名字自动查找）")]
        [SerializeField] Button zhButton;
        [SerializeField] Button enButton;
        [SerializeField] Button openButton;

        [Header("打开面板的按钮名字")]
        [SerializeField] string openButtonName = "Button_ChooseLanguage";

        [Header("选中 / 未选中颜色")]
        [SerializeField] Color selectedColor = Color.white;
        [Tooltip("未选中按钮的压暗系数，1 = 保持原样，0.5 = 亮度减半")]
        [Range(0f, 1f)]
        [SerializeField] float unselectedDim = 1f;

        [Header("点面板外面自动收起")]
        [SerializeField] bool closeOnOutsideClick = true;

        Image _zhImage;
        Image _enImage;
        Color _zhBase = Color.white;
        Color _enBase = Color.white;

        Canvas _canvas;
        bool _openWired;

        void Awake()
        {
            Resolve();
            CacheBaseColor(zhButton, ref _zhImage, ref _zhBase);
            CacheBaseColor(enButton, ref _enImage, ref _enBase);
        }

        void Start()
        {
            if (zhButton != null)
            {
                zhButton.onClick.RemoveListener(OnZhClicked);
                zhButton.onClick.AddListener(OnZhClicked);
            }

            if (enButton != null)
            {
                enButton.onClick.RemoveListener(OnEnClicked);
                enButton.onClick.AddListener(OnEnClicked);
            }

            WireOpenButton();
            if (openButton == null)
            {
                // 没有打开按钮，说明面板是常驻的，不应该自己收起来
                closeOnOutsideClick = false;
            }

            ApplyColors();
        }

        void OnDestroy()
        {
            if (zhButton != null) zhButton.onClick.RemoveListener(OnZhClicked);
            if (enButton != null) enButton.onClick.RemoveListener(OnEnClicked);
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
        }

        void Update()
        {
            if (!closeOnOutsideClick) return;
            if (!gameObject.activeSelf) return;
            if (openButton == null) return;
            if (!Input.GetMouseButtonDown(0)) return;

            if (IsPointerOver(transform as RectTransform)) return;
            if (IsPointerOver(openButton.transform as RectTransform)) return;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 把“语言……”按钮接上开关逻辑。
        /// 面板默认是隐藏的（未激活），组件自己的 Awake/Start 不会跑，
        /// 所以由 LanguageBootstrap 在场景加载后主动调用这里。
        /// </summary>
        public void WireOpenButton()
        {
            if (_openWired) return;

            Resolve();
            if (openButton == null) return;

            // 场景里这条 OnClick 是一条 target 为空的坏引用（指向 MainMenu.Quit），清掉。
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(Toggle);
            _openWired = true;
            closeOnOutsideClick = true;
        }
        void OnZhClicked() { Select(AppLanguage.ZhCn); }

        void OnEnClicked() { Select(AppLanguage.EnUs); }

        void Select(AppLanguage language)
        {
            LanguageManager.Set(language);
            ApplyColors();

            // 只有能重新打开的面板，选完才自动收起
            if (openButton != null) gameObject.SetActive(false);
        }

        void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (gameObject.activeSelf) ApplyColors();
        }

        /// <summary>把当前语言的按钮刷成白色，另一个恢复暗一点的底色。</summary>
        public void ApplyColors()
        {
            Tint(zhButton, _zhImage, _zhBase, LanguageManager.Current == AppLanguage.ZhCn);
            Tint(enButton, _enImage, _enBase, LanguageManager.Current == AppLanguage.EnUs);
        }

        void Tint(Button button, Image image, Color baseColor, bool selected)
        {
            if (button == null) return;

            if (image != null)
            {
                image.color = selected
                    ? selectedColor
                    : new Color(baseColor.r * unselectedDim, baseColor.g * unselectedDim,
                                baseColor.b * unselectedDim, baseColor.a);
                return;
            }

            // 没有背景图的按钮：只能靠文字颜色区分
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = selected ? selectedColor : new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }

        void Resolve()
        {
            _canvas = GetComponentInParent<Canvas>();

            if (zhButton == null)
            {
                Transform t = LanguageManager.FindTransform("Button_zh_cn");
                if (t != null) zhButton = t.GetComponent<Button>();
            }

            if (enButton == null)
            {
                Transform t = LanguageManager.FindTransform("Button_en_us");
                if (t != null) enButton = t.GetComponent<Button>();
            }

            if (openButton == null && !string.IsNullOrEmpty(openButtonName))
            {
                Transform t = LanguageManager.FindTransform(openButtonName);
                // 别把面板自己身上的按钮当成“打开按钮”
                if (t != null && !t.IsChildOf(transform))
                {
                    openButton = t.GetComponent<Button>();
                }
            }
        }

        static void CacheBaseColor(Button button, ref Image image, ref Color baseColor)
        {
            if (button == null) return;

            image = button.GetComponent<Image>();
            if (image != null) baseColor = image.color;
        }

        bool IsPointerOver(RectTransform rect)
        {
            if (rect == null) return false;

            Camera cam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = _canvas.worldCamera;
            }
            return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, cam);
        }
    }
}
