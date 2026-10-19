// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using FileInOut.Output.Calculix;
using PrePoMax;

static partial class SelectionWindows
{
    static int Keywords(string fixture,string output)
    {
        Console.WriteLine("KEYWORDS load Windows assembly");
        var assembly=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"PrePoMax.exe"));
        Console.WriteLine("KEYWORDS open fixture");
        CoreHost.FileReadError=error=>throw new InvalidDataException("Keyword fixture read failed",error);
        var c=new Controller(new NullMainView());c.Open(fixture);
        Console.WriteLine("KEYWORDS construct real editor");
        var type=assembly.GetType("PrePoMax.Forms.FrmCalculixKeywordEditor",true);
        using(var form=(Form)Activator.CreateInstance(type))
        {
            type.GetProperty("Keywords").SetValue(form,c.GetCalculixModelKeywords());
            type.GetProperty("UserKeywords").SetValue(form,c.GetCalculixUserKeywords());
            type.GetProperty("KeywordOverrides").SetValue(form,c.GetCalculixKeywordOverrides());
            object Get(object target,string name)
            {
                var p=target.GetType().GetProperty(name,All);if(p!=null)return p.GetValue(target);
                return target.GetType().GetField(name,All).GetValue(target);
            }
            string LF(string text)=>text.Replace("\r\n","\n").Replace('\r','\n');
            bool prepared=false;
            object Snapshot(bool hide)
            {
                Console.WriteLine("KEYWORDS prepare hide="+hide);
                ((CheckBox)Get(form,"cbHide")).Checked=hide;
                if(!prepared){Call(form,"PrepareForm");prepared=true;}
                Console.WriteLine("KEYWORDS show hide="+hide);form.Show();Application.DoEvents();
                var tree=(TreeView)Get(form,"cltvKeywordsTree");
                object Node(TreeNode node)=>new{caption=node.Text,data=LF((string)Get(node.Tag,"Data")),user=Get(node.Tag,"Keyword") is CalculixUserKeyword,children=node.Nodes.Cast<TreeNode>().Select(Node).ToArray()};
                return new{deck=LF((string)Get(Get(form,"citbInpFile"),"Text")),nodes=tree.Nodes[0].Nodes.Cast<TreeNode>().Select(Node).ToArray()};
            }
            File.WriteAllText(output,Json.Serialize(new{hidden=Snapshot(true),full=Snapshot(false),real_Windows_form=true,private_desktop=true}));
            Console.WriteLine("KEYWORDS complete");
            ((Timer)Get(form,"tUpdate")).Stop();
            void Detach(object control,string eventName,string methodName)
            {
                var e=control.GetType().GetEvent(eventName,All);
                var method=type.GetMethod(methodName,All);
                e.RemoveEventHandler(control,Delegate.CreateDelegate(e.EventHandlerType,form,method));
            }
            var treeControl=(TreeView)Get(form,"cltvKeywordsTree");
            Detach(treeControl,"AfterSelect","cltvKeywordsTree_AfterSelect");
            Detach(treeControl,"AfterCollapse","cltvKeywordsTree_AfterCollapse");
            Detach(treeControl,"AfterExpand","cltvKeywordsTree_AfterExpand");
            Detach(Get(form,"citbKeyword"),"TextChanged","ictbKeyword_TextChanged");
            void Fonts(TreeNode node){node.NodeFont=null;foreach(TreeNode child in node.Nodes)Fonts(child);}
            foreach(TreeNode node in treeControl.Nodes)Fonts(node);
            form.Hide();Console.WriteLine("KEYWORDS hidden");
        }
        Console.WriteLine("KEYWORDS disposed");Application.Exit();Console.WriteLine("KEYWORDS exit");
        return 0;
    }
}
