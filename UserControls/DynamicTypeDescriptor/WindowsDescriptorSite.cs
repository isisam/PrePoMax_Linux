// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections;
using System.ComponentModel;
using System.Drawing.Design;
namespace DynamicTypeDescriptor
{
    public sealed class WindowsDescriptorSite : ISite
    {
        private readonly SimpleSite site = new SimpleSite();
        public WindowsDescriptorSite(DynamicCustomTypeDescriptor descriptor)
        {
            IPropertyValueUIService service = new PropertyValueUIService();
            service.AddPropertyValueUIHandler((context, property, items) => {
                if (property is CustomPropertyDescriptor custom) items.AddRange((ICollection)custom.StateItems);
            });
            site.AddService(service);
        }
        public object GetService(System.Type type) => ((System.IServiceProvider)site).GetService(type);
        public IComponent Component => site.Component;
        public IContainer Container => ((ISite)site).Container;
        public bool DesignMode => site.DesignMode;
        public string Name { get => site.Name; set => site.Name = value; }
    }
}
