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
using System.IO;
using System.Runtime.Serialization;
using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;

namespace PrePoMax
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("PrePoMax, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null")]
    [Serializable]
    public class SolverSettings : ISettings
    {
        // Variables
        private string _workDirectory;
        private bool _usePmxFolderAsWorkDirectory;
        private int _numCPUs;
        private string _calculixExe;
        private SolverTypeEnum _calculixDefaultSolverType;
        private List<EnvironmentVariable> _environmentVariables;
        private ConvertPyramidsToEnum _convertPyramidsTo;
        private bool _binaryResultsFile;
        private string _abaqusExe;
        private string _femasterExe;
        public static string NonEnglishDirectoryWarning = "The selected work directory path contains non-English characters. " +
                                                          "Some program features might not work as expected.";


        // Properties
        public string WorkDirectoryForSettingsOnly
        {
            get { return Tools.GetGlobalPath(_workDirectory); }
            set
            {
                string path = Tools.GetGlobalPath(value);
                _workDirectory = Tools.GetLocalPath(path);
            }
        }
        public bool UsePmxFolderAsWorkDirectory
        {
            get { return _usePmxFolderAsWorkDirectory; }
            set { _usePmxFolderAsWorkDirectory = value; }
        }
        public int NumCPUs
        {
            get { return _numCPUs; }
            set { _numCPUs = Math.Max(1, value); }
        }
        public string CalculixExe
        {
            get { return Tools.GetGlobalPath(_calculixExe); }
            set { _calculixExe = SetExecutable(value, "CalculiX"); }
        }
        public SolverTypeEnum CalculixDefaultSolverType
        {
            get { return _calculixDefaultSolverType; }
            set { _calculixDefaultSolverType = value; }
        }
        public List<EnvironmentVariable> EnvironmentVariables
        {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }
        public ConvertPyramidsToEnum ConvertPyramidsTo
        {
            get { return _convertPyramidsTo; }
            set { _convertPyramidsTo = value; }
        }
        public bool BinaryResultsFile
        {
            get { return _binaryResultsFile; }
            set { _binaryResultsFile = value; }
        }
        public string AbaqusExe
        {
            get { return Tools.GetGlobalPath(_abaqusExe); }
            set { _abaqusExe = SetExecutable(value, "Abaqus"); }
        }
        public string FEMasterExe
        {
            get { return Tools.GetGlobalPath(_femasterExe); }
            set { _femasterExe = SetExecutable(value, "FEMaster"); }
        }


        // Constructors
        public SolverSettings()
        {
            Reset();
        }


        // Methods
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _workDirectory = CreateDefaultWorkDirectory();
            _usePmxFolderAsWorkDirectory = false;
            _numCPUs = 1;
            _calculixExe = null;
            _calculixDefaultSolverType = SolverTypeEnum.Pardiso;
            _environmentVariables = null;
            _convertPyramidsTo = ConvertPyramidsToEnum.Wedges;
            _binaryResultsFile = false;
            _abaqusExe = null;
            _femasterExe = null;
            SetBundledCalculixDefault(true);
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Migrate unset work directories from earlier first-run settings, while
            // retaining explicitly configured paths and the PMX-folder preference.
            if (string.IsNullOrWhiteSpace(_workDirectory))
                _workDirectory = CreateDefaultWorkDirectory();
            // A saved matrix solver is an existing user choice, including in older settings files.
            SetBundledCalculixDefault(false);
        }
        private static string CreateDefaultWorkDirectory()
        {
            string directory = Path.Combine(CaeGlobals.PlatformServices.Paths.ApplicationDirectory, "Work");
            Directory.CreateDirectory(directory);
            return directory;
        }
        private void SetBundledCalculixDefault(bool resetMatrixSolver)
        {
            if (!string.IsNullOrWhiteSpace(_calculixExe)) return;
            string executable = Tools.GetGlobalPath("#\\Solver\\ccx.exe");
            if (File.Exists(executable))
            {
                _calculixExe = Tools.GetLocalPath(executable);
                if (resetMatrixSolver) _calculixDefaultSolverType = SolverTypeEnum.Spooles;
            }
        }
        private static string SetExecutable(string value, string solverName)
        {
            string path = Tools.GetGlobalPath(value);
            if (!File.Exists(path))
                throw new Exception("The selected " + solverName + " executable does not exist.");
            string extension = Path.GetExtension(path);
            if (extension != ".exe" && extension != ".bat" && extension != ".cmd")
                throw new Exception("The selected executable's file extension is not .exe, .bat or .cmd.");
            return Tools.GetLocalPath(path);
        }
    }
}
