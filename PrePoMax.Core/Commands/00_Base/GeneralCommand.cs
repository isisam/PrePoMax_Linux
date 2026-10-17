// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax.Commands
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    public abstract class GeneralCommand : Command
    {
        // Constructor                                                                                                              
        public GeneralCommand(string name)
            : base(name)
        {
        }
    }
}
