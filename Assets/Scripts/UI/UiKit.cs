using UnityEngine;
using UnityEngine.UI;

namespace Shmupper
{
    /// Thin builders over uGUI. The whole interface is assembled in code, so these exist to keep
    /// the screens readable: every panel is a few calls rather than twenty lines of RectTransform
    /// bookkeeping. Legacy Text is used deliberately - it needs no imported font asset, which
    /// keeps the project free of art dependencies.
    public static class UiKit
    {
        public const int SortHud = 10;
        public const int SortFrontend = 50;

        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
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

        public static Text Label(Transform parent, string name, string content, int size, Color color,
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

            var text = go.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        /// Centred banner text, the shape almost every frontend line uses.
        public static Text Banner(Transform parent, string name, string content, int size, Color color, float y)
        {
            return Label(parent, name, content, size, color, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1700f, size + 18f));
        }

        /// Cheap drop shadow: a second copy of the label offset behind the first. Against a
        /// bright torch-lit wall, unshadowed HUD text becomes unreadable.
        public static Text Shadowed(Transform parent, string name, string content, int size, Color color,
                                    TextAnchor anchor, Vector2 anchorPoint, Vector2 pivot,
                                    Vector2 position, Vector2 box, out Text shadow)
        {
            shadow = Label(parent, name + "Shadow", content, size, new Color(0f, 0f, 0f, 0.75f),
                anchor, anchorPoint, pivot, position + new Vector2(2.5f, -2.5f), box);

            return Label(parent, name, content, size, color, anchor, anchorPoint, pivot, position, box);
        }

        public static void SetText(Text label, Text shadow, string content)
        {
            if (label != null) label.text = content;
            if (shadow != null) shadow.text = content;
        }

        public static string Money(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
