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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ViewExport3mfFileProperties : ViewExportFileProperties
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(1, 10, "Edge thickness")]
        [Description("Enter the edge thickness ratio.")]
        [Id(2, 1)]
        public double EdgeThicknessRatio
        {
            get { return (_properties as Export3mfFileProperties).EdgeThicknessRatio; }
            set { (_properties as Export3mfFileProperties).EdgeThicknessRatio = value; }
        }


        // Constructors                                                                                                             
        public ViewExport3mfFileProperties(Export3mfFileProperties properties)
            : base(properties)
        {
        }


        // Methods                                                                                                                  


    }
}

