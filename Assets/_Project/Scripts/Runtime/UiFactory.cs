using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public static class UiTheme
    {
        public static readonly Color Ink = new Color(0.12f, 0.08f, 0.07f, 0.94f);
        public static readonly Color Panel = new Color(0.22f, 0.15f, 0.12f, 1f);
        public static readonly Color Paper = new Color(0.96f, 0.9f, 0.82f, 1f);
        public static readonly Color Muted = new Color(0.8f, 0.72f, 0.62f, 1f);
        public static readonly Color Diamond = new Color(0.25f, 0.78f, 0.84f, 1f);
        public static readonly Color Coin = new Color(0.95f, 0.76f, 0.28f, 1f);
        public static readonly Color Slow = new Color(0.88f, 0.46f, 0.22f, 1f);
        public static readonly Color Button = new Color(0.36f, 0.26f, 0.19f, 1f);
        public static readonly Color ButtonOn = new Color(0.16f, 0.4f, 0.44f, 1f);
        public static readonly Color Disabled = new Color(0.24f, 0.2f, 0.18f, 0.85f);
        public static readonly Color Danger = new Color(0.52f, 0.2f, 0.16f, 1f);
        public static readonly Color Banner = new Color(0.45f, 0.18f, 0.12f, 0.96f);
    }

    public static class UiFactory
    {
        static Font font;
        static Sprite sprite;

        public static Font Font
        {
            get
            {
                if (font == null)
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Sprite Sprite
        {
            get
            {
                if (sprite == null)
                {
                    var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var pixels = new Color[16];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = Color.white;
                    texture.SetPixels(pixels);
                    texture.Apply(false, true);
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                }
                return sprite;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite;
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Box(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform parent, string value, int size, Color color, TextAnchor anchor)
        {
            RectTransform rect = Rect("Label", parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        public static Button Press(Transform parent, string label, UnityAction onClick, Color color, float height)
        {
            height = Mathf.Max(108f, height);
            RectTransform rect = Panel("Button", parent, color);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, 1f);
            colors.selectedColor = color;
            colors.disabledColor = UiTheme.Disabled;
            colors.fadeDuration = 0.04f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.targetGraphic = rect.GetComponent<Image>();
            button.targetGraphic.color = Color.white;
            if (onClick != null)
                button.onClick.AddListener(onClick);
            Text caption = Label(rect, label, 32, UiTheme.Paper, TextAnchor.MiddleCenter);
            UiFactory.Stretch(caption.rectTransform);
            caption.raycastTarget = false;
            return button;
        }

        public static Text Caption(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }

        public static RectTransform Scroll(Transform parent, out RectTransform content)
        {
            RectTransform root = Panel("Scroll", parent, new Color(0f, 0f, 0f, 0f));
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = Panel("Viewport", root, new Color(0f, 0f, 0f, 0.01f));
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Panel("Content", viewport, new Color(0f, 0f, 0f, 0f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 12f;
            layout.padding = new RectOffset(12, 12, 12, 28);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36f;
            return root;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                parent.GetChild(i).gameObject.SetActive(false);
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
