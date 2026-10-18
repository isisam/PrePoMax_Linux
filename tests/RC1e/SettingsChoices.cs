using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RC1e.Tests {
public static class SettingsChoices
{
    private sealed class Context : ITypeDescriptorContext
    {
        public object Instance { get; set; }
        public PropertyDescriptor PropertyDescriptor { get; set; }
        public IContainer Container { get { return null; } }
        public object GetService(Type type) { return null; }
        public bool OnComponentChanging() { return true; }
        public void OnComponentChanged() { }
    }
    public static string Check(object view)
    {
        var rows = new List<object>();
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(view))
        {
            var context = new Context { Instance = view, PropertyDescriptor = property };
            var converter = property.Converter;
            if (!converter.GetStandardValuesSupported(context)) continue;
            var choices = converter.GetStandardValues(context);
            if (choices == null) continue;
            foreach (object choice in choices)
            {
                // The dynamic descriptor returns identity-bearing StandardValueAttribute objects.
                var valueProperty = choice.GetType().GetProperty("Value");
                object value = valueProperty == null ? choice : valueProperty.GetValue(choice, null);
                string text = converter.ConvertToString(context, CultureInfo.InvariantCulture, value);
                object parsed = converter.ConvertFromString(context, CultureInfo.InvariantCulture, text);
                if (!object.Equals(value, parsed))
                    throw new Exception(view.GetType().Name + "." + property.Name + ": " + text + " lost choice identity");
                rows.Add(new { property = property.Name, identity = value.ToString(), display = text });
            }
        }
        return new JavaScriptSerializer().Serialize(rows);
    }
}
}
