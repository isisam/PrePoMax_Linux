// SPDX-License-Identifier: GPL-3.0-or-later
using FileInOut.Output;
using FileInOut.Output.Calculix;
using System.Text;

namespace PrePoMax.Avalonia;

/// <summary>Read-only counterpart of FrmCalculixKeywordEditor.PrepareForm.
/// Generated paths precede insertion of user keywords, as on Windows.</summary>
public sealed class KeywordNode(string caption,string data,bool user,bool modified,int[]? generatedPath)
{
    public string Caption {get;}=caption;
    public string Data {get;}=data;
    public bool User {get;}=user;
    public bool Modified {get;}=modified;
    public int[]? GeneratedPath {get;}=generatedPath;
    public List<KeywordNode> Children {get;}=[];
    public int Offset {get;internal set;}
}

public sealed class KeywordDocument
{
    public List<KeywordNode> Nodes {get;}=[];
    public string Text {get;private set;}="";
    public List<string> Warnings {get;}=[];
    public int SuppressedUserKeywords {get;private set;}
    private static string LF(string text)=>text.Replace("\r\n","\n").Replace('\r','\n');
    private static string First(string text)=>LF(text).Split('\n').FirstOrDefault(s=>s.Length>0)??"User keyword";
    public static KeywordDocument Create(Controller controller,bool hide)
    {
        var document=new KeywordDocument();
        var overrides=controller.GetCalculixKeywordOverrides();
        KeywordNode Build(CalculixKeyword keyword,int[]? path)
        {
            var change=path==null?null:overrides?.FirstOrDefault(o=>o!=null&&o.HasSameIndices(path)&&o.OriginalSignature==CalculixFileWriter.GetKeywordSignature(keyword));
            string data=change?.Data??CalculixFileWriter.GetShortKeywordData(keyword,hide);
            if(change?.Disabled==true)
            {
                var lines=LF(data);if(lines.EndsWith('\n'))lines=lines[..^1];
                data=lines.Length==0?"** Disabled by CalculiX keyword editor":string.Join('\n',lines.Split('\n').Select(l=>"**"+l));
            }
            string caption=change==null&&keyword is CalTitle title?title.Title:First(data);
            var node=new KeywordNode(caption,LF(data),keyword is CalculixUserKeyword,change!=null,path);
            for(int i=0;i<keyword.Keywords.Count;i++)node.Children.Add(Build(keyword.Keywords[i],path?.Append(i).ToArray()));
            return node;
        }
        var generated=controller.GetCalculixModelKeywords();
        for(int i=0;i<generated.Count;i++)document.Nodes.Add(Build(generated[i],[i]));
        var users=controller.GetCalculixUserKeywords();
        if(users!=null)foreach(var entry in users)
        {
            if(entry.Value.IsSuppressed){document.SuppressedUserKeywords++;continue;}
            var nodes=document.Nodes;var path=entry.Key;bool valid=path.Length>0;
            for(int i=0;valid&&i<path.Length-1;i++)
            {
                if(path[i]<0||path[i]>=nodes.Count)valid=false;
                else nodes=nodes[path[i]].Children;
            }
            if(!valid||path[^1]<0){document.Warnings.Add("Unresolved user keyword path: "+string.Join("/",path));continue;}
            nodes.Insert(Math.Min(path[^1],nodes.Count),Build(entry.Value,null));
        }
        var text=new StringBuilder();
        void Append(KeywordNode node)
        {
            node.Offset=text.Length;text.Append(node.Data).Append('\n');
            foreach(var child in node.Children)Append(child);
        }
        foreach(var node in document.Nodes)Append(node);
        document.Text=text.ToString();return document;
    }
    public IEnumerable<KeywordNode> Flatten()
    {
        IEnumerable<KeywordNode> Walk(KeywordNode node){yield return node;foreach(var child in node.Children)foreach(var n in Walk(child))yield return n;}
        return Nodes.SelectMany(Walk);
    }
}
