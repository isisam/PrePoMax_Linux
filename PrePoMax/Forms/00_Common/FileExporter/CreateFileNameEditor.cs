using System;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
namespace PrePoMax
{
    public class CreateFileNameEditor : UITypeEditor
    {
        public static string FileName;
        public static string Filter { get { return ViewModelHost.ExportFileFilter; } set { ViewModelHost.ExportFileFilter = value; } }
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService =
                (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            //
            if (editorService != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    if (value == null || (value is string stringValue && stringValue == "")) value = FileName;
                    //
                    saveFileDialog.FileName = Path.GetFileName(value as string);
                    saveFileDialog.InitialDirectory = Path.GetDirectoryName(value as string);
                    saveFileDialog.Filter = Filter;
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return saveFileDialog.FileName;
                    }
                }
            }
            //
            return value;
        }
    }

}
