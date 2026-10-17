// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using CaeModel;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    class CDuplicateCompositeLayups : PreprocessCommand
    {
        private readonly string[] _names;
        public CDuplicateCompositeLayups(string[] names) : base("Duplicate composite layups") { _names = (string[])names.Clone(); }
        public override bool Execute(Controller receiver) { receiver.DuplicateCompositeLayups(_names); return true; }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Duplicate(writer, "pmx.model.composite_layups", _names);
        }
        public override string GetCommandString() { return base.GetCommandString() + GetArrayAsString(_names); }
    }
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    class CAddCompositeLayup : PreprocessCommand
    {
        private readonly CompositeLayup _layup;
        public CAddCompositeLayup(CompositeLayup layup) : base("Add composite layup") { _layup = layup.DeepClone(); }
        public override bool Execute(Controller receiver) { receiver.AddCompositeLayup(_layup.DeepClone()); return true; }
        public override void ToPython(PythonJournalWriter writer) { PythonCompositeLayupEmitter.Emit(writer, null, _layup); }
        public override string GetCommandString() { return base.GetCommandString() + _layup.Name; }
    }
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    class CReplaceCompositeLayup : PreprocessCommand
    {
        private readonly string _oldName;
        private readonly CompositeLayup _layup;
        public CReplaceCompositeLayup(string oldName, CompositeLayup layup) : base("Edit composite layup")
        {
            _oldName = oldName;
            _layup = layup.DeepClone();
        }
        public override bool Execute(Controller receiver) { receiver.ReplaceCompositeLayup(_oldName, _layup.DeepClone()); return true; }
        public override void ToPython(PythonJournalWriter writer) { PythonCompositeLayupEmitter.Emit(writer, _oldName, _layup); }
        public override string GetCommandString() { return base.GetCommandString() + _oldName + ", " + _layup.Name; }
    }
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    class CRemoveCompositeLayup : PreprocessCommand
    {
        private readonly string _name;
        public CRemoveCompositeLayup(string name) : base("Remove composite layup") { _name = name; }
        public override bool Execute(Controller receiver) { receiver.RemoveCompositeLayup(_name); return true; }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.composite_layups", "remove", PythonValueFormatter.Text(_name)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString() { return base.GetCommandString() + _name; }
    }
}
