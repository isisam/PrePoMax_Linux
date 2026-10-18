using PrePoMax.Forms;
// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ViewExportFileProperties
    {
        // Variables                                                                                                                
        protected DynamicCustomTypeDescriptor _dctd = null;
        protected ExportFileProperties _properties;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "File name")]
        [DescriptionAttribute("Select the file name for the exported file.")]
        [Editor("PrePoMax.CreateFileNameEditor, PrePoMax", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Id(1, 1)]
        public string FileName { get { return _properties.FileName; } set { _properties.FileName = value; } }


        // Constructors                                                                                                             
        public ViewExportFileProperties(ExportFileProperties exportFileProperties)
        {
            _properties = exportFileProperties;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public ExportFileProperties GetBase()
        {
            return _properties;
        }
        public string GetFilter()
        {
            return ViewModelHost.ExportFileFilter;
        }
    }
}

