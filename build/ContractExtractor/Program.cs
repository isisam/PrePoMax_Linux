using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
if(args[0]=="--forwards") {
 var groups=new Dictionary<string,SortedSet<string>>();
 foreach(var path in args.Skip(1)) {
  var root=CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
  foreach(var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()) {
   if(type.Parent is not NamespaceDeclarationSyntax ns || !type.Modifiers.Any(SyntaxKind.PublicKeyword)) continue;
   string target=Path.GetFullPath(path).Replace('\\','/').Contains("/PrePoMax.Core/") ? "PrePoMax" : ns.Name.ToString()=="UserControls" ? "UserControls" : "vtkControl";
   if(!groups.ContainsKey(target)) groups[target]=new SortedSet<string>();
   var generic=type is TypeDeclarationSyntax td && td.TypeParameterList != null ? "<"+new string(',',td.TypeParameterList.Parameters.Count-1)+">" : "";
   groups[target].Add(ns.Name+"."+type.Identifier.Text+generic);
  }
 }
 foreach(var group in groups) {
  var output="// Legacy Windows assembly entry points; implementation lives in the shared projects.\n"+string.Join("\n",group.Value.Select(name=>"[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(global::"+name+"))]"))+"\n";
  File.WriteAllText(Path.Combine(group.Key,"Properties","ForwardedCoreTypes.cs"),output,new System.Text.UTF8Encoding(false));
  Console.WriteLine(group.Key+" forwards="+group.Value.Count);
 }
 return;
}
if(args[0]=="--forward") {
 foreach(var path in args.Skip(1)) {
  var source=File.ReadAllText(path); var root=CSharpSyntaxTree.ParseText(source).GetRoot();
  var edits=new List<(int index,string text)>();
  foreach(var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()) {
   if(type.AttributeLists.ToString().Contains("TypeForwardedFrom")) continue;
   if(type is not EnumDeclarationSyntax && !type.AttributeLists.ToString().Contains("Serializable")) continue;
   var ns=type.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "";
   string identity=Path.GetFullPath(path).Replace('\\','/').Contains("/PrePoMax.Core/") ? "PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null" : (ns=="UserControls" ? "UserControls" : "vtkControl")+", Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
   int index=type.SpanStart; int lineStart=source.LastIndexOf('\n',Math.Max(0,index-1))+1; string indent=source.Substring(lineStart,index-lineStart);
   edits.Add((index,"[System.Runtime.CompilerServices.TypeForwardedFrom(\""+identity+"\")]\n"+indent));
  }
  foreach(var edit in edits.OrderByDescending(e=>e.index)) source=source.Insert(edit.index,edit.text);
  File.WriteAllText(path,source,new System.Text.UTF8Encoding(false));
 }
 return;
}
if(args[0]=="--format") {
 foreach(var path in args.Skip(1)) File.WriteAllText(path, CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().NormalizeWhitespace().ToFullString()+Environment.NewLine, new System.Text.UTF8Encoding(false));
 return;
}
if(args[0]=="--null") {
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(args[1]));
 var contract=tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single();
 Console.WriteLine("using System;using System.Collections.Generic;using System.Drawing;using CaeGlobals;using CaeMesh;using CaeModel;using CaeResults;using CaeJob;using UserControls;using vtkControl;namespace PrePoMax { public class NullMainView : IMainView {");
 foreach(var member in contract.Members) {
  if(member is PropertyDeclarationSyntax prop) { Console.WriteLine("public "+prop.NormalizeWhitespace().ToString()+ (prop.Identifier.Text=="Viewport" ? " = new NullViewport3D();" : "")); continue; }
  if(member is not MethodDeclarationSyntax m) continue;
  var signature=m.WithAttributeLists(default).WithModifiers(default).WithBody(null).WithExpressionBody(null).WithSemicolonToken(default).NormalizeWhitespace().ToString();
  var name=m.Identifier.Text; var ret=m.ReturnType.ToString(); var body="";
  foreach(var p in m.ParameterList.Parameters.Where(p=>p.Modifiers.Any(SyntaxKind.OutKeyword))) body+=p.Identifier+" = default("+p.Type+"); ";
  var names=string.Join(", ",m.ParameterList.Parameters.Skip(1).Select(p=>p.Identifier.Text));
  if(name=="InvokeIfRequired") body+=(ret=="void" ? "action("+names+");" : "return func();");
  else if(name=="Open") body="ActionOnFile(fileName, parameters);";
  else if(name=="BeginInvoke") body="return System.Threading.Tasks.Task.Run(() => action.DynamicInvoke());";
  else if(name=="WriteDataToOutput") body="Console.WriteLine(data);";
  else if(name=="WriteErrorToOutput") body="Console.Error.WriteLine(data);";
  else if(name=="GetSidePanelsState") body="return new SidePanelsState();";
  else if(name=="GetStableTreeExpandCollapseState") body="return Array.Empty<Dictionary<string,bool>>();";
  else if(name=="GetTreeExpandCollapseState") body="return Array.Empty<bool[]>();";
  else if(name=="GetViewPlaneNormal" || name=="GetClosestUpView") body="return new double[] { 0, 0, 1 };";
  else if(name=="GetDeformationFactor") body="return 1;";
  else if(name=="SaveAutomationScreenshot" || name=="ExportThreeMF" || name=="ShowSplitMeshResults" || name=="EditMeshingParametersFromHistory") body="throw new NotSupportedException(\"This operation requires a graphical host.\");";
  else if(ret!="void") body+="return default("+ret+");";
  Console.WriteLine("public virtual "+signature+" { "+body+" }");
 }
 Console.WriteLine("}}"); return;
}
var list = new List<object>();
foreach(var path in args) {
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(path));
 foreach(var cls in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()) {
 foreach(var m in cls.Members) {
 if(m is MethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.PublicKeyword))
 list.Add(new {path, cls=cls.Identifier.Text, name=method.Identifier.Text, kind="method", ret=method.ReturnType.ToString(), signature=method.WithBody(null).WithExpressionBody(null).WithAttributeLists(default).WithModifiers(default).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).NormalizeWhitespace().ToString(), text=m.ToString(), isStatic=method.Modifiers.Any(SyntaxKind.StaticKeyword)});
 if(m is PropertyDeclarationSyntax prop && prop.Modifiers.Any(SyntaxKind.PublicKeyword))
 list.Add(new {path, cls=cls.Identifier.Text, name=prop.Identifier.Text, kind="property", ret=prop.Type.ToString(), signature=prop.Type+" "+prop.Identifier+" { "+string.Join(" ",prop.AccessorList?.Accessors.Where(a=>!a.Modifiers.Any(SyntaxKind.PrivateKeyword)).Select(a=>a.Keyword+";")??new[]{"get;"})+" }", text=m.ToString(), isStatic=prop.Modifiers.Any(SyntaxKind.StaticKeyword)});
 }
 }
}
Console.Write(JsonSerializer.Serialize(list));
