using UnityEngine;
using UnityEngine.UI;

// Процедурни UI текстури и цветови профили за Evidence Board (2003 детективски офис), без външни assets.
internal static class EvidenceBoardVisuals
{
    public enum ButtonStyle
    {
        PrimaryManila,
        CorkControl,
        CaseAction,
        CaseMutedDanger
    }

    public static readonly Color BrassAccent = new Color(0.58f, 0.47f, 0.29f, 0.85f);
    public static readonly Color BrassHairline = new Color(0.58f, 0.47f, 0.29f, 0.38f);
    public static readonly Color HeaderRailColor = new Color(0.13f, 0.10f, 0.08f, 0.90f);
    public static readonly Color ParchmentText = new Color(0.93f, 0.89f, 0.80f, 1f);
    public static readonly Color MutedLabelText = new Color(0.76f, 0.71f, 0.61f, 0.92f);
    public static readonly Color StatusTextColor = new Color(0.92f, 0.84f, 0.64f, 1f);
    public static readonly Color PaperInk = new Color(0.11f, 0.085f, 0.06f, 1f);
    public static readonly Color PaperPlaceholder = new Color(0.44f, 0.38f, 0.29f, 0.74f);
    public static readonly Color PaperSelection = new Color(0.52f, 0.40f, 0.20f, 0.30f);

    private static readonly Color[] PaperTints =
    {
        new Color(0.97f, 0.94f, 0.85f, 1f),
        new Color(0.95f, 0.91f, 0.81f, 1f),
        new Color(0.94f, 0.90f, 0.78f, 1f)
    };

    private static readonly Color[] PinTints =
    {
        new Color(0.72f, 0.21f, 0.17f, 1f),
        new Color(0.75f, 0.56f, 0.23f, 1f),
        new Color(0.57f, 0.18f, 0.20f, 1f),
        new Color(0.30f, 0.40f, 0.44f, 1f)
    };

    private static Sprite corkSprite;
    private static Sprite corkVignetteSprite;
    private static Sprite woodFrameSprite;
    private static Sprite paperSheetSprite;
    private static Sprite printoutPaperSprite;
    private static Sprite timelineCardPaperSprite;
    private static Sprite photoFrameSprite;
    private static Sprite softShadowSprite;
    private static Sprite pushpinSprite;
    private static Sprite buttonSprite;
    private static Sprite slotGuideSprite;

    public static Sprite CorkSprite => corkSprite != null ? corkSprite : (corkSprite = CreateCorkSprite());
    public static Sprite CorkVignetteSprite => corkVignetteSprite != null ? corkVignetteSprite : (corkVignetteSprite = CreateCorkVignetteSprite());
    public static Sprite WoodFrameSprite => woodFrameSprite != null ? woodFrameSprite : (woodFrameSprite = CreateWoodFrameSprite());
    public static Sprite PaperSheetSprite => paperSheetSprite != null ? paperSheetSprite : (paperSheetSprite = CreatePaperSheetSprite());
    public static Sprite PrintoutPaperSprite => printoutPaperSprite != null ? printoutPaperSprite : (printoutPaperSprite = CreatePrintoutPaperSprite());
    public static Sprite TimelineCardPaperSprite => timelineCardPaperSprite != null ? timelineCardPaperSprite : (timelineCardPaperSprite = CreateTimelineCardPaperSprite());
    public static Sprite PhotoFrameSprite => photoFrameSprite != null ? photoFrameSprite : (photoFrameSprite = CreatePhotoFrameSprite());
    public static Sprite SoftShadowSprite => softShadowSprite != null ? softShadowSprite : (softShadowSprite = CreateSoftShadowSprite());
    public static Sprite PushpinSprite => pushpinSprite != null ? pushpinSprite : (pushpinSprite = CreatePushpinSprite());
    public static Sprite ButtonSprite => buttonSprite != null ? buttonSprite : (buttonSprite = CreateButtonSprite());
    public static Sprite SlotGuideSprite => slotGuideSprite != null ? slotGuideSprite : (slotGuideSprite = CreateSlotGuideSprite());

    public static Color GetPaperTint(string noteId) => PaperTints[StableIndex(noteId, PaperTints.Length, 17)];

