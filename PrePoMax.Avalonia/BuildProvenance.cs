// SPDX-License-Identifier: GPL-3.0-or-later
namespace PrePoMax.Avalonia;
internal static class BuildProvenance
{
    public static string Header
    {
        get
        {
            string file=Path.Combine(AppContext.BaseDirectory,"SOURCE_REVISION.txt");
            var lines=File.Exists(file)?File.ReadAllLines(file):[];
            return "git rev: "+(lines.Length>0?lines[0]:"unpackaged build; see execution harness")+Environment.NewLine+
                "git status --short:"+Environment.NewLine+string.Join(Environment.NewLine,lines.Skip(1))+Environment.NewLine;
        }
    }
}
