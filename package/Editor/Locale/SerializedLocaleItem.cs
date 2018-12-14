using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Localization;

namespace UnityEditor.Experimental.Localization
{
    class SerializedLocaleItem : TreeViewItem
    {
        SerializedObject m_SerializedObject;

       
        public SerializedObject serializedObject
        {
            get
            {
                if(m_SerializedObject == null && reference != null)
                {
                    m_SerializedObject = new SerializedObject(reference);
                }
                return m_SerializedObject;
            }
        }

        public SerializedProperty property { get; set; }
        public SerializedProperty nameProp { get { return GetProperty(k_NamePropertyName); } }
        public SerializedProperty identifierIdProp { get { return GetProperty(k_IdPropertyName); } }
        public SerializedProperty identifierCodeProp { get { return GetProperty(k_CodePropertyName); } }
        public SerializedProperty fallbackProp { get { return GetProperty(k_FallbackPropertyName); } }

        SerializedProperty GetProperty(string propName)
        {
            if (serializedObject == null)
                return null;
            return serializedObject.FindProperty(propName);
        }

        public Locale reference
        {
            get
            {
                return property.objectReferenceValue as Locale;
            }
            set
            {
                if (property.objectReferenceValue == value)
                    return;
                
                property.objectReferenceValue = value;
                m_SerializedObject = null;
            }
        }

        const string k_NamePropertyName = "m_Name";
        const string k_IdPropertyName = "m_Identifier.m_Id";
        const string k_CodePropertyName = "m_Identifier.m_Code";
        const string k_FallbackPropertyName = "m_Fallback";

        public override string displayName { get { return name + " " + identifierCode + " " + id; } }

        public int identifierId
        {
            get { return identifierIdProp != null ? identifierIdProp.intValue : 0; }
            set
            {
                if (identifierIdProp != null)
                    identifierIdProp.intValue = value;
            }
        }

        public string name
        {
            get { return nameProp != null ? nameProp.stringValue : string.Empty; }
            set
            {
                if(nameProp != null)
                    nameProp.stringValue = value;
            }
        }

        public string identifierCode
        {
            get { return identifierCodeProp != null ? identifierCodeProp.stringValue : string.Empty; }
            set
            {
                if(identifierCodeProp != null)
                    identifierCodeProp.stringValue = value;
            }
        }

        public Locale fallback
        {
            get { return fallbackProp != null ? fallbackProp.objectReferenceValue as Locale : null;  }
            set
            {
                if (fallbackProp != null)
                    fallbackProp.objectReferenceValue = value;
            }
        }

        public SerializedLocaleItem(SerializedProperty prop)
        {
            property = prop;
            reference = prop.objectReferenceValue as Locale;
        }
    }
}
