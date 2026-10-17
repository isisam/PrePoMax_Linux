using System;
using System.Windows.Forms;
using CaeGlobals;

namespace PrePoMax
{
    internal sealed class WindowsDialogService : IDialogService
    {
        public MessageBoxModeEnum Mode
        {
            get
            {
                return MessageBoxes.Mode;
            }

            set
            {
                MessageBoxes.Mode = value;
            }
        }

        public bool IsNonInteractiveThread
        {
            get
            {
                return MessageBoxes.IsNonInteractiveThread;
            }

            set
            {
                MessageBoxes.IsNonInteractiveThread = value;
            }
        }

        public Action<string> WriteDataToOutput
        {
            get
            {
                return MessageBoxes.WriteDataToOutput;
            }

            set
            {
                MessageBoxes.WriteDataToOutput = value;
            }
        }

        public void ClearReportedMessages()
        {
            MessageBoxes.ClearReportedMessages();
        }

        public string[] GetReportedMessages()
        {
            return MessageBoxes.GetReportedMessages();
        }

        public DialogAnswer Show(string text, string caption, DialogButtons buttons, DialogIcon icon)
        {
            return (DialogAnswer)(int)MessageBoxes.Show(text, caption, (MessageBoxButtons)(int)buttons, (MessageBoxIcon)(int)icon);
        }
    }
}
