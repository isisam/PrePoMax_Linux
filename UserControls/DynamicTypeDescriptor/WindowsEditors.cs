// Windows-only UI adapters for the shared descriptor.
using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using DynamicTypeDescriptor;
namespace DynamicTypeDescriptor
{
    public class StandardValueEditor : UITypeEditor
    {
        private StandardValueEditorUI m_ui = new StandardValueEditorUI();

        public StandardValueEditor()
        {

        }

        public override bool GetPaintValueSupported(System.ComponentModel.ITypeDescriptorContext context)
        {
            return false;
        }

        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }

        public override bool IsDropDownResizable
        {
            get
            {
                return true;
            }
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                IWindowsFormsEditorService editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
                if (editorService == null)
                    return value;

                m_ui.SetData(context, editorService, value);

                editorService.DropDownControl(m_ui);

                value = m_ui.GetValue();

            }

            return value;
        }
    }

    public class PropertyValuePaintEditor : UITypeEditor
    {
        public override bool GetPaintValueSupported(ITypeDescriptorContext context)
        {
            // let the property browser know we'd like
            // to do custom painting.
            if (context != null)
            {
                if (context.PropertyDescriptor != null)
                {
                    if (context.PropertyDescriptor is CustomPropertyDescriptor)
                    {
                        CustomPropertyDescriptor cpd = context.PropertyDescriptor as CustomPropertyDescriptor;
                        return (cpd.ValueImage != null);
                    }
                }
            }
            return base.GetPaintValueSupported(context);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.None;
        }
        public override void PaintValue(PaintValueEventArgs pe)
        {
            if (pe.Context != null)
            {
                if (pe.Context.PropertyDescriptor != null)
                {
                    if (pe.Context.PropertyDescriptor is CustomPropertyDescriptor)
                    {
                        CustomPropertyDescriptor cpd = pe.Context.PropertyDescriptor as CustomPropertyDescriptor;

                        if (cpd.ValueImage != null)
                        {
                            pe.Graphics.DrawImage((System.Drawing.Image)cpd.ValueImage, pe.Bounds);
                            return;
                        }
                    }
                }
            }
            base.PaintValue(pe);
        }

    }

}
