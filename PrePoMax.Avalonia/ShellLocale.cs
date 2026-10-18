// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
namespace PrePoMax.Avalonia;
internal static class ShellLocale
{
    // Byte-identical rcm/l10n catalogue (84d8eff7); no WinForms/System.Web code is imported.
    private static readonly Dictionary<string,string> catalogue=JsonSerializer.Deserialize<Dictionary<string,string>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"lang","zh-TW.json")))!;
    public static bool Chinese {get;set;}
    public static string Text(string value)=>Chinese&&catalogue.TryGetValue(value,out var translated)?translated:value;
}
