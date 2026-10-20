using NCalc;
using System.Globalization;
var cases = new (string, double)[] {
    ("2 + 3 * 4",14), ("(2 + 3) * 4",20), ("Pow(2, 3)",8),
    ("Sqrt(81)",9), ("Abs(-12)",12), ("Sin(0)",0),
    ("if(3 > 2, 7, 9)",7), ("Min(3, 4) + Max(2, 8)",11),
    ("1.25e2 / 5",25), ("-3 + 10 / 2",2)
};
foreach(var (text,expected) in cases) {
    var actual=Convert.ToDouble(new Expression(text).Evaluate(),CultureInfo.InvariantCulture);
    if(Math.Abs(actual-expected)>1e-12)throw new Exception(text+" => "+actual);
}
var variable=new Expression("[x] * 2 + [y]");variable.Parameters["x"]=3.5;variable.Parameters["y"]=4;
if(Convert.ToDouble(variable.Evaluate())!=11)throw new Exception("parameters");
if(!Convert.ToBoolean(new Expression("3 > 2 and 1 < 4").Evaluate()))throw new Exception("boolean");
bool rejected=false;try{new Expression("1 + (").Evaluate();}catch{rejected=true;}
if(!rejected)throw new Exception("invalid expression must fail");
Console.WriteLine("PASS 13 NCalc / public ANTLR arithmetic, functions, parameters, booleans and parse-error checks");
bool stub=false;try{GmshCommon.Gmsh.Initialize();}catch(NotSupportedException){stub=true;}
if(!stub)throw new Exception("Public Gmsh stub must refuse meshing");
Console.WriteLine("PASS Gmsh public stub refuses unsupported operations");
