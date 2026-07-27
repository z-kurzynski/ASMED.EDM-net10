using Microsoft.Win32;

namespace ASMED.EDM.Core.Helpers;

/// <summary>
/// Helper do zarządzania konfiguracją aplikacji w rejestrze Windows.
/// HKEY_CURRENT_USER\Software\ASMED\EDM
/// </summary>
public static class RegistryConfigHelper
{
    private const string RegistryKeyPath = @"Software\ASMED\EDM";

    // Klucze konfiguracji MySQL
    public const string KeyMySqlPrimaryConnection = "MySqlPrimaryConnection";
    public const string KeyMySqlBackupConnection = "MySqlBackupConnection";
    public const string KeyMySqlLocalConnection = "MySqlLocalConnection";
    public const string KeyActiveConnection = "ActiveConnection"; // "Primary" | "Backup" | "Local"
    public const string KeyEnableFailover = "EnableFailover"; // "true" | "false"
    public const string KeyConnectionTimeout = "ConnectionTimeout"; // sekundy (default: 5)

    /// <summary>
    /// Odczytuje wartość z rejestru.
    /// </summary>
    public static string? GetValue(string keyName, string? defaultValue = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            return key?.GetValue(keyName)?.ToString() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Zapisuje wartość do rejestru.
    /// </summary>
    public static void SetValue(string keyName, string? value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            if (value != null)
                key.SetValue(keyName, value);
            else
                key.DeleteValue(keyName, throwOnMissingValue: false);
        }
        catch
        {
            // Silent fail - Registry może być niedostępny (brak uprawnień, non-Windows, etc.)
        }
    }

    /// <summary>
    /// Usuwa klucz z rejestru.
    /// </summary>
    public static void DeleteValue(string keyName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
            key?.DeleteValue(keyName, throwOnMissingValue: false);
        }
        catch
        {
            // Silent fail
        }
    }

    /// <summary>
    /// Odczytuje wartość bool z rejestru.
    /// </summary>
    public static bool GetBoolValue(string keyName, bool defaultValue)
    {
        var val = GetValue(keyName, defaultValue ? "true" : "false");
        return val?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
               val == "1";
    }

    /// <summary>
    /// Zapisuje wartość bool do rejestru.
    /// </summary>
    public static void SetBoolValue(string keyName, bool value)
    {
        SetValue(keyName, value ? "true" : "false");
    }

    /// <summary>
    /// Odczytuje wartość int z rejestru.
    /// </summary>
    public static int GetIntValue(string keyName, int defaultValue)
    {
        var val = GetValue(keyName, defaultValue.ToString());
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Zapisuje wartość int do rejestru.
    /// </summary>
    public static void SetIntValue(string keyName, int value)
    {
        SetValue(keyName, value.ToString());
    }

    /// <summary>
    /// Czyści całą sekcję rejestru ASMED\EDM (np. reset do defaults).
    /// </summary>
    public static void ClearAll()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Silent fail
        }
    }
}
