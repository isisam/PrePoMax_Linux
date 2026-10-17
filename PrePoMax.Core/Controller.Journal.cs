using System;
using System.IO;
using System.Linq;
using PrePoMax.Python.Journal;

namespace PrePoMax
{
    public partial class Controller
    {
        // Compressed PMX bytes are kept unchanged: Save must not turn the baseline into the
        // final state. Transient session state, deliberately absent from the PMX schema.
        [NonSerialized] private byte[] _journalBaseline;
        [NonSerialized] private string _journalBaselineName;
        [NonSerialized] private int _journalBaselineCommands;

        private void CaptureJournalBaseline(string fileName)
        {
            _journalBaseline = File.ReadAllBytes(fileName);
            _journalBaselineName = Path.GetFileName(fileName);
            _journalBaselineCommands = GetActiveCommandCount();
        }

        public PythonJournalReport ExportSessionJournal(string fileName)
        {
            var commands = GetActiveCommands();
            if (_journalBaseline == null)
                return PythonJournalExporter.Export(commands, fileName, OpenedFileName,
                    Settings.General.NumOfSplitFaces, Settings.GetWorkDirectory());

            if (commands.Count < _journalBaselineCommands)
                throw new InvalidOperationException("Undo crossed the journal baseline. Reopen the desired model before exporting its session.");
            string baselinePath = Path.ChangeExtension(fileName, ".baseline.pmx");
            // Both files travel together; replay verifies the bytes before replacing its model.
            File.WriteAllBytes(baselinePath, _journalBaseline);
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(_journalBaseline)).Replace("-", "").ToLowerInvariant();
            PythonJournalReport report;
            string text = PythonJournalExporter.GetText(commands.Skip(_journalBaselineCommands).ToList(), fileName,
                OpenedFileName, out report, Settings.General.NumOfSplitFaces, Settings.GetWorkDirectory());
            string prefix = "# PPM-JOURNAL-BASELINE file " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(baselinePath))) + " " + hash + "\n" +
                "# Journal baseline: named fixture " + _journalBaselineName + "\n" +
                "# Baseline history commands skipped: " + _journalBaselineCommands + "\n" +
                "# The host verifies and opens this immutable baseline BEFORE recording the session edits.\n";
            // GetText's empty-model preamble belongs to full history exports, not this delta.
            text = text.Replace(PythonJournalExporter.EmptyBaselinePreamble, "");
            File.WriteAllText(fileName, prefix + text, new System.Text.UTF8Encoding(false));
            return report;
        }

        private string PrepareJournalReplay(string script, string fileName)
        {
            const string marker = "# PPM-JOURNAL-BASELINE ";
            if (script == null || !script.StartsWith(marker, StringComparison.Ordinal)) return script;
            if (_commands.IsCommandRecordingSuppressed)
                throw new InvalidOperationException("A session journal must be run as a top-level script, not inside a recorded script.");
            int end = script.IndexOf('\n');
            string[] parts = script.Substring(marker.Length, (end < 0 ? script.Length : end) - marker.Length).Trim().Split(' ');
            if (parts.Length == 1 && parts[0] == "empty") New();
            else if (parts.Length == 3 && parts[0] == "file")
            {
                if (fileName == null) throw new InvalidOperationException("Run this journal from its Python file so its baseline can be resolved.");
                string baselinePath = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                if (!Path.IsPathRooted(baselinePath)) baselinePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(fileName)), baselinePath);
                string hash;
                using (var sha = System.Security.Cryptography.SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(baselinePath))).Replace("-", "").ToLowerInvariant();
                if (hash != parts[2]) throw new InvalidDataException("Journal baseline SHA256 mismatch.");
                // Verify first: a missing/corrupt baseline leaves the current model untouched.
                Open(baselinePath);
            }
            else throw new InvalidDataException("Unrecognized journal baseline metadata.");
            return end < 0 ? "" : script.Substring(end + 1);
        }
    }
}