    public static Color GetPinTint(string noteId) => PinTints[StableIndex(noteId, PinTints.Length, 31)];

    public static void ApplyButtonStyle(Button button, Image background, TMPro.TMP_Text label, ButtonStyle style)
    {
        background.sprite = ButtonSprite;
        background.type = Image.Type.Sliced;

        Color normal;
        Color highlighted;
        Color pressed;
        Color disabled;
        Color textColor;

        switch (style)
        {
            case ButtonStyle.PrimaryManila:
                normal = new Color(0.79f, 0.70f, 0.50f, 1f);
                highlighted = new Color(0.86f, 0.77f, 0.57f, 1f);
                pressed = new Color(0.67f, 0.58f, 0.39f, 1f);
                disabled = new Color(0.48f, 0.43f, 0.33f, 0.65f);
                textColor = PaperInk;
                break;

            case ButtonStyle.CorkControl:
                normal = new Color(0.20f, 0.15f, 0.12f, 0.96f);
                highlighted = new Color(0.28f, 0.21f, 0.16f, 0.98f);
                pressed = new Color(0.14f, 0.10f, 0.08f, 0.98f);
                disabled = new Color(0.16f, 0.12f, 0.10f, 0.45f);
                textColor = ParchmentText;
                break;

            case ButtonStyle.CaseMutedDanger:
                normal = new Color(0.25f, 0.17f, 0.16f, 0.96f);
                highlighted = new Color(0.34f, 0.22f, 0.21f, 0.98f);
                pressed = new Color(0.19f, 0.12f, 0.12f, 0.98f);
                disabled = new Color(0.18f, 0.14f, 0.14f, 0.50f);
                textColor = new Color(0.94f, 0.87f, 0.82f, 1f);
                break;

            default:
                normal = new Color(0.16f, 0.21f, 0.21f, 0.96f);
                highlighted = new Color(0.22f, 0.29f, 0.28f, 0.98f);
                pressed = new Color(0.11f, 0.15f, 0.15f, 0.98f);
                disabled = new Color(0.13f, 0.16f, 0.16f, 0.50f);
                textColor = ParchmentText;
                break;
        }

        background.color = Color.white;
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (label != null)
        {
            label.color = textColor;
        }
    }

    private static int StableIndex(string key, int count, int salt)
    {
        if (string.IsNullOrEmpty(key) || count <= 1) return 0;
        uint hash = (uint)salt;
        for (int i = 0; i < key.Length; i++)
            hash = hash * 31u + key[i];
        return (int)(hash % (uint)count);
    }

