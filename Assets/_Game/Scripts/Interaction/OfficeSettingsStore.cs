using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public sealed class OfficeSettingsData
{
    public int version = 1;
    public bool fullscreen;
    public int quality;
    public float volume = 1f;
    public string[] bindings;

    public OfficeSettingsData Copy() => new OfficeSettingsData
    {
        version = version, fullscreen = fullscreen, quality = quality, volume = volume,
        bindings = (string[])bindings.Clone()
    };
}

// PROTOTYPE CODE: настройки извън game save; Esc остава фиксиран.
public static class OfficeSettingsStore
{
    private const string Key = "office-settings-v1";
    private static readonly string[] DefaultBindings =
        { "<Keyboard>/w", "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d", "<Keyboard>/e" };
    private static OfficeSettingsData current;
    private static OfficeSettingsData defaults;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache()
    {
        current = null;
        defaults = null;
    }

    public static OfficeSettingsData Defaults() => (defaults ??= new OfficeSettingsData
    {
        fullscreen = Screen.fullScreen,
        quality = QualitySettings.GetQualityLevel(),
        volume = 1f,
        bindings = (string[])DefaultBindings.Clone()
    }).Copy();

    public static OfficeSettingsData Current
    {
        get
        {
            if (current == null) Load();
            return current.Copy();
        }
    }

    public static void Load()
    {
        OfficeSettingsData data = Defaults();
        if (PlayerPrefs.HasKey(Key))
        {
            try
            {
                OfficeSettingsData saved = JsonUtility.FromJson<OfficeSettingsData>(PlayerPrefs.GetString(Key));
                if (Valid(saved)) data = saved;
                else Debug.LogWarning("Невалидни Office настройки; използват се стойности по подразбиране.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Office settings load failed: {exception.Message}");
            }
        }
        current = data;
        ApplyDisplay(data);
    }

    public static bool TryApply(OfficeSettingsData data, PlayerInputReader reader, out string message)
    {
        if (!Valid(data) || reader == null)
        {
            message = "Настройките са невалидни.";
            return false;
        }
        OfficeSettingsData previous = Current;
        try
        {
            ApplyDisplay(data);
            reader.ApplyBindings(data.bindings);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            current = data.Copy();
            message = "Настройките са приложени.";
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Office settings save failed: {exception.Message}");
            ApplyDisplay(previous);
            reader.ApplyBindings(previous.bindings);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(previous));
            message = "Настройките не можаха да бъдат записани.";
            return false;
        }
    }

    public static void ApplyBindings(PlayerInputReader reader)
    {
        if (current == null) Load();
        reader.ApplyBindings(current.bindings);
    }

    public static bool Valid(OfficeSettingsData data)
    {
        if (data == null || data.version != 1 || data.quality < 0
            || data.quality >= QualitySettings.names.Length || float.IsNaN(data.volume)
            || data.volume < 0f || data.volume > 1f || data.bindings == null
            || data.bindings.Length != 5) return false;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in data.bindings)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("<Keyboard>/", StringComparison.Ordinal)
                || path.Length <= "<Keyboard>/".Length
                || path.Equals("<Keyboard>/escape", StringComparison.OrdinalIgnoreCase)
                || (Keyboard.current != null && !Keyboard.current.allKeys.Any(key =>
                    string.Equals(key.name, path.Substring("<Keyboard>/".Length), StringComparison.OrdinalIgnoreCase)))
                || !used.Add(path)) return false;
        }
        return true;
    }

    private static void ApplyDisplay(OfficeSettingsData data)
    {
        Screen.fullScreen = data.fullscreen;
        QualitySettings.SetQualityLevel(data.quality);
        AudioListener.volume = data.volume;
    }
}
