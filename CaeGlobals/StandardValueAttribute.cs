// Extracted VERBATIM from CaeGlobals/DynamicTypeDescriptor/DynamicTypeDescriptor.cs (class StandardValueAttribute).
// The descriptor engine now lives in PrePoMax.PropertyMetadata. Keep this enum
// attribute in CaeGlobals on both targets to preserve its identity without a cycle.
using System;
using System.Collections;
using System.Reflection;

namespace DynamicTypeDescriptor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class StandardValueAttribute : Attribute
    {
        //public StandardValueAttribute()
        //{

        //}

        public StandardValueAttribute(object value)
        {
            m_Value = value;
        }
        public StandardValueAttribute(object value, string displayName)
        {
            m_DisplayName = displayName;
            m_Value = value;
        }
        public StandardValueAttribute(string displayName, string description)
        {
            m_DisplayName = displayName;
            m_Description = description;
        }
        private string m_DisplayName = String.Empty;
        public string DisplayName
        {
            get
            {
                if (String.IsNullOrEmpty(m_DisplayName))
                {
                    if (Value != null)
                    {
                        return Value.ToString();
                    }
                }
                return m_DisplayName;
            }
            set
            {
                m_DisplayName = value;
            }
        }

        private bool m_Visible = true;
        public bool Visible
        {
            get
            {
                return m_Visible;
            }
            set
            {
                m_Visible = value;
            }
        }

        private bool m_Enabled = true;
        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }
            set
            {
                m_Enabled = value;
            }
        }

        private string m_Description = String.Empty;
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
            }
        }

        internal object m_Value = null;

        public object Value
        {
            get
            {
                return m_Value;
            }
        }
        public override string ToString()
        {
            return DisplayName;
        }
        internal static StandardValueAttribute[] GetEnumItems(Type enumType)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException("'enumInstance' is null.");
            }

            if (!enumType.IsEnum)
            {
                throw new ArgumentException("'enumInstance' is not Enum type.");
            }

            ArrayList arrAttr = new ArrayList();
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fields)
            {
                StandardValueAttribute[] attr = fi.GetCustomAttributes(typeof(StandardValueAttribute), false) as StandardValueAttribute[];

                if (attr != null && attr.Length > 0)
                {
                    attr[0].m_Value = fi.GetValue(null);
                    arrAttr.Add(attr[0]);
                }
                else
                {
                    StandardValueAttribute atr = new StandardValueAttribute(fi.GetValue(null));
                    arrAttr.Add(atr);
                }
            }
            StandardValueAttribute[] retAttr = arrAttr.ToArray(typeof(StandardValueAttribute)) as StandardValueAttribute[];
            return retAttr;
        }



    }
}
