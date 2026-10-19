// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using PrePoMax;
using PrePoMax.Commands;

static partial class SelectionWindows
{
    static void History(Controller c,string folder)
    {
        var steps=(object[])Json.DeserializeObject(File.ReadAllText(Path.Combine(folder,"history-manifest.json")));
        var results=new List<object>();int index=0;
        foreach(var step in steps.Cast<Dictionary<string,object>>())
        {
            string op=(string)step["operation"];
            if(op=="baseline")c.SaveToPmxCommand(Path.Combine(folder,"baseline-windows.pmx"));
            if(op=="material")
            {
                var name=(string)step["name"];var material=c.Model.Materials[name].DeepClone();
                var elastic=material.Properties.First(p=>p is Elastic||p is ElasticEngineeringConstants||p is ElasticLamina);
                var property=elastic is Elastic?"YoungsPoissonsTemp":elastic is ElasticEngineeringConstants?"EngineeringConstantsTemp":"LaminaTemp";
                var values=(EquationContainer[][])elastic.GetType().GetProperty(property).GetValue(elastic);
                values[0][0].SetEquation((string)step["value"]);
                c.ReplaceMaterialCommand(name,material);
            }
            else if(op=="node-set")
            {
                var selection=(Dictionary<string,object>)step["selection"];
                double[] vector(string key)=>selection[key]==null?null:((object[])selection[key]).Select(Convert.ToDouble).ToArray();
                double[][] planes=((object[])selection["PlaneParameters"]).Cast<object[]>().Select(p=>p.Select(Convert.ToDouble).ToArray()).ToArray();
                c.Selection=new Selection();c.SelectItem=vtkSelectItem.Node;c.SelectBy=vtkSelectBy.Node;
                c.SelectPointOrArea(vector("PickedPoint"),null,planes,Convert.ToBoolean(selection["CompletelyInside"]),vtkSelectOperation.None,c.Model.Mesh.Parts.Values.Where(p=>p.Visible).Select(p=>p.Name).ToArray());
                var expected=((object[])step["ids"]).Select(Convert.ToInt32).OrderBy(n=>n).ToArray();
                var ids=c.GetSelectionIds();
                File.WriteAllText(Path.Combine(folder,"windows-box-selection.json"),Json.Serialize(new{ids,expected}));
                if(!ids.OrderBy(n=>n).SequenceEqual(expected))throw new Exception("Windows box selection differs");
                var set=new FeNodeSet((string)step["name"],ids){CreationData=c.Selection.DeepCopy(true,false),Valid=true};
                c.GetNodesCenterOfGravity(set);c.AddNodeSetCommand(set);
            }
            else if(op=="undo")c.UndoHistory(RegenerateTypeEnum.All);
            else if(op=="redo")c.RedoHistory();
            else if(op!="baseline")throw new Exception("Unknown history operation "+op);
            string target=Path.Combine(folder,"windows-"+index.ToString("000")+".inp");c.ExportToCalculix(target);
            bool same=File.ReadAllBytes(target).SequenceEqual(File.ReadAllBytes(Path.Combine(folder,(string)step["file"])));
            bool count=c.GetActiveCommandCount()==Convert.ToInt32(step["count"]);
            var errors=c.GetCommandCollectionErrors();
            results.Add(new{index,op,byte_identical=same,commandCount=c.GetActiveCommandCount(),count_equal=count,errors});
            File.WriteAllText(Path.Combine(folder,"windows-history-comparison.json"),Json.Serialize(new {passed=results.Count==steps.Length&&same&&count,results,independent_commands=true,private_desktop=true}));
            if(!same||!count||errors?.Count>0)throw new Exception("History differs at "+index+" "+op);
            index++;
        }
    }
}
