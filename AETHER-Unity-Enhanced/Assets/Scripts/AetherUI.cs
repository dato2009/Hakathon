using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aether
{
    public static class Ui
    {
        public static readonly Color Background = Hex("07111F");
        public static readonly Color Sidebar = Hex("0A1829");
        public static readonly Color Card = Hex("12253A");
        public static readonly Color Surface = Hex("1B3349");
        public static readonly Color Line = Hex("31516A");
        public static readonly Color Ink = Hex("E6F5FF");
        public static readonly Color Muted = Hex("A3BDCE");
        public static readonly Color Cyan = Hex("57E0E6");
        public static readonly Color Green = Hex("62DDB2");
        public static readonly Color Amber = Hex("F0C576");
        public static readonly Color Red = Hex("FF6F83");
        private static Font font;

        public static Color Hex(string rgb)
        {
            Color c;
            return ColorUtility.TryParseHtmlString("#" + rgb, out c) ? c : Color.white;
        }

        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch { }
                    if (font == null)
                    {
                        try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                        catch { }
                    }
                }
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return rect;
        }

        public static RectTransform Panel(string name, Transform parent, Color bg)
        {
            RectTransform rect = Rect(name, parent);
            Image img = rect.gameObject.AddComponent<Image>();
            img.color = bg;
            return rect;
        }

        public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void Size(Transform item, float height, float width = -1f)
        {
            LayoutElement element = item.GetComponent<LayoutElement>();
            if (element == null) element = item.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            if (width >= 0) { element.preferredWidth = width; element.minWidth = width; }
        }

        public static void Vertical(Transform parent, int pad = 0, float spacing = 10,
            bool expandHeight = false)
        {
            VerticalLayoutGroup group = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(pad, pad, pad, pad);
            group.spacing = spacing;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = expandHeight;
            group.childForceExpandWidth = true;
        }

        public static void Horizontal(Transform parent, int pad = 0, float spacing = 12,
            bool expandWidth = true)
        {
            HorizontalLayoutGroup group = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(pad, pad, pad, pad);
            group.spacing = spacing;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = true;
            group.childForceExpandWidth = expandWidth;
        }

        public static RectTransform Row(Transform parent, float height = 90f, float gap = 12f)
        {
            RectTransform row = Rect("Row", parent);
            Horizontal(row, 0, gap);
            Size(row, height);
            return row;
        }

        public static RectTransform Column(Transform parent, float gap = 9f)
        {
            RectTransform col = Rect("Column", parent);
            Vertical(col, 0, gap);
            return col;
        }

        public static RectTransform CardPanel(Transform parent, float height, int padding = 18)
        {
            RectTransform panel = Panel("Card", parent, Card);
            Vertical(panel, padding, 9f);
            Size(panel, height);
            return panel;
        }

        public static Text Label(Transform parent, string value, int size, Color color,
            float height, FontStyle style = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect("Text", parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = value;
            label.color = color;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = false;
            Size(rect, height);
            return label;
        }

        public static Button Action(Transform parent, string caption, Action click,
            Color bg, Color fg, float height = 43f, int fontSize = 14)
        {
            RectTransform rect = Panel("Button: " + caption, parent, bg);
            Size(rect, height);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.91f, 0.98f, 1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.90f, 0.96f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.targetGraphic = rect.GetComponent<Image>();
            if (click != null) button.onClick.AddListener(() => click());
            RectTransform textRect = Rect("Caption", rect);
            Stretch(textRect, 9, 2, 9, 2);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = caption;
            text.fontSize = fontSize;
            text.color = fg;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return button;
        }

        public static InputField Input(Transform parent, string startingText,
            string placeholder = "Enter value")
        {
            RectTransform background = Panel("Input", parent, Surface);
            Size(background, 43f);
            InputField field = background.gameObject.AddComponent<InputField>();
            RectTransform textRect = Rect("Input text", background);
            Stretch(textRect, 12, 4, 12, 4);
            Text text = textRect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = 15;
            text.color = Ink;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform placeholderRect = Rect("Placeholder", background);
            Stretch(placeholderRect, 12, 4, 12, 4);
            Text hint = placeholderRect.gameObject.AddComponent<Text>();
            hint.font = DefaultFont;
            hint.fontSize = 14;
            hint.color = Muted;
            hint.text = placeholder;
            hint.fontStyle = FontStyle.Italic;
            hint.alignment = TextAnchor.MiddleLeft;
            field.textComponent = text;
            field.placeholder = hint;
            field.text = startingText;
            return field;
        }

        public static RectTransform Space(Transform parent, float height)
        {
            RectTransform rect = Rect("Space", parent);
            Size(rect, height);
            return rect;
        }

        public static ScrollRect Scroll(Transform parent, float top, float left = 0)
        {
            RectTransform holder = Rect("ScrollArea", parent);
            Stretch(holder, left, 14, 20, top);
            RectTransform viewport = Rect("Viewport", holder);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            Vertical(content, 12, 16f);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = holder.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 35f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        public static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
        }
    }

    public class HeartRateGraph : MonoBehaviour
    {
        private List<float> heartRates = new List<float>();
        private float baseline;
        private float lastWidth = -1f;
        private float lastHeight = -1f;

        public void Configure(List<HealthSample> samples, float baseRate)
        {
            heartRates.Clear();
            int start = Mathf.Max(0, samples.Count - 14);
            for (int i = start; i < samples.Count; i++) heartRates.Add(samples[i].heartRate);
            baseline = baseRate;
            lastWidth = -1;
        }

        private void LateUpdate()
        {
            RectTransform rect = (RectTransform)transform;
            if (Mathf.Abs(rect.rect.width - lastWidth) > 1 || Mathf.Abs(rect.rect.height - lastHeight) > 1)
            {
                lastWidth = rect.rect.width;
                lastHeight = rect.rect.height;
                Draw();
            }
        }

        private void Draw()
        {
            if (heartRates.Count < 2) return;
            Ui.Clear(transform);
            float width = Mathf.Max(100, ((RectTransform)transform).rect.width);
            float height = Mathf.Max(120, ((RectTransform)transform).rect.height);
            float minValue = baseline - 7f, maxValue = baseline + 7f;
            foreach (float value in heartRates)
            {
                minValue = Mathf.Min(minValue, value - 3);
                maxValue = Mathf.Max(maxValue, value + 3);
            }
            float xmin = 36f, xmax = width - 20f, ymin = 25f, ymax = height - 25f;
            float baselineY = Mathf.Lerp(ymin, ymax, (baseline - minValue) / (maxValue - minValue));
            for (int i = 0; i < 17; i++)
            {
                float xa = Mathf.Lerp(xmin, xmax, i / 17f);
                float xb = Mathf.Lerp(xmin, xmax, (i + 0.56f) / 17f);
                DrawLine(new Vector2(xa, baselineY), new Vector2(xb, baselineY), Ui.Amber, 1.4f);
            }
            for (int i = 0; i < heartRates.Count; i++)
            {
                float x = Mathf.Lerp(xmin, xmax, i / (float)(heartRates.Count - 1));
                float y = Mathf.Lerp(ymin, ymax, (heartRates[i] - minValue) / (maxValue - minValue));
                if (i > 0)
                {
                    float prevX = Mathf.Lerp(xmin, xmax, (i - 1) / (float)(heartRates.Count - 1));
                    float prevY = Mathf.Lerp(ymin, ymax, (heartRates[i - 1] - minValue) / (maxValue - minValue));
                    DrawLine(new Vector2(prevX, prevY), new Vector2(x, y), Ui.Cyan, 3f);
                }
                Dot(new Vector2(x, y), Ui.Cyan, 8f);
            }
            FloatingText(string.Format("{0:0} bpm", maxValue), new Vector2(5f, ymax - 6f));
            FloatingText(string.Format("{0:0} bpm", minValue), new Vector2(5f, ymin - 6f));
            FloatingText("Baseline " + baseline.ToString("0.0"), new Vector2(width - 148f, baselineY + 9f), Ui.Amber);
        }

        private void DrawLine(Vector2 a, Vector2 b, Color c, float thickness)
        {
            RectTransform line = Ui.Panel("Graph line", transform, c);
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = (a + b) * 0.5f;
            line.sizeDelta = new Vector2(Vector2.Distance(a, b), thickness);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            line.localEulerAngles = new Vector3(0, 0, angle);
        }

        private void Dot(Vector2 pos, Color c, float size)
        {
            RectTransform point = Ui.Panel("Graph point", transform, c);
            point.anchorMin = Vector2.zero;
            point.anchorMax = Vector2.zero;
            point.pivot = new Vector2(0.5f, 0.5f);
            point.anchoredPosition = pos;
            point.sizeDelta = new Vector2(size, size);
        }

        private void FloatingText(string value, Vector2 position, Color? color = null)
        {
            RectTransform labelRect = Ui.Rect("Graph label", transform);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = position;
            labelRect.sizeDelta = new Vector2(145, 20);
            Text label = labelRect.gameObject.AddComponent<Text>();
            label.font = Ui.DefaultFont;
            label.color = color ?? Ui.Muted;
            label.fontSize = 11;
            label.text = value;
        }
    }
}
