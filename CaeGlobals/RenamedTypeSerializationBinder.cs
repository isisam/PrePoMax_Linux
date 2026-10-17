// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    public class RenamedTypeSerializationBinder : SerializationBinder
    {
        // Variables                                                                                                                
        private static readonly Dictionary<string, string> _renamedTypeNames = GetRenamedTypeNames();
        private static readonly RenamedTypeSerializationBinder _instance = new RenamedTypeSerializationBinder();


        // Properties                                                                                                               
        public static RenamedTypeSerializationBinder Instance { get { return _instance; } }


        // Constructors                                                                                                             
        protected RenamedTypeSerializationBinder()
        {
        }


        // Methods                                                                                                                  
        private static Dictionary<string, string> GetRenamedTypeNames()
        {
            // Add one entry for each renamed or moved serializable type:
            // { "OldNamespace.OldTypeName", "NewNamespace.NewTypeName, NewAssemblyName" }
            return new Dictionary<string, string>()
            {
                { "CaeMesh.Meshing.ElementSizeTypeEnum", "CaeMesh.ElementSizeTypeEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmMesh2DEnum", "CaeMesh.GmshAlgorithmMesh2DEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmMesh3DEnum", "CaeMesh.GmshAlgorithmMesh3DEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshAlgorithmRecombineEnum", "CaeMesh.GmshAlgorithmRecombineEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshCommandEnum", "CaeMesh.GmshCommandEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeFirstOrderShellEnum", "CaeMesh.GmshOptimizeFirstOrderShellEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeFirstOrderSolidEnum", "CaeMesh.GmshOptimizeFirstOrderSolidEnum, CaeMesh" },
                { "CaeMesh.Meshing.GmshOptimizeHighOrderEnum", "CaeMesh.GmshOptimizeHighOrderEnum, CaeMesh" },
                { "CaeMesh.TetrahedralNetgen", "CaeMesh.NetgenMesh, CaeMesh" },
                { "PrePoMax.Commands.CAddTetrahedralNetgenToUncoveredParts",
                  "PrePoMax.Commands.CAddNetgenMeshToUncoveredParts, PrePoMax" },
                { "PrePoMax.SplitPartMeshData", "CaeMesh.SplitPartMeshData, CaeMesh" },
                { "CaeMesh.SurfaceExtraction", "CaeMesh.FaceOffset, CaeMesh" },
                // The extrude mesh item was removed; its selected source surfaces are swept by a sweep mesh item
                { "CaeMesh.ExtrudeMesh", "CaeMesh.SweepMesh, CaeMesh" }
            };
        }
        public override Type BindToType(string assemblyName, string typeName)
        {
#if !NETFRAMEWORK
            if (typeName == "System.UnitySerializationHolder") return typeof(LegacyFormatter.FrameworkTypeReference);
#endif
            if (typeName == null) return null;
            // Old histories persisted the undo/redo event's Controller callback. Callbacks are rebound by
            // the host after loading, so their runtime-specific delegate/method records are only consumed.
            if (typeName.StartsWith("System.DelegateSerializationHolder", StringComparison.Ordinal) ||
                typeName == "System.Reflection.MemberInfoSerializationHolder") return typeof(LegacyCallbackRecord);
            if (typeName == "System.Windows.Forms.FormWindowState") return Resolve("PrePoMax.WindowState, PrePoMax.Core");
            //
            string renamedTypeName;
            if (_renamedTypeNames.TryGetValue(typeName, out renamedTypeName))
                return Resolve(renamedTypeName);
            string genericName = GetRenamedGenericArgumentTypeNames(typeName) ?? typeName;
            return Resolve(genericName + ", " + assemblyName);
        }
        internal static Type Resolve(string qualifiedName)
        {
            return Type.GetType(qualifiedName,
                name => {
                    if (name.Name == "System.Private.CoreLib" || name.Name == "mscorlib") return typeof(object).Assembly;
                    if (name.Name == "System.Drawing.Primitives" || name.Name == "System.Drawing") return typeof(System.Drawing.Color).Assembly;
                    // Never load WinForms/ActiViz just to read a Controller/history/side-panel record.
                    if (name.Name == "PrePoMax") return TryLoad("PrePoMax.Core") ?? TryLoad(name.FullName);
                    if (name.Name == "UserControls" || name.Name == "vtkControl") return TryLoad("PrePoMax.ViewContracts") ?? TryLoad(name.FullName);
                    return TryLoad(name.FullName);
                },
                (assembly, name, ignoreCase) => {
                    Type found = assembly?.GetType(name, false, ignoreCase);
                    if (found != null) return found;
                    foreach (string candidate in new[] { "PrePoMax.Core", "PrePoMax.ViewContracts" }) {
                        found = TryLoad(candidate)?.GetType(name, false, ignoreCase);
                        if (found != null) return found;
                    }
                    return null;
                }, false);
        }
        private static System.Reflection.Assembly TryLoad(string name)
        {
            try { return System.Reflection.Assembly.Load(name); }
            catch (System.IO.FileNotFoundException) { return null; }
        }
        private static string GetRenamedGenericArgumentTypeNames(string typeName)
        {
            string renamedTypeName = typeName;
            //
            foreach (var entry in _renamedTypeNames)
            {
                int index = 0;
                while ((index = renamedTypeName.IndexOf(entry.Key, index, StringComparison.Ordinal)) >= 0)
                {
                    int nameEnd = index + entry.Key.Length;
                    bool isArgumentStart = index > 0 && renamedTypeName[index - 1] == '[';
                    bool isArgumentEnd = nameEnd < renamedTypeName.Length &&
                                         (renamedTypeName[nameEnd] == ',' || renamedTypeName[nameEnd] == ']');
                    //
                    if (isArgumentStart && isArgumentEnd)
                    {
                        int argumentEnd = GetArgumentEnd(renamedTypeName, nameEnd);
                        renamedTypeName = renamedTypeName.Substring(0, index) + entry.Value +
                                          renamedTypeName.Substring(argumentEnd);
                        index += entry.Value.Length;
                    }
                    else index = nameEnd;
                }
            }
            //
            if (renamedTypeName == typeName) return null;
            else return renamedTypeName;
        }
        private static int GetArgumentEnd(string typeName, int start)
        {
            int depth = 0;
            //
            for (int i = start; i < typeName.Length; i++)
            {
                if (typeName[i] == '[') depth++;
                else if (typeName[i] == ']')
                {
                    if (depth == 0) return i;
                    else depth--;
                }
            }
            //
            return typeName.Length;
        }
    }
}
