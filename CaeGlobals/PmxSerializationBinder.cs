using System;

namespace CaeGlobals
{
    /// <summary>Preserves legacy wire identities when implementation types move between assemblies.</summary>
    public sealed class PmxSerializationBinder : RenamedTypeSerializationBinder
    {
        public new static readonly PmxSerializationBinder Instance = new PmxSerializationBinder();
        public override void BindToName(Type type, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = null;
            if (type.FullName == "PrePoMax.WindowState")
            {
                assemblyName = "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                typeName = "System.Windows.Forms.FormWindowState";
                return;
            }

            string simple = type.Assembly.GetName().Name;
            if (simple == "PrePoMax.PropertyMetadata")
            {
                assemblyName = "CaeGlobals, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                typeName = type.FullName;
            }
            else if (simple == "PrePoMax.Core" || simple == "PrePoMax.ViewModels")
            {
                assemblyName = "PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null";
                typeName = LegacyName(type.FullName);
            }
            else if (simple == "PrePoMax.ViewContracts")
            {
                assemblyName = (type.Namespace == "UserControls" ? "UserControls" : "vtkControl") + ", Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                typeName = LegacyName(type.FullName);
            }
            else if (type.FullName != null && (type.FullName.Contains("PrePoMax.Core,") || type.FullName.Contains("PrePoMax.ViewContracts,") || type.FullName.Contains("PrePoMax.ViewModels,") || type.FullName.Contains("PrePoMax.PropertyMetadata,")))
            {
                typeName = LegacyName(type.FullName);
            }
        }

        private static string LegacyName(string name)
        {
            // Generic arguments carry their own assembly identity. ViewContracts DTOs retain their namespaces.
            return System.Text.RegularExpressions.Regex.Replace(name, @"(PrePoMax\.Core|PrePoMax\.ViewContracts|PrePoMax\.ViewModels|PrePoMax\.PropertyMetadata), Version=[0-9.]+, Culture=neutral, PublicKeyToken=null", match =>
            {
                int index = match.Index;
                string prefix = name.Substring(0, index);
                int bracket = prefix.LastIndexOf('[');
                string argument = prefix.Substring(bracket + 1);
                string target = match.Groups[1].Value == "PrePoMax.PropertyMetadata" ? "CaeGlobals" :
                    match.Groups[1].Value == "PrePoMax.Core" || match.Groups[1].Value == "PrePoMax.ViewModels" ? "PrePoMax" : argument.StartsWith("UserControls.", StringComparison.Ordinal) ? "UserControls" : "vtkControl";
                return target + ", Version=" + (target == "PrePoMax" ? "3.0.0.0" : "1.0.0.0") + ", Culture=neutral, PublicKeyToken=null";
            });
        }
    }
}
