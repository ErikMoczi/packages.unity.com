
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyTypeScriptConfigGenerator : IPropertyContainer
    {
        public static ClassValueClassProperty<TinyTypeScriptConfigGenerator, TinyTypeScriptCompilerOptions> compilerOptionsProperty { get; private set; }
        public static ValueClassProperty<TinyTypeScriptConfigGenerator, bool> compileOnSaveProperty { get; private set; }
        public static ValueListClassProperty<TinyTypeScriptConfigGenerator, string> includeProperty { get; private set; }
        public static ValueListClassProperty<TinyTypeScriptConfigGenerator, string> excludeProperty { get; private set; }
        public static ValueListClassProperty<TinyTypeScriptConfigGenerator, string> filesProperty { get; private set; }

        private static ClassPropertyBag<TinyTypeScriptConfigGenerator> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
            compilerOptionsProperty = new ClassValueClassProperty<TinyTypeScriptConfigGenerator, TinyTypeScriptCompilerOptions>(
                "compilerOptions"
                ,c => c.m_compilerOptions
                ,(c, v) => c.m_compilerOptions = v
            );

            compileOnSaveProperty = new ValueClassProperty<TinyTypeScriptConfigGenerator, bool>(
                "compileOnSave"
                ,c => c.m_compileOnSave
                ,(c, v) => c.m_compileOnSave = v
            );

            includeProperty = new ValueListClassProperty<TinyTypeScriptConfigGenerator, string>(
                "include"
                ,c => c.m_include
            );

            excludeProperty = new ValueListClassProperty<TinyTypeScriptConfigGenerator, string>(
                "exclude"
                ,c => c.m_exclude
            );

            filesProperty = new ValueListClassProperty<TinyTypeScriptConfigGenerator, string>(
                "files"
                ,c => c.m_files
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyTypeScriptConfigGenerator>(
                compilerOptionsProperty,
                compileOnSaveProperty,
                includeProperty,
                excludeProperty,
                filesProperty
            );
        }

        static TinyTypeScriptConfigGenerator()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private TinyTypeScriptCompilerOptions m_compilerOptions;
        private bool m_compileOnSave;
        private readonly List<string> m_include = new List<string>();
        private readonly List<string> m_exclude = new List<string>();
        private readonly List<string> m_files = new List<string>();

        public TinyTypeScriptCompilerOptions compilerOptions
        {
            get { return compilerOptionsProperty.GetValue(this); }
            set { compilerOptionsProperty.SetValue(this, value); }
        }

        public bool compileOnSave
        {
            get { return compileOnSaveProperty.GetValue(this); }
            set { compileOnSaveProperty.SetValue(this, value); }
        }

        public PropertyList<TinyTypeScriptConfigGenerator, string> include => new PropertyList<TinyTypeScriptConfigGenerator, string>(includeProperty, this);

        public PropertyList<TinyTypeScriptConfigGenerator, string> exclude => new PropertyList<TinyTypeScriptConfigGenerator, string>(excludeProperty, this);

        public PropertyList<TinyTypeScriptConfigGenerator, string> files => new PropertyList<TinyTypeScriptConfigGenerator, string>(filesProperty, this);


        public partial class TinyTypeScriptCompilerOptions : IPropertyContainer
        {
            public static ValueClassProperty<TinyTypeScriptCompilerOptions, bool> experimentalDecoratorsProperty { get; private set; }
            public static ValueClassProperty<TinyTypeScriptCompilerOptions, bool> skipLibCheckProperty { get; private set; }
            public static ValueClassProperty<TinyTypeScriptCompilerOptions, string> outFileProperty { get; private set; }
            public static ValueClassProperty<TinyTypeScriptCompilerOptions, bool> sourceMapProperty { get; private set; }
            public static ValueClassProperty<TinyTypeScriptCompilerOptions, string> targetProperty { get; private set; }

            private static ClassPropertyBag<TinyTypeScriptCompilerOptions> s_PropertyBag { get; set; }

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => null;

            private static void InitializeProperties()
            {
                experimentalDecoratorsProperty = new ValueClassProperty<TinyTypeScriptCompilerOptions, bool>(
                    "experimentalDecorators"
                    ,c => c.m_experimentalDecorators
                    ,(c, v) => c.m_experimentalDecorators = v
                );

                skipLibCheckProperty = new ValueClassProperty<TinyTypeScriptCompilerOptions, bool>(
                    "skipLibCheck"
                    ,c => c.m_skipLibCheck
                    ,(c, v) => c.m_skipLibCheck = v
                );

                outFileProperty = new ValueClassProperty<TinyTypeScriptCompilerOptions, string>(
                    "outFile"
                    ,c => c.m_outFile
                    ,(c, v) => c.m_outFile = v
                );

                sourceMapProperty = new ValueClassProperty<TinyTypeScriptCompilerOptions, bool>(
                    "sourceMap"
                    ,c => c.m_sourceMap
                    ,(c, v) => c.m_sourceMap = v
                );

                targetProperty = new ValueClassProperty<TinyTypeScriptCompilerOptions, string>(
                    "target"
                    ,c => c.m_target
                    ,(c, v) => c.m_target = v
                );
            }

            /// <summary>
            /// Implement this partial method to initialize custom properties
            /// </summary>
            static partial void InitializeCustomProperties();

            private static void InitializePropertyBag()
            {
                s_PropertyBag = new ClassPropertyBag<TinyTypeScriptCompilerOptions>(
                    experimentalDecoratorsProperty,
                    skipLibCheckProperty,
                    outFileProperty,
                    sourceMapProperty,
                    targetProperty
                );
            }

            static TinyTypeScriptCompilerOptions()
            {
                InitializeProperties();
                InitializeCustomProperties();
                InitializePropertyBag();
            }

            private bool m_experimentalDecorators;
            private bool m_skipLibCheck;
            private string m_outFile;
            private bool m_sourceMap;
            private string m_target;

            public bool experimentalDecorators
            {
                get { return experimentalDecoratorsProperty.GetValue(this); }
                set { experimentalDecoratorsProperty.SetValue(this, value); }
            }

            public bool skipLibCheck
            {
                get { return skipLibCheckProperty.GetValue(this); }
                set { skipLibCheckProperty.SetValue(this, value); }
            }

            public string outFile
            {
                get { return outFileProperty.GetValue(this); }
                set { outFileProperty.SetValue(this, value); }
            }

            public bool sourceMap
            {
                get { return sourceMapProperty.GetValue(this); }
                set { sourceMapProperty.SetValue(this, value); }
            }

            public string target
            {
                get { return targetProperty.GetValue(this); }
                set { targetProperty.SetValue(this, value); }
            }
        }    }
}
