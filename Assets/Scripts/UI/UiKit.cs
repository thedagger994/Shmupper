using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shmupper
{
    /// Thin builders over uGUI. The whole interface is assembled in code, so these exist to keep
    /// the screens readable: every panel is a few calls rather than twenty lines of RectTransform
    /// bookkeeping.
    ///
    /// Text is TextMeshPro rather than uGUI's legacy Text. Legacy Text rasterises a bitmap at one
    /// fixed pixel size and the canvas scaler then stretches it to fit the display, which is why
    /// the front end went soft and blurry on anything above the 1920x1080 reference resolution.
    /// TextMeshPro stores glyphs as signed distance fields and reconstructs the outline at
    /// whatever size it is drawn, so it stays sharp at any resolution.
    public static class UiKit
    {
        public const int SortHud = 10;
        public const int SortFrontend = 50;

        /// IM Fell English - a digitisation of a 17th century letterpress face, with the uneven,
        /// over-inked edges of type pressed into paper. Chosen to sit with the castle setting and
        /// the film grain rather than against them.
        public const string FontResourcePath = "Fonts/IMFellEnglish SDF";

        static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.Load<TMP_FontAsset>(FontResourcePath);

                // Falls back to whatever TextMeshPro considers default, so a project where the
                // font asset has not been forged yet still renders readable menus.
                if (_font == null && TMP_Settings.instance != null) _font = TMP_Settings.defaultFontAsset;

                return _font;
            }
        }

        /// uGUI's nine point anchor maps one to one onto TextMeshPro's alignment enum. Doing the
        /// translation here means every call site keeps reading in the vocabulary it was written
        /// in rather than being rewritten for the new text component.
        public static TextAlignmentOptions Align(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:  return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:   return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:   return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:  return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:  return TextAlignmentOptions.Bottom;
                default:                      return TextAlignmentOptions.BottomRight;
            }
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Rect(Transform parent, string name, Color color,
                                 Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Stretch(Transform parent, string name, Color color)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TMP_Text Label(Transform parent, string name, string content, int size, Color color,
                                     TextAnchor anchor, Vector2 anchorPoint, Vector2 pivot,
                                     Vector2 position, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorPoint;
            rt.anchorMax = anchorPoint;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = box;

            var text = go.AddComponent<TextMeshProUGUI>();
            if (Font != null) text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = Align(anchor);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = true;

            // IM Fell English is a book face with generous sidebearings. A touch of extra
            // tracking stops the all caps arcade lines from looking cramped at large sizes.
            text.characterSpacing = 4f;
            return text;
        }

        /// Centred banner text, the shape almost every frontend line uses.
        public static TMP_Text Banner(Transform parent, string name, string content, int size, Color color, float y)
        {
            return Label(parent, name, content, size, color, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1700f, size + 18f));
        }

        /// Cheap drop shadow: a second copy of the label offset behind the first. Against a
        /// bright torch-lit wall, unshadowed HUD text becomes unreadable.
        public static TMP_Text Shadowed(Transform parent, string name, string content, int size, Color color,
                                        TextAnchor anchor, Vector2 anchorPoint, Vector2 pivot,
                                        Vector2 position, Vector2 box, out TMP_Text shadow)
        {
            shadow = Label(parent, name + "Shadow", content, size, new Color(0f, 0f, 0f, 0.75f),
                anchor, anchorPoint, pivot, position + new Vector2(2.5f, -2.5f), box);

            return Label(parent, name, content, size, color, anchor, anchorPoint, pivot, position, box);
        }

        public static void SetText(TMP_Text label, TMP_Text shadow, string content)
        {
            if (label != null) label.text = content;
            if (shadow != null) shadow.text = content;
        }

        public static string Money(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
