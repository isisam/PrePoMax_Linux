using System;

namespace CaeGlobals
{
    /// <summary>Optional presentation callbacks supplied by the Windows host; no UI or translator dependency.</summary>
    public static class UiLocalization
    {
        public static Func<string, string, string> TranslateText;
        public static Func<string, string> TranslateMessage;
        public static string Text(string source, string context = null)
        { return TranslateText == null ? source : TranslateText(source, context); }
        public static string Message(string source)
        { return TranslateMessage == null ? source : TranslateMessage(source); }
    }
}
