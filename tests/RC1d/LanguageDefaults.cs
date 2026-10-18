using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using PrePoMax;

// Standalone net48 unit checks against the actual delivered core assembly.
class LanguageDefaults
{
    static int count;
    static void Equal(string actual, string expected, string label)
    {
        if (actual != expected) throw new Exception(label + ": " + actual);
        Console.WriteLine("PASS " + label);
        count++;
    }
    static GeneralSettings Reload(GeneralSettings settings)
    {
        using (var stream = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, settings);
            stream.Position = 0;
            return (GeneralSettings)formatter.Deserialize(stream);
        }
    }
    static void Main()
    {
        var original = Thread.CurrentThread.CurrentUICulture;
        var numeric = Thread.CurrentThread.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "zh-TW", "en-US", "zh-HK", "zh-Hant", "zh-CN", "ja-JP" })
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                string expected = culture == "zh-TW" || culture == "zh-HK" || culture == "zh-Hant" ? "zh-TW" : "en";
                var fresh = new GeneralSettings();
                Equal(fresh.Language, expected, "fresh " + culture);
                fresh.Language = "en";
                fresh.Reset();
                Equal(fresh.Language, expected, "reset " + culture);
                typeof(GeneralSettings).GetField("_language", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(fresh, null);
                Equal(Reload(fresh).Language, expected, "legacy missing language " + culture);
            }
            foreach (string language in new[] { "en", "zh-TW" })
            {
                var saved = new GeneralSettings { Language = language };
                foreach (string culture in new[] { "zh-TW", "en-US" })
                {
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                    Equal(Reload(saved).Language, language, "saved " + language + " on " + culture);
                }
            }
            Equal(Thread.CurrentThread.CurrentCulture.Name, numeric.Name, "numeric culture unchanged");
            Console.WriteLine("PASS " + count + " language unit checks");
        }
        finally { Thread.CurrentThread.CurrentUICulture = original; }
    }
}
