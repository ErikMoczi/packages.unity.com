
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class ScriptMetadata : IPropertyContainer
    {
        public static ValueClassProperty<ScriptMetadata, bool> SuccessProperty { get; private set; }
        public static ClassListClassProperty<ScriptMetadata, ScriptDiagnostic> DiagnosticsProperty { get; private set; }
        public static ClassListClassProperty<ScriptMetadata, ScriptComponentSystem> SystemsProperty { get; private set; }
        public static ClassListClassProperty<ScriptMetadata, ScriptEntityFilter> FiltersProperty { get; private set; }
        public static ClassListClassProperty<ScriptMetadata, ScriptComponentBehaviour> BehavioursProperty { get; private set; }

        private static ClassPropertyBag<ScriptMetadata> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            SuccessProperty = new ValueClassProperty<ScriptMetadata, bool>(
                "Success"
                ,c => c.m_Success
                ,(c, v) => c.m_Success = v
            );

            DiagnosticsProperty = new ClassListClassProperty<ScriptMetadata, ScriptDiagnostic>(
                "Diagnostics"
                ,c => c.m_Diagnostics
                ,c => new ScriptDiagnostic()
            );

            SystemsProperty = new ClassListClassProperty<ScriptMetadata, ScriptComponentSystem>(
                "Systems"
                ,c => c.m_Systems
                ,c => new ScriptComponentSystem()
            );

            FiltersProperty = new ClassListClassProperty<ScriptMetadata, ScriptEntityFilter>(
                "Filters"
                ,c => c.m_Filters
                ,c => new ScriptEntityFilter()
            );

            BehavioursProperty = new ClassListClassProperty<ScriptMetadata, ScriptComponentBehaviour>(
                "Behaviours"
                ,c => c.m_Behaviours
                ,c => new ScriptComponentBehaviour()
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptMetadata>(
                SuccessProperty,
                DiagnosticsProperty,
                SystemsProperty,
                FiltersProperty,
                BehavioursProperty
            );
        }

        static ScriptMetadata()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private bool m_Success;
        private readonly List<ScriptDiagnostic> m_Diagnostics = new List<ScriptDiagnostic>();
        private readonly List<ScriptComponentSystem> m_Systems = new List<ScriptComponentSystem>();
        private readonly List<ScriptEntityFilter> m_Filters = new List<ScriptEntityFilter>();
        private readonly List<ScriptComponentBehaviour> m_Behaviours = new List<ScriptComponentBehaviour>();

        public bool Success
        {
            get { return SuccessProperty.GetValue(this); }
            set { SuccessProperty.SetValue(this, value); }
        }

        public PropertyList<ScriptMetadata, ScriptDiagnostic> Diagnostics => new PropertyList<ScriptMetadata, ScriptDiagnostic>(DiagnosticsProperty, this);

        public PropertyList<ScriptMetadata, ScriptComponentSystem> Systems => new PropertyList<ScriptMetadata, ScriptComponentSystem>(SystemsProperty, this);

        public PropertyList<ScriptMetadata, ScriptEntityFilter> Filters => new PropertyList<ScriptMetadata, ScriptEntityFilter>(FiltersProperty, this);

        public PropertyList<ScriptMetadata, ScriptComponentBehaviour> Behaviours => new PropertyList<ScriptMetadata, ScriptComponentBehaviour>(BehavioursProperty, this);
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptComponentSystem : IPropertyContainer
    {
        public static ValueClassProperty<ScriptComponentSystem, string> NameProperty { get; private set; }
        public static ValueClassProperty<ScriptComponentSystem, string> QualifiedNameProperty { get; private set; }
        public static ValueClassProperty<ScriptComponentSystem, string> DescriptionProperty { get; private set; }
        public static ValueClassProperty<ScriptComponentSystem, bool> IsFenceProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentSystem, string> FiltersProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentSystem, string> ExecuteAfterProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentSystem, string> ExecuteBeforeProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentSystem, string> RequiredComponentsProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentSystem, string> OptionalComponentsProperty { get; private set; }
        public static ClassValueClassProperty<ScriptComponentSystem, ScriptSource> SourceProperty { get; private set; }

        private static ClassPropertyBag<ScriptComponentSystem> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<ScriptComponentSystem, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            QualifiedNameProperty = new ValueClassProperty<ScriptComponentSystem, string>(
                "QualifiedName"
                ,c => c.m_QualifiedName
                ,(c, v) => c.m_QualifiedName = v
            );

            DescriptionProperty = new ValueClassProperty<ScriptComponentSystem, string>(
                "Description"
                ,c => c.m_Description
                ,(c, v) => c.m_Description = v
            );

            IsFenceProperty = new ValueClassProperty<ScriptComponentSystem, bool>(
                "IsFence"
                ,c => c.m_IsFence
                ,(c, v) => c.m_IsFence = v
            );

            FiltersProperty = new ValueListClassProperty<ScriptComponentSystem, string>(
                "Filters"
                ,c => c.m_Filters
            );

            ExecuteAfterProperty = new ValueListClassProperty<ScriptComponentSystem, string>(
                "ExecuteAfter"
                ,c => c.m_ExecuteAfter
            );

            ExecuteBeforeProperty = new ValueListClassProperty<ScriptComponentSystem, string>(
                "ExecuteBefore"
                ,c => c.m_ExecuteBefore
            );

            RequiredComponentsProperty = new ValueListClassProperty<ScriptComponentSystem, string>(
                "RequiredComponents"
                ,c => c.m_RequiredComponents
            );

            OptionalComponentsProperty = new ValueListClassProperty<ScriptComponentSystem, string>(
                "OptionalComponents"
                ,c => c.m_OptionalComponents
            );

            SourceProperty = new ClassValueClassProperty<ScriptComponentSystem, ScriptSource>(
                "Source"
                ,c => c.m_Source
                ,(c, v) => c.m_Source = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptComponentSystem>(
                NameProperty,
                QualifiedNameProperty,
                DescriptionProperty,
                IsFenceProperty,
                FiltersProperty,
                ExecuteAfterProperty,
                ExecuteBeforeProperty,
                RequiredComponentsProperty,
                OptionalComponentsProperty,
                SourceProperty
            );
        }

        static ScriptComponentSystem()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private string m_QualifiedName;
        private string m_Description;
        private bool m_IsFence;
        private readonly List<string> m_Filters = new List<string>();
        private readonly List<string> m_ExecuteAfter = new List<string>();
        private readonly List<string> m_ExecuteBefore = new List<string>();
        private readonly List<string> m_RequiredComponents = new List<string>();
        private readonly List<string> m_OptionalComponents = new List<string>();
        private ScriptSource m_Source;

        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public string QualifiedName
        {
            get { return QualifiedNameProperty.GetValue(this); }
            set { QualifiedNameProperty.SetValue(this, value); }
        }

        public string Description
        {
            get { return DescriptionProperty.GetValue(this); }
            set { DescriptionProperty.SetValue(this, value); }
        }

        public bool IsFence
        {
            get { return IsFenceProperty.GetValue(this); }
            set { IsFenceProperty.SetValue(this, value); }
        }

        public PropertyList<ScriptComponentSystem, string> Filters => new PropertyList<ScriptComponentSystem, string>(FiltersProperty, this);

        public PropertyList<ScriptComponentSystem, string> ExecuteAfter => new PropertyList<ScriptComponentSystem, string>(ExecuteAfterProperty, this);

        public PropertyList<ScriptComponentSystem, string> ExecuteBefore => new PropertyList<ScriptComponentSystem, string>(ExecuteBeforeProperty, this);

        public PropertyList<ScriptComponentSystem, string> RequiredComponents => new PropertyList<ScriptComponentSystem, string>(RequiredComponentsProperty, this);

        public PropertyList<ScriptComponentSystem, string> OptionalComponents => new PropertyList<ScriptComponentSystem, string>(OptionalComponentsProperty, this);

        public ScriptSource Source
        {
            get { return SourceProperty.GetValue(this); }
            set { SourceProperty.SetValue(this, value); }
        }
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptComponentBehaviour : IPropertyContainer
    {
        public static ValueClassProperty<ScriptComponentBehaviour, string> NameProperty { get; private set; }
        public static ValueClassProperty<ScriptComponentBehaviour, string> QualifiedNameProperty { get; private set; }
        public static ValueClassProperty<ScriptComponentBehaviour, string> DescriptionProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> FiltersProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> ExecuteAfterProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> ExecuteBeforeProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> RequiredComponentsProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> OptionalComponentsProperty { get; private set; }
        public static ClassListClassProperty<ScriptComponentBehaviour, ScriptField> FieldsProperty { get; private set; }
        public static ValueListClassProperty<ScriptComponentBehaviour, string> MethodsProperty { get; private set; }
        public static ClassValueClassProperty<ScriptComponentBehaviour, ScriptSource> SourceProperty { get; private set; }

        private static ClassPropertyBag<ScriptComponentBehaviour> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<ScriptComponentBehaviour, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            QualifiedNameProperty = new ValueClassProperty<ScriptComponentBehaviour, string>(
                "QualifiedName"
                ,c => c.m_QualifiedName
                ,(c, v) => c.m_QualifiedName = v
            );

            DescriptionProperty = new ValueClassProperty<ScriptComponentBehaviour, string>(
                "Description"
                ,c => c.m_Description
                ,(c, v) => c.m_Description = v
            );

            FiltersProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "Filters"
                ,c => c.m_Filters
            );

            ExecuteAfterProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "ExecuteAfter"
                ,c => c.m_ExecuteAfter
            );

            ExecuteBeforeProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "ExecuteBefore"
                ,c => c.m_ExecuteBefore
            );

            RequiredComponentsProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "RequiredComponents"
                ,c => c.m_RequiredComponents
            );

            OptionalComponentsProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "OptionalComponents"
                ,c => c.m_OptionalComponents
            );

            FieldsProperty = new ClassListClassProperty<ScriptComponentBehaviour, ScriptField>(
                "Fields"
                ,c => c.m_Fields
                ,c => new ScriptField()
            );

            MethodsProperty = new ValueListClassProperty<ScriptComponentBehaviour, string>(
                "Methods"
                ,c => c.m_Methods
            );

            SourceProperty = new ClassValueClassProperty<ScriptComponentBehaviour, ScriptSource>(
                "Source"
                ,c => c.m_Source
                ,(c, v) => c.m_Source = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptComponentBehaviour>(
                NameProperty,
                QualifiedNameProperty,
                DescriptionProperty,
                FiltersProperty,
                ExecuteAfterProperty,
                ExecuteBeforeProperty,
                RequiredComponentsProperty,
                OptionalComponentsProperty,
                FieldsProperty,
                MethodsProperty,
                SourceProperty
            );
        }

        static ScriptComponentBehaviour()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private string m_QualifiedName;
        private string m_Description;
        private readonly List<string> m_Filters = new List<string>();
        private readonly List<string> m_ExecuteAfter = new List<string>();
        private readonly List<string> m_ExecuteBefore = new List<string>();
        private readonly List<string> m_RequiredComponents = new List<string>();
        private readonly List<string> m_OptionalComponents = new List<string>();
        private readonly List<ScriptField> m_Fields = new List<ScriptField>();
        private readonly List<string> m_Methods = new List<string>();
        private ScriptSource m_Source;

        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public string QualifiedName
        {
            get { return QualifiedNameProperty.GetValue(this); }
            set { QualifiedNameProperty.SetValue(this, value); }
        }

        public string Description
        {
            get { return DescriptionProperty.GetValue(this); }
            set { DescriptionProperty.SetValue(this, value); }
        }

        public PropertyList<ScriptComponentBehaviour, string> Filters => new PropertyList<ScriptComponentBehaviour, string>(FiltersProperty, this);

        public PropertyList<ScriptComponentBehaviour, string> ExecuteAfter => new PropertyList<ScriptComponentBehaviour, string>(ExecuteAfterProperty, this);

        public PropertyList<ScriptComponentBehaviour, string> ExecuteBefore => new PropertyList<ScriptComponentBehaviour, string>(ExecuteBeforeProperty, this);

        public PropertyList<ScriptComponentBehaviour, string> RequiredComponents => new PropertyList<ScriptComponentBehaviour, string>(RequiredComponentsProperty, this);

        public PropertyList<ScriptComponentBehaviour, string> OptionalComponents => new PropertyList<ScriptComponentBehaviour, string>(OptionalComponentsProperty, this);

        public PropertyList<ScriptComponentBehaviour, ScriptField> Fields => new PropertyList<ScriptComponentBehaviour, ScriptField>(FieldsProperty, this);

        public PropertyList<ScriptComponentBehaviour, string> Methods => new PropertyList<ScriptComponentBehaviour, string>(MethodsProperty, this);

        public ScriptSource Source
        {
            get { return SourceProperty.GetValue(this); }
            set { SourceProperty.SetValue(this, value); }
        }
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptEntityFilter : IPropertyContainer
    {
        public static ValueClassProperty<ScriptEntityFilter, string> NameProperty { get; private set; }
        public static ValueClassProperty<ScriptEntityFilter, string> QualifiedNameProperty { get; private set; }
        public static ValueClassProperty<ScriptEntityFilter, string> DescriptionProperty { get; private set; }
        public static ClassListClassProperty<ScriptEntityFilter, ScriptField> FieldsProperty { get; private set; }
        public static ClassValueClassProperty<ScriptEntityFilter, ScriptSource> SourceProperty { get; private set; }

        private static ClassPropertyBag<ScriptEntityFilter> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<ScriptEntityFilter, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            QualifiedNameProperty = new ValueClassProperty<ScriptEntityFilter, string>(
                "QualifiedName"
                ,c => c.m_QualifiedName
                ,(c, v) => c.m_QualifiedName = v
            );

            DescriptionProperty = new ValueClassProperty<ScriptEntityFilter, string>(
                "Description"
                ,c => c.m_Description
                ,(c, v) => c.m_Description = v
            );

            FieldsProperty = new ClassListClassProperty<ScriptEntityFilter, ScriptField>(
                "Fields"
                ,c => c.m_Fields
                ,c => new ScriptField()
            );

            SourceProperty = new ClassValueClassProperty<ScriptEntityFilter, ScriptSource>(
                "Source"
                ,c => c.m_Source
                ,(c, v) => c.m_Source = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptEntityFilter>(
                NameProperty,
                QualifiedNameProperty,
                DescriptionProperty,
                FieldsProperty,
                SourceProperty
            );
        }

        static ScriptEntityFilter()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private string m_QualifiedName;
        private string m_Description;
        private readonly List<ScriptField> m_Fields = new List<ScriptField>();
        private ScriptSource m_Source;

        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public string QualifiedName
        {
            get { return QualifiedNameProperty.GetValue(this); }
            set { QualifiedNameProperty.SetValue(this, value); }
        }

        public string Description
        {
            get { return DescriptionProperty.GetValue(this); }
            set { DescriptionProperty.SetValue(this, value); }
        }

        public PropertyList<ScriptEntityFilter, ScriptField> Fields => new PropertyList<ScriptEntityFilter, ScriptField>(FieldsProperty, this);

        public ScriptSource Source
        {
            get { return SourceProperty.GetValue(this); }
            set { SourceProperty.SetValue(this, value); }
        }
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptField : IPropertyContainer
    {
        public static ValueClassProperty<ScriptField, string> NameProperty { get; private set; }
        public static ValueClassProperty<ScriptField, string> QualifiedNameProperty { get; private set; }
        public static ValueClassProperty<ScriptField, string> DescriptionProperty { get; private set; }
        public static ValueClassProperty<ScriptField, bool> IsStaticProperty { get; private set; }
        public static ValueClassProperty<ScriptField, bool> IsReadonlyProperty { get; private set; }
        public static ValueClassProperty<ScriptField, bool> IsOptionalProperty { get; private set; }
        public static ValueClassProperty<ScriptField, bool> IsPublicProperty { get; private set; }
        public static ClassValueClassProperty<ScriptField, ScriptSource> SourceProperty { get; private set; }

        private static ClassPropertyBag<ScriptField> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<ScriptField, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            QualifiedNameProperty = new ValueClassProperty<ScriptField, string>(
                "QualifiedName"
                ,c => c.m_QualifiedName
                ,(c, v) => c.m_QualifiedName = v
            );

            DescriptionProperty = new ValueClassProperty<ScriptField, string>(
                "Description"
                ,c => c.m_Description
                ,(c, v) => c.m_Description = v
            );

            IsStaticProperty = new ValueClassProperty<ScriptField, bool>(
                "IsStatic"
                ,c => c.m_IsStatic
                ,(c, v) => c.m_IsStatic = v
            );

            IsReadonlyProperty = new ValueClassProperty<ScriptField, bool>(
                "IsReadonly"
                ,c => c.m_IsReadonly
                ,(c, v) => c.m_IsReadonly = v
            );

            IsOptionalProperty = new ValueClassProperty<ScriptField, bool>(
                "IsOptional"
                ,c => c.m_IsOptional
                ,(c, v) => c.m_IsOptional = v
            );

            IsPublicProperty = new ValueClassProperty<ScriptField, bool>(
                "IsPublic"
                ,c => c.m_IsPublic
                ,(c, v) => c.m_IsPublic = v
            );

            SourceProperty = new ClassValueClassProperty<ScriptField, ScriptSource>(
                "Source"
                ,c => c.m_Source
                ,(c, v) => c.m_Source = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptField>(
                NameProperty,
                QualifiedNameProperty,
                DescriptionProperty,
                IsStaticProperty,
                IsReadonlyProperty,
                IsOptionalProperty,
                IsPublicProperty,
                SourceProperty
            );
        }

        static ScriptField()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private string m_QualifiedName;
        private string m_Description;
        private bool m_IsStatic;
        private bool m_IsReadonly;
        private bool m_IsOptional;
        private bool m_IsPublic;
        private ScriptSource m_Source;

        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public string QualifiedName
        {
            get { return QualifiedNameProperty.GetValue(this); }
            set { QualifiedNameProperty.SetValue(this, value); }
        }

        public string Description
        {
            get { return DescriptionProperty.GetValue(this); }
            set { DescriptionProperty.SetValue(this, value); }
        }

        public bool IsStatic
        {
            get { return IsStaticProperty.GetValue(this); }
            set { IsStaticProperty.SetValue(this, value); }
        }

        public bool IsReadonly
        {
            get { return IsReadonlyProperty.GetValue(this); }
            set { IsReadonlyProperty.SetValue(this, value); }
        }

        public bool IsOptional
        {
            get { return IsOptionalProperty.GetValue(this); }
            set { IsOptionalProperty.SetValue(this, value); }
        }

        public bool IsPublic
        {
            get { return IsPublicProperty.GetValue(this); }
            set { IsPublicProperty.SetValue(this, value); }
        }

        public ScriptSource Source
        {
            get { return SourceProperty.GetValue(this); }
            set { SourceProperty.SetValue(this, value); }
        }
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptDiagnostic : IPropertyContainer
    {
        public static ValueClassProperty<ScriptDiagnostic, int> CategoryProperty { get; private set; }
        public static ValueClassProperty<ScriptDiagnostic, string> MessageProperty { get; private set; }
        public static ClassValueClassProperty<ScriptDiagnostic, ScriptSource> SourceProperty { get; private set; }

        private static ClassPropertyBag<ScriptDiagnostic> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            CategoryProperty = new ValueClassProperty<ScriptDiagnostic, int>(
                "Category"
                ,c => c.m_Category
                ,(c, v) => c.m_Category = v
            );

            MessageProperty = new ValueClassProperty<ScriptDiagnostic, string>(
                "Message"
                ,c => c.m_Message
                ,(c, v) => c.m_Message = v
            );

            SourceProperty = new ClassValueClassProperty<ScriptDiagnostic, ScriptSource>(
                "Source"
                ,c => c.m_Source
                ,(c, v) => c.m_Source = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptDiagnostic>(
                CategoryProperty,
                MessageProperty,
                SourceProperty
            );
        }

        static ScriptDiagnostic()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private int m_Category;
        private string m_Message;
        private ScriptSource m_Source;

        public int Category
        {
            get { return CategoryProperty.GetValue(this); }
            set { CategoryProperty.SetValue(this, value); }
        }

        public string Message
        {
            get { return MessageProperty.GetValue(this); }
            set { MessageProperty.SetValue(this, value); }
        }

        public ScriptSource Source
        {
            get { return SourceProperty.GetValue(this); }
            set { SourceProperty.SetValue(this, value); }
        }
    }
}
namespace Unity.Tiny
{
    internal partial class ScriptSource : IPropertyContainer
    {
        public static ValueClassProperty<ScriptSource, string> FileProperty { get; private set; }
        public static ValueClassProperty<ScriptSource, int> LineProperty { get; private set; }
        public static ValueClassProperty<ScriptSource, int> CharacterProperty { get; private set; }

        private static ClassPropertyBag<ScriptSource> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            FileProperty = new ValueClassProperty<ScriptSource, string>(
                "File"
                ,c => c.m_File
                ,(c, v) => c.m_File = v
            );

            LineProperty = new ValueClassProperty<ScriptSource, int>(
                "Line"
                ,c => c.m_Line
                ,(c, v) => c.m_Line = v
            );

            CharacterProperty = new ValueClassProperty<ScriptSource, int>(
                "Character"
                ,c => c.m_Character
                ,(c, v) => c.m_Character = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<ScriptSource>(
                FileProperty,
                LineProperty,
                CharacterProperty
            );
        }

        static ScriptSource()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_File;
        private int m_Line;
        private int m_Character;

        public string File
        {
            get { return FileProperty.GetValue(this); }
            set { FileProperty.SetValue(this, value); }
        }

        public int Line
        {
            get { return LineProperty.GetValue(this); }
            set { LineProperty.SetValue(this, value); }
        }

        public int Character
        {
            get { return CharacterProperty.GetValue(this); }
            set { CharacterProperty.SetValue(this, value); }
        }
    }
}
