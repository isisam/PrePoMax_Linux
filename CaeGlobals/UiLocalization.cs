using System;

namespace CaeGlobals
{
    /// <summary>Optional presentation callbacks supplied by the Windows host; no UI or translator dependency.</summary>
    public static class UiLocalization
    {
        public static Func<string, string, string> TranslateText;
        public static Func<string, string> TranslateMessage;
        public static Action<string> SetLanguage;
        public static string DefaultLanguage
        {
            get
            {
                string name = System.Globalization.CultureInfo.CurrentUICulture.Name;
                return name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase) ? "zh-TW" : "en";
            }
        }
        public static string Text(string source, string context = null)
        { return TranslateText == null ? source : TranslateText(source, context); }
        public static string Message(string source)
        { return TranslateMessage == null ? source : TranslateMessage(source); }
    }
}
