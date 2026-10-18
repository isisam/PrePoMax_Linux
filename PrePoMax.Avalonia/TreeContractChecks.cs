// SPDX-License-Identifier: GPL-3.0-or-later
using CaeGlobals;
using System.Text.Json;

namespace PrePoMax.Avalonia;
internal static class TreeContractChecks
{
    public static void Run(string fixture,string output)
    {
        SettingsContainer.IsSaveDisabled=true;
        var records=new List<object>();
        Controller Open(){var c=new Controller(new NullMainView());c.RegenerationWorkDirectory=Path.GetDirectoryName(Path.GetFullPath(fixture));c.Open(Path.GetFullPath(fixture));return c;}
        void Check(bool value,string message){if(!value)throw new Exception(message);}
        var initial=Open();
        foreach(var view in Enum.GetValues<ViewGeometryModelResults>())
        {
            var entries=ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(initial,view)).ToArray();
            Check(entries.Select(e=>e.Key).Distinct().Count()==entries.Length,"Tree keys must be unique: "+view);
            Check(entries.All(e=>e.Entity?.Internal!=true),"Internal entities must be filtered");
            records.Add(new{view=view.ToString(),entries=entries.Select(e=>new{e.Key,e.Kind,e.Caption})});
        }
        foreach(string kind in new[]{"Material","Section","CompositeLayup","NodeSet","ElementSet","Surface","Step","BoundaryCondition","Load"})
        {
            var c=Open();
            var entry=ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(c,ViewGeometryModelResults.Model)).FirstOrDefault(e=>e.Kind==kind&&e.Entity!=null);
            if(entry==null){records.Add(new{kind,skipped="fixture has no entity"});continue;}
            if(ModelTreeSnapshot.CanExecute(entry,"Duplicate"))
            {
                var oldKeys=ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(c,ViewGeometryModelResults.Model)).Select(e=>e.Key).ToHashSet();
                int before=c.GetActiveCommandCount();ModelTreeSnapshot.Execute(c,entry,"Duplicate");
                Check(c.GetActiveCommandCount()==before+1,"Duplicate must record one command: "+kind);
                var duplicates=ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(c,ViewGeometryModelResults.Model)).Where(e=>e.Kind==kind&&e.Entity!=null&&(kind=="Step"||e.Step==entry.Step)).ToArray();
                var added=duplicates.Where(e=>!oldKeys.Contains(e.Key)).ToArray();
                Check(added.Length==1,"Duplicate must create one entity in the same category: "+kind);
                var copy=added.Single();
                ModelTreeSnapshot.Execute(c,copy,"Delete");
                Check(!ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(c,ViewGeometryModelResults.Model)).Any(e=>e.Key==copy.Key),"Delete not in model: "+kind);
            }
            if(ModelTreeSnapshot.CanExecute(entry,"Deactivate"))
            {ModelTreeSnapshot.Execute(c,entry,"Deactivate");Check(!entry.Entity!.Active,"Deactivate");ModelTreeSnapshot.Execute(c,entry,"Activate");Check(entry.Entity.Active,"Activate");}
            Check(c.GetCommandCollectionErrors()?.Count is null or 0,"Command errors: "+kind);
            records.Add(new{kind,passed=true});
        }
        var part=ModelTreeSnapshot.Flatten(ModelTreeSnapshot.Build(initial,ViewGeometryModelResults.Model)).First(e=>e.Kind=="Part"&&e.Entity!=null);
        ModelTreeSnapshot.Execute(initial,part,"Hide");Check(!part.Entity!.Visible,"Hide must persist in model");
        ModelTreeSnapshot.Execute(initial,part,"Show");Check(part.Entity.Visible,"Show must persist in model");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS model tree identities, commands, entity state and internal filtering");
    }
}
