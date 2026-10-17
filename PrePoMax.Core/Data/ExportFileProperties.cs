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
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    public class ExportFileProperties
    {
        // Variables                                                                                                                
        private string _fileName;
        private string _filter;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public string Filter { get { return _filter; } set { _filter = value; } }


        // Constructors                                                                                                             
        public ExportFileProperties(string fileName, string filter)
        {
            _fileName = fileName;
            _filter = filter;
        }


        // Methods                                                                                                                  
        
    }
}

