using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace PrePoMax.L10n
{
    /// <summary>Presentation-only translation. Never changes numeric/UI thread culture or model values.</summary>
    public static class Translator
    {
        private static Dictionary<string, string> _catalogue = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly List<KeyValuePair<Regex, string>> _templates = new List<KeyValuePair<Regex, string>>();
        public static string Language { get; private set; } = "en";
        public static string CatalogueError { get; private set; }
        public static string CataloguePath { get; private set; }
        public static event EventHandler LanguageChanged;
        public static bool Enabled { get { return Language == "zh-TW" && _catalogue.Count > 0; } }
        public static string DefaultLanguage
        {
            get
            {
                string name = CultureInfo.CurrentUICulture.Name;
                return name.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("zh-MO", StringComparison.OrdinalIgnoreCase) ? "zh-TW" : "en";
            }
        }
        public static void Initialize(string path = null)
        {
            CataloguePath = path ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang", "zh-TW.json");
            CatalogueError = null;
            _catalogue = new Dictionary<string, string>(StringComparer.Ordinal);
            _templates.Clear();
            try
            {
                if (File.Exists(CataloguePath))
                {
                    var serializer = new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024 };
                    var values = serializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(CataloguePath));
                    if (values == null) { CatalogueError = "Catalogue must be a JSON object."; return; }
                    foreach (var pair in values)
                        if (!string.IsNullOrEmpty(pair.Value)) _catalogue[pair.Key] = pair.Value;
                    // Explicit format templates only; unknown text is never word-substituted at runtime.
                    foreach (var pair in _catalogue)
                    {
                        var tokens = Regex.Matches(pair.Key, @"\{[0-9]+\}");
                        if (tokens.Count == 0) continue;
                        string pattern = Regex.Escape(pair.Key);
                        foreach (Match token in tokens) pattern = pattern.Replace(Regex.Escape(token.Value), "(.+?)");
                        _templates.Add(new KeyValuePair<Regex, string>(
                            new Regex("^" + pattern + "$", RegexOptions.Singleline | RegexOptions.CultureInvariant,
                                TimeSpan.FromMilliseconds(30)), pair.Key));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is ArgumentException || ex is InvalidOperationException)
            {
                _catalogue.Clear(); _templates.Clear(); CatalogueError = ex.Message;
            }
        }
        public static void SetLanguage(string language)
        {
            string next = language == "zh-TW" ? "zh-TW" : language == "en" ? "en" : DefaultLanguage;
            if (Language == next) return;
            Language = next;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
        public static string Translate(string source, string context = null)
        {
            if (!Enabled || string.IsNullOrEmpty(source)) return source;
            string translated;
            if (context != null && _catalogue.TryGetValue(context + "|" + source, out translated)) return translated;
            if (context != null && _catalogue.TryGetValue(context, out translated)) return translated;
            if (_catalogue.TryGetValue(source, out translated)) return translated;
            // Keep descriptor ordering prefixes and menu mnemonics separate from terminology.
            string clean = source.TrimStart('\u200B', '\t');
            if (clean != source) return source.Substring(0, source.Length - clean.Length) + Translate(clean, context);
            if (source.Contains("&"))
            {
                clean = source.Replace("&", "");
                if (_catalogue.TryGetValue(clean, out translated)) return translated;
            }
            return source;
        }
        public static string Message(string source)
        {
            string exact = Translate(source);
            if (!Enabled || exact != source || string.IsNullOrEmpty(source)) return exact;
            foreach (var template in _templates)
            {
                Match match;
                try { match = template.Key.Match(source); } catch (RegexMatchTimeoutException) { continue; }
                if (!match.Success) continue;
                string result = _catalogue[template.Value];
                var tokens = Regex.Matches(template.Value, @"\{[0-9]+\}");
                for (int i = 0; i < tokens.Count; ++i) result = result.Replace(tokens[i].Value, match.Groups[i + 1].Value);
                return result;
            }
            return source;
        }
    }
}
