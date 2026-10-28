using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiamondMine.Runtime
{
    public static class UiTheme
    {
        public static readonly Color Ink = new Color(0.10f, 0.18f, 0.24f, 1f);
        public static readonly Color Panel = new Color(0.98f, 0.96f, 0.93f, 1f);
        public static readonly Color Paper = new Color(0.96f, 0.94f, 0.89f, 1f);
        public static readonly Color Muted = new Color(0.36f, 0.44f, 0.49f, 1f);
        public static readonly Color Diamond = new Color(0.20f, 0.72f, 0.92f, 1f);
        public static readonly Color Coin = new Color(0.96f, 0.75f, 0.18f, 1f);
        public static readonly Color Green = new Color(0.24f, 0.66f, 0.26f, 1f);
        public static readonly Color Blue = new Color(0.17f, 0.55f, 0.91f, 1f);
        public static readonly Color DarkNavy = new Color(0.09f, 0.15f, 0.20f, 0.92f);
        public static readonly Color CardBorder = new Color(0.89f, 0.85f, 0.78f, 1f);
        public static readonly Color Disabled = new Color(0.55f, 0.58f, 0.60f, 0.85f);
        public static readonly Color Danger = new Color(0.72f, 0.25f, 0.20f, 1f);
        public static readonly Color Button = new Color(0.24f, 0.66f, 0.26f, 1f);
        public static readonly Color ButtonOn = new Color(0.17f, 0.55f, 0.91f, 1f);
        public static readonly Color Slow = new Color(0.88f, 0.46f, 0.22f, 1f);
        public static readonly Color Banner = new Color(0.45f, 0.18f, 0.12f, 0.96f);
    }

    public static class UiFactory
    {
        static Font font;
        static Font boldFont;
        static Sprite sprite;
        static Sprite neutralCapsule;
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static Font Font
        {
            get
            {
                if (font == null)
                    font = Resources.Load<Font>("Fonts/Nunito") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        static Font BoldFont
        {
            get
            {
                if(boldFont==null)boldFont=Resources.Load<Font>("Fonts/DiamondMineRounded-Bold") ?? Font;
                return boldFont;
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

        // White, antialiased capsule: progress colour must not multiply an already-dark illustration.
        static Sprite NeutralCapsule
        {
            get
            {
                if(neutralCapsule!=null)return neutralCapsule;
                var texture=new Texture2D(64,32,TextureFormat.RGBA32,false);
                texture.name="Neutral scalable progress capsule";
                texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                var pixels=new Color[64*32];
                for(int y=0;y<32;y++)for(int x=0;x<64;x++)
                {
                    float dx=Mathf.Max(Mathf.Abs(x+.5f-32)-16,0),dy=Mathf.Abs(y+.5f-16);
                    float alpha=Mathf.Clamp01(16.5f-Mathf.Sqrt(dx*dx+dy*dy));
                    pixels[y*64+x]=new Color(1,1,1,alpha);
                }
                texture.SetPixels(pixels);texture.Apply(false,true);
                neutralCapsule=Sprite.Create(texture,new Rect(0,0,64,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
                return neutralCapsule;
            }
        }

        public static Sprite GetSprite(string name, Vector4 border = default)
        {
            string key = name + "_" + border.ToString();
            if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
                return cached;

            Texture2D tex = Resources.Load<Texture2D>("Ui/" + name);
            if (tex == null)
                return Sprite;

            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            spriteCache[key] = s;
            return s;
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

        public static RectTransform Card(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetSprite("card_bg", new Vector4(24f, 24f, 24f, 24f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            return rect;
        }

        public static RectTransform DarkPill(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetSprite("dark_pill", new Vector4(28f, 28f, 28f, 28f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            return rect;
        }

        public static RectTransform SpeechBubble(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetSprite("speech_bubble", new Vector4(24f, 24f, 24f, 32f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            return rect;
        }

        public static Button TabButton(string name, Transform parent, string label, bool active, UnityAction onClick)
        {
            RectTransform rect = Rect(name, parent);
            Image img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetSprite("dark_pill", new Vector4(16f, 16f, 16f, 16f));
            img.type = Image.Type.Sliced;
            img.color = active ? UiTheme.DarkNavy : new Color(0.85f, 0.82f, 0.76f, 0.9f);
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text txt = Label(rect, label, 22, active ? Color.white : UiTheme.Ink, TextAnchor.MiddleCenter, true);
            Stretch(txt.rectTransform);
            return btn;
        }

        public static Image Icon(string name, Transform parent, string spriteName)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetSprite(spriteName);
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
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

        public static Text Label(Transform parent, string value, int size, Color color, TextAnchor anchor, bool bold = false)
        {
            RectTransform rect = Rect("Label", parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = bold ? BoldFont : Font;
            text.fontSize = size;
            text.fontStyle = bold && text.font == Font ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        public static Button ButtonGreen(string name, Transform parent, string label, UnityAction onClick, string iconName = null)
        {
            RectTransform rect = Rect(name, parent);
            Image img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetSprite("btn_green", new Vector4(16f, 16f, 16f, 16f));
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(iconName))
            {
                Image icon = Icon("Icon", rect, iconName);
                icon.rectTransform.anchorMin = new Vector2(0.08f, 0.15f);
                icon.rectTransform.anchorMax = new Vector2(0.36f, 0.85f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;

                Text txt = Label(rect, label, 24, Color.white, TextAnchor.MiddleLeft, true);
                txt.rectTransform.anchorMin = new Vector2(0.38f, 0.05f);
                txt.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
                txt.rectTransform.offsetMin = Vector2.zero;
                txt.rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                Text txt = Label(rect, label, 24, Color.white, TextAnchor.MiddleCenter, true);
                Stretch(txt.rectTransform);
            }
            return btn;
        }

        public static Button ButtonBlue(string name, Transform parent, string label, UnityAction onClick)
        {
            RectTransform rect = Rect(name, parent);
            Image img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetSprite("btn_blue", new Vector4(16f, 16f, 16f, 16f));
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text txt = Label(rect, label, 24, Color.white, TextAnchor.MiddleCenter, true);
            Stretch(txt.rectTransform);
            return btn;
        }

        public static Button CircleButton(string name, Transform parent, UnityAction onClick, string iconName = null, string labelText = null)
        {
            RectTransform rect = Rect(name, parent);
            Image img = rect.gameObject.AddComponent<Image>();
            img.sprite = GetSprite("btn_circle");
            img.color = Color.white;
            Button btn = rect.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(iconName))
            {
                Image icon = Icon("Icon", rect, iconName);
                icon.rectTransform.anchorMin = new Vector2(0.20f, 0.22f);
                icon.rectTransform.anchorMax = new Vector2(0.80f, 0.82f);
                icon.rectTransform.offsetMin = Vector2.zero;
                icon.rectTransform.offsetMax = Vector2.zero;
            }
            if (!string.IsNullOrEmpty(labelText))
            {
                Text txt = Label(rect, labelText, 20, Color.white, TextAnchor.LowerCenter, true);
                txt.rectTransform.anchorMin = new Vector2(0f, -0.40f);
                txt.rectTransform.anchorMax = new Vector2(1f, 0.05f);
                txt.rectTransform.offsetMin = Vector2.zero;
                txt.rectTransform.offsetMax = Vector2.zero;
            }
            return btn;
        }

        public static RectTransform ProgressBar(string name, Transform parent, float fill, Color fillColor, float height = 16f)
        {
            RectTransform row = Rect(name, parent);
            Image track = row.gameObject.AddComponent<Image>();
            track.sprite = NeutralCapsule;
            track.type = Image.Type.Sliced;
            track.color = new Color(0.78f, 0.74f, 0.68f, 1f);

            RectTransform fillRect = Rect("Fill", row);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            Image fillImage = fillRect.gameObject.AddComponent<Image>();
            fillImage.sprite = NeutralCapsule;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            return row;
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
            if (button == null) return null;
            Text t = button.GetComponentInChildren<Text>();
            if (t == null)
                t = Label(button.transform, "", 1, Color.clear, TextAnchor.MiddleCenter, false);
            return t;
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
