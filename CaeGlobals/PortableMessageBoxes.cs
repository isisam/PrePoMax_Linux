using System;
using System.Collections.Generic;
using CaeGlobals;

namespace CaeGlobals
{
    public enum DialogAnswer
    {
        None,
        OK,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No
    }

    public enum DialogButtons
    {
        OK,
        OKCancel,
        AbortRetryIgnore,
        YesNoCancel,
        YesNo,
        RetryCancel
    }

    public enum DialogIcon
    {
        None = 0,
        Error = 16,
        Question = 32,
        Warning = 48,
        Information = 64
    }

    public interface IDialogService
    {
        MessageBoxModeEnum Mode { get; set; }

        bool IsNonInteractiveThread { get; set; }

        Action<string> WriteDataToOutput { get; set; }

        void ClearReportedMessages();
        string[] GetReportedMessages();
        DialogAnswer Show(string text, string caption, DialogButtons buttons, DialogIcon icon);
    }

    public static class PortableMessageBoxes
    {
        public static IDialogService Service { get; set; } = new HeadlessDialogService();

        public static MessageBoxModeEnum Mode
        {
            get
            {
                return Service.Mode;
            }

            set
            {
                Service.Mode = value;
            }
        }

        public static bool IsNonInteractiveThread
        {
            get
            {
                return Service.IsNonInteractiveThread;
            }

            set
            {
                Service.IsNonInteractiveThread = value;
            }
        }

        public static Action<string> WriteDataToOutput
        {
            get
            {
                return Service.WriteDataToOutput;
            }

            set
            {
                Service.WriteDataToOutput = value;
            }
        }

        public static void ClearReportedMessages()
        {
            Service.ClearReportedMessages();
        }

        public static string[] GetReportedMessages()
        {
            return Service.GetReportedMessages();
        }

        public static DialogAnswer Show(string text, string caption = "", DialogButtons buttons = DialogButtons.OK, DialogIcon icon = DialogIcon.None)
        {
            return Service.Show(text, caption, buttons, icon);
        }

        public static void ShowInfo(string text, string caption = "")
        {
            Show(text, caption);
        }

        public static void ShowWarning(string text)
        {
            Show(text, "Warning");
        }

        public static void ShowError(string text)
        {
            Show(text, "Error");
        }

        public static void ShowWorkDirectoryError()
        {
            ShowError("The work directory does not exist.");
        }

        public static DialogAnswer ShowWarningQuestionOKCancel(string text)
        {
            return Show(text, "Warning", DialogButtons.OKCancel, DialogIcon.Question);
        }

        public static DialogAnswer ShowWarningQuestionYesNo(string text)
        {
            return Show(text, "Warning", DialogButtons.YesNo, DialogIcon.Question);
        }

        public static DialogAnswer ShowWarningQuestionYesNoCancel(string text)
        {
            return ShowWarningQuestionYesNoCancel("Warning", text);
        }

        public static DialogAnswer ShowWarningQuestionYesNoCancel(string caption, string text)
        {
            return Show(text, caption, DialogButtons.YesNoCancel, DialogIcon.Question);
        }

        public static DialogAnswer ShowQuestionYesNo(string caption, string text)
        {
            return Show(text, caption, DialogButtons.YesNo, DialogIcon.Question);
        }
    }

    public sealed class HeadlessDialogService : IDialogService
    {
        [ThreadStatic]
        private static bool nonInteractive;
        [ThreadStatic]
        private static List<string> messages;
        public MessageBoxModeEnum Mode { get; set; } = MessageBoxModeEnum.Strict;

        public bool IsNonInteractiveThread
        {
            get
            {
                return nonInteractive;
            }

            set
            {
                nonInteractive = value;
            }
        }

        public Action<string> WriteDataToOutput { get; set; } = Console.Error.WriteLine;

        public void ClearReportedMessages()
        {
            messages = null;
        }

        public string[] GetReportedMessages()
        {
            return messages == null ? Array.Empty<string>() : messages.ToArray();
        }

        public DialogAnswer Show(string text, string caption, DialogButtons buttons, DialogIcon icon)
        {
            WriteDataToOutput?.Invoke(caption + ": " + text);
            if (nonInteractive && buttons == DialogButtons.OK)
                (messages ?? (messages = new List<string>())).Add(caption + ": " + text);
            if (buttons != DialogButtons.OK && (Mode != MessageBoxModeEnum.AutoAnswer || nonInteractive))
                throw new PromptRefusedException(PromptRefusedException.RefusalText + Environment.NewLine + caption + ": " + text);
            return buttons == DialogButtons.YesNo || buttons == DialogButtons.YesNoCancel ? DialogAnswer.Yes : DialogAnswer.OK;
        }
    }
}