    private static Texture2D NewTexture(int width, int height, TextureWrapMode wrapMode)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = wrapMode,
            hideFlags = HideFlags.HideAndDontSave
        };
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex, Vector4 border)
    {
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreateCorkSprite()
    {
        const int size = 256;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Repeat);
        Color[] pixels = new Color[size * size];

        Color baseCork = new Color(0.42f, 0.30f, 0.19f, 1f);
        Color warmGrain = new Color(0.52f, 0.38f, 0.25f, 1f);
        Color darkPore = new Color(0.24f, 0.16f, 0.10f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coarse = TileNoise(x, y, size, 16, 101);
                float medium = TileNoise(x, y, size, 32, 211);
                float fine = Hash01(x, y, 307);
                float pore = Hash01(x / 2, y / 2, 409);

                float blend = coarse * 0.45f + medium * 0.35f + fine * 0.20f;
                Color c = Color.Lerp(baseCork, warmGrain, blend);

                if (fine > 0.89f)
                {
                    c = Color.Lerp(c, new Color(0.58f, 0.44f, 0.29f, 1f), 0.30f);
                }
                else if (fine < 0.12f || pore < 0.07f)
                {
                    c = Color.Lerp(c, darkPore, 0.42f);
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, Vector4.zero);
    }

    private static Sprite CreateCorkVignetteSprite()
    {
        const int size = 128;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            float dy = Mathf.Min(v, 1f - v);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float dx = Mathf.Min(u, 1f - u);

                float edge = Mathf.Min(dx, dy);
                float innerShadow = 1f - Mathf.SmoothStep(0f, 0.11f, edge);

                // Нежна топла винетка към долните и външните ръбове (атмосфера на настолна лампа).
                float distCenter = Vector2.Distance(new Vector2(u, v), new Vector2(0.45f, 0.58f));
                float lampFalloff = Mathf.Clamp01((distCenter - 0.28f) * 0.55f);

                float alpha = Mathf.Clamp01(innerShadow * 0.46f + lampFalloff * 0.22f);
                pixels[y * size + x] = new Color(0.08f, 0.05f, 0.03f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, Vector4.zero);
    }

    private static Sprite CreateWoodFrameSprite()
    {
        const int size = 128;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        Color darkWood = new Color(0.14f, 0.095f, 0.065f, 1f);
        Color midWood = new Color(0.24f, 0.16f, 0.105f, 1f);
        Color bevelLight = new Color(0.34f, 0.24f, 0.16f, 1f);
        Color outerEdge = new Color(0.08f, 0.055f, 0.035f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float grain = Mathf.Sin(y * 0.45f + TileNoise(x, y, size, 16, 521) * 4.5f) * 0.5f + 0.5f;
                float fine = Hash01(x, y, 613) * 0.12f;
                Color c = Color.Lerp(darkWood, midWood, grain * 0.85f + fine);

                int borderDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                if (borderDist <= 1)
                {
                    c = outerEdge;
                }
                else if (borderDist == 2 || borderDist == 3)
                {
                    bool topOrLeft = y >= size - 4 || x <= 3;
                    c = Color.Lerp(c, topOrLeft ? bevelLight : outerEdge, 0.45f);
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, new Vector4(16, 16, 16, 16));
    }

    private static Sprite CreatePaperSheetSprite()
    {
        const int size = 256;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            float dy = Mathf.Min(v, 1f - v);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float dx = Mathf.Min(u, 1f - u);

                // Фина хартиена текстура (влакна + леко неравномерно пожълтяване).
                float fiber = TileNoise(x, y, size, 32, 701) * 0.6f + Hash01(x, y, 809) * 0.4f;
                float blotch = TileNoise(x, y, size, 8, 911);

                float edgeDist = Mathf.Min(dx, dy);
                float edgeWear = 1f - Mathf.SmoothStep(0.0f, 0.14f, edgeDist);
                float cornerWear = Mathf.Clamp01((Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) - 0.46f) * 1.6f);

                // Базовата текстура е светла, за да се тонира чисто чрез PaperTints.
                float r = 0.99f - fiber * 0.032f - blotch * 0.025f - edgeWear * 0.075f - cornerWear * 0.045f;
                float g = 0.975f - fiber * 0.038f - blotch * 0.035f - edgeWear * 0.095f - cornerWear * 0.060f;
                float b = 0.935f - fiber * 0.045f - blotch * 0.050f - edgeWear * 0.125f - cornerWear * 0.080f;

                // Деликатни редове на бележник в зоната за писане (под кабарчето).
                if (u > 0.06f && u < 0.94f && v > 0.08f && v < 0.855f)
                {
                    if (Mathf.Abs(v - 0.845f) < 0.0045f)
                    {
                        // Топла горна разделителна линия под кабарчето.
                        r -= 0.09f;
                        g -= 0.11f;
                        b -= 0.10f;
                    }
                    else
                    {
                        float linePos = (0.845f - v) / 0.082f;
                        float frac = linePos - Mathf.Floor(linePos);
                        if (frac < 0.045f || frac > 0.955f)
                        {
                            r -= 0.045f;
                            g -= 0.050f;
                            b -= 0.040f;
                        }
                    }
                }

                // Лек отпечатък на ръба на листа (1-2px), за да се отделя от корка.
                if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                {
                    r *= 0.64f;
                    g *= 0.61f;
                    b *= 0.56f;
                }
                else if (x == 1 || y == size - 2)
                {
                    r = Mathf.Min(1f, r + 0.02f);
                    g = Mathf.Min(1f, g + 0.02f);
                    b = Mathf.Min(1f, b + 0.015f);
                }

                pixels[y * size + x] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, Vector4.zero);
    }

    private static Sprite CreatePrintoutPaperSprite() => CreateOfficePaperSprite(includeHeaderRule: true);

    private static Sprite CreateTimelineCardPaperSprite() => CreateOfficePaperSprite(includeHeaderRule: false);

    private static Sprite CreateOfficePaperSprite(bool includeHeaderRule)
    {
        const int size = 256;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            float dy = Mathf.Min(v, 1f - v);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float dx = Mathf.Min(u, 1f - u);

                float fiber = TileNoise(x, y, size, 32, 1103) * 0.55f + Hash01(x, y, 1217) * 0.45f;
                float blotch = TileNoise(x, y, size, 8, 1301);
                float edgeDist = Mathf.Min(dx, dy);
                float edgeWear = 1f - Mathf.SmoothStep(0f, 0.12f, edgeDist);

                // Офисна хартия за разпечатка (леко по-светла и по-студена кремава от ръчните бележки).
                float r = 0.965f - fiber * 0.028f - blotch * 0.022f - edgeWear * 0.065f;
                float g = 0.945f - fiber * 0.032f - blotch * 0.028f - edgeWear * 0.075f;
                float b = 0.895f - fiber * 0.038f - blotch * 0.038f - edgeWear * 0.095f;

                // Фина хоризонтална линия под служебното заглавие (само за фиксираните документи, не за динамични Timeline карти).
                if (includeHeaderRule && u > 0.06f && u < 0.94f && Mathf.Abs(v - 0.775f) < 0.0045f)
                {
                    r -= 0.12f;
                    g -= 0.12f;
                    b -= 0.11f;
                }

                // Дискретен хартиен ръб.
                if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                {
                    r *= 0.62f;
                    g *= 0.60f;
                    b *= 0.56f;
                }
                else if (x == 1 || y == size - 2)
                {
                    r = Mathf.Min(1f, r + 0.025f);
                    g = Mathf.Min(1f, g + 0.025f);
                    b = Mathf.Min(1f, b + 0.02f);
                }

                pixels[y * size + x] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, Vector4.zero);
    }

    private static Sprite CreatePhotoFrameSprite()
    {
        const int size = 128;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            float dy = Mathf.Min(v, 1f - v);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float dx = Mathf.Min(u, 1f - u);
                float edgeDist = Mathf.Min(dx, dy);
                float edgeWear = 1f - Mathf.SmoothStep(0f, 0.10f, edgeDist);
                float grain = Hash01(x, y, 1451) * 0.018f;

                // Леко пожълтяла фотохартия (2003).
                float r = 0.96f - grain - edgeWear * 0.055f;
                float g = 0.945f - grain - edgeWear * 0.065f;
                float b = 0.905f - grain - edgeWear * 0.080f;

                if (x == 0 || y == 0 || x == size - 1 || y == size - 1)
                {
                    r *= 0.65f;
                    g *= 0.63f;
                    b *= 0.60f;
                }

                pixels[y * size + x] = new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, new Vector4(12, 12, 12, 12));
    }

    private static Sprite CreateSoftShadowSprite()
    {
        const int size = 64;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        const float pad = 12f;
        const float half = (size - 1) * 0.5f;
        float innerHalf = half - pad;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x - half) - innerHalf);
                float dy = Mathf.Max(0f, Mathf.Abs(y - half) - innerHalf);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(1f - dist / pad);
                float alpha = t * t * (3f - 2f * t) * 0.52f;
                pixels[y * size + x] = new Color(0.05f, 0.03f, 0.015f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, new Vector4(18, 18, 18, 18));
    }

    private static Sprite CreatePushpinSprite()
    {
        const int size = 64;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size * 0.47f, size * 0.53f);
        Vector2 shadowCenter = new Vector2(size * 0.56f, size * 0.41f);
        float headRadius = size * 0.30f;
        float collarRadius = size * 0.34f;
        float shadowRadius = size * 0.35f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                Color finalColor = new Color(0f, 0f, 0f, 0f);

                // Мека сянка от кабарчето върху хартията.
                float sDist = Vector2.Distance(p, shadowCenter);
                if (sDist < shadowRadius)
                {
                    float st = 1f - sDist / shadowRadius;
                    float sa = st * st * 0.42f;
                    finalColor = new Color(0.06f, 0.04f, 0.02f, sa);
                }

                // Тъмен метален кант/основа под главата на кабарчето.
                float cDist = Vector2.Distance(p, center + new Vector2(1.2f, -1.5f));
                if (cDist < collarRadius)
                {
                    float ca = Mathf.Clamp01((collarRadius - cDist) * 1.4f);
                    Color collar = new Color(0.24f, 0.21f, 0.19f, ca);
                    finalColor = AlphaBlend(finalColor, collar);
                }

                // Сферична глава на кабарчето (неутрално светла база за оцветяване чрез PinTints).
                float hDist = Vector2.Distance(p, center);
                if (hDist < headRadius)
                {
                    float ha = Mathf.Clamp01((headRadius - hDist) * 1.6f);
                    Vector2 n2 = (p - center) / headRadius;
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - Vector2.Dot(n2, n2)));
                    Vector3 normal = new Vector3(n2.x, n2.y, nz).normalized;
                    Vector3 lightDir = new Vector3(-0.48f, 0.56f, 0.68f).normalized;
                    Vector3 halfVec = (lightDir + Vector3.forward).normalized;

                    float ndotl = Mathf.Clamp01(Vector3.Dot(normal, lightDir));
                    float rim = Mathf.Clamp01(1f - nz);
                    float spec = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(normal, halfVec)), 18f);

                    float shade = 0.44f + ndotl * 0.56f - rim * 0.18f;
                    float r = Mathf.Clamp01(shade + spec * 0.55f);
                    float g = Mathf.Clamp01(shade + spec * 0.55f);
                    float b = Mathf.Clamp01(shade + spec * 0.52f);

                    Color head = new Color(r, g, b, ha);
                    finalColor = AlphaBlend(finalColor, head);
                }

                pixels[y * size + x] = finalColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, Vector4.zero);
    }

    private static Sprite CreateButtonSprite()
    {
        const int size = 32;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                bool outer = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                bool innerTop = y == size - 2 && x > 0 && x < size - 1;
                bool innerBottom = y == 1 && x > 0 && x < size - 1;

                float grad = Mathf.Lerp(0.90f, 1.0f, v);
                Color c = new Color(grad, grad, grad, 1f);
                if (outer)
                {
                    c = new Color(0.45f, 0.42f, 0.38f, 1f);
                }
                else if (innerTop)
                {
                    c = new Color(1.0f, 1.0f, 0.98f, 1f);
                }
                else if (innerBottom)
                {
                    c = new Color(0.76f, 0.74f, 0.70f, 1f);
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, new Vector4(6, 6, 6, 6));
    }

    private static Sprite CreateSlotGuideSprite()
    {
        const int size = 64;
        Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
        Color[] pixels = new Color[size * size];

        const int cornerLen = 10;
        const int thickness = 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool nearLeft = x < thickness;
                bool nearRight = x >= size - thickness;
                bool nearBottom = y < thickness;
                bool nearTop = y >= size - thickness;

                bool inCornerX = x < cornerLen || x >= size - cornerLen;
                bool inCornerY = y < cornerLen || y >= size - cornerLen;

                bool draw = ((nearLeft || nearRight) && inCornerY) || ((nearBottom || nearTop) && inCornerX);
                pixels[y * size + x] = draw ? new Color(0.86f, 0.78f, 0.62f, 0.20f) : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return MakeSprite(tex, new Vector4(14, 14, 14, 14));
    }

    private static Color AlphaBlend(Color dst, Color src)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0.0001f) return Color.clear;
        float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;
        return new Color(r, g, b, outA);
    }

    private static float TileNoise(int x, int y, int period, int cellSize, int seed)
    {
        int gridCount = period / cellSize;
        float gx = x / (float)cellSize;
        float gy = y / (float)cellSize;
        int x0 = Mathf.FloorToInt(gx) % gridCount;
        int y0 = Mathf.FloorToInt(gy) % gridCount;
        int x1 = (x0 + 1) % gridCount;
        int y1 = (y0 + 1) % gridCount;
        float tx = gx - Mathf.Floor(gx);
        float ty = gy - Mathf.Floor(gy);
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);

        float v00 = Hash01(x0, y0, seed);
        float v10 = Hash01(x1, y0, seed);
        float v01 = Hash01(x0, y1, seed);
        float v11 = Hash01(x1, y1, seed);
        return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
    }

    private static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0x00FFFFFFu) / 16777215f;
        }
    }
}
