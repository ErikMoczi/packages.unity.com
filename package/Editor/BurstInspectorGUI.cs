
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Unity.Burst.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Burst.Editor
{
    internal enum DisassemblyKind
    {
        Asm = 0,
        IL = 1,
        UnoptimizedIR = 2,
        OptimizedIR = 3,
    }

    internal class BurstCompileTarget
    {
        /// <summary>
        /// The Execute method of the target's producer type.
        /// </summary>
        public MethodInfo method;

        /// <summary>
        /// The type of the actual job (i.e. BoidsSimulationJob).
        /// </summary>
        public Type jobType;

        /// <summary>
        /// The type of job (i.e. IJobParallelFor)
        /// </summary>
        public Type jobInterfaceType;

        /// <summary>
        /// Generated disassembly, or null if disassembly failed
        /// </summary>
        public string[] disassembly;

        /// <summary>
        /// Set to true if burst compilation is possible.
        /// </summary>
        public bool supportsBurst;

        public string GetDisplayName()
        {
            return jobType.ToString();
        }
    }

    public class BurstInspectorGUI : EditorWindow
    {
        private List<BurstCompileTarget> m_Targets;
        private BurstMethodTreeView m_TreeView;
        private SearchField m_SearchField;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Jobs/Burst Inspector")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            BurstInspectorGUI window = EditorWindow.GetWindow<BurstInspectorGUI>("Burst Inspector");
            window.Show();
        }

        public void OnEnable()
        {
            if (m_TreeView == null)
            {
                m_TreeView = new BurstMethodTreeView(new TreeViewState());
            }
        }

        private static List<BurstCompileTarget> FindExecuteMethods()
        {
            var result = new List<BurstCompileTarget>();

            List<Type> valueTypes = new List<Type>();
            Dictionary<Type, Type> interfaceToProducer = new Dictionary<Type, Type>();

            // Find all ways to execute job types (via producer attributes)
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.IsValueType)
                        valueTypes.Add(t);

                    if (!t.IsInterface)
                        continue;

                    object[] attrs = t.GetCustomAttributes(typeof(JobProducerTypeAttribute), false);
                    if (attrs.Length == 0)
                        continue;

                    JobProducerTypeAttribute attr = (JobProducerTypeAttribute) attrs[0];

                    interfaceToProducer.Add(t, attr.ProducerType);

                    //Debug.Log($"{t} has producer {attr.ProducerType}");
                }
            }

            //Debug.Log($"Mapped {interfaceToProducer.Count} producers; {valueTypes.Count} value types");

            // Revisit all types to find things that are compilable using the above producers.
            foreach (var type in valueTypes)
            {
                Type foundProducer = null;
                Type foundInterface = null;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceToProducer.TryGetValue(interfaceType, out foundProducer))
                    {
                        foundInterface = interfaceType;
                        break;
                    }
                }

                if (null == foundProducer)
                    continue;

                try
                {
                    Type concreteProducer = foundProducer.MakeGenericType(type);

                    MethodInfo executeMethod = concreteProducer.GetMethod("Execute");
                    var target = new BurstCompileTarget
                    {
                        method = executeMethod,
                        jobInterfaceType = foundInterface,
                        jobType = type,
                    };

                    string options;
                    target.supportsBurst = BurstLoader.ExtractBurstCompilerOptions(type, out options);

                    if (!target.supportsBurst)
                    {
                        target.disassembly = null;
                    }

                    result.Add(target);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return result;
        }

        private Vector2 scrollPos;

        [SerializeField]
        private bool m_SafetyChecks = true;

        [SerializeField]
        private bool m_Optimizations = true;

        [SerializeField]
        private bool m_FastMath = true;

        [SerializeField]
        private string m_SelectedItem;

        [SerializeField]
        private int m_CodeGenOption = 2;

        [SerializeField]
        private DisassemblyKind m_Kind = DisassemblyKind.Asm;

        const string kFontSizeIndexPref = "BurstInspectorFontSizeIndex";

        private static readonly string[] s_DisassemblyKindNames = new string[]
        {
            "Assembly",
            ".NET IL",
            "LLVM IR (Unoptimized)",
            "LLVM IR (Optimized)",
        };

        private static readonly string[] s_DisasmOptions = new string[]
        {
            " -disassembly=asm",
            " -disassembly=il",
            " -disassembly=ir-unopt",
            " -disassembly=ir-opt",
        };


        GUIStyle m_FixedFontStyle = null;

        private int m_FontSizeIndex = -1;
        private LongTextArea m_TextArea = null;

        private static int[] s_FontSizes = new int[]
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20,
        };

        private static string[] s_FontSizesStr = null;

        private int FontSize { get { return s_FontSizes[m_FontSizeIndex]; } }

        public void OnGUI()
        {
            if (m_Targets == null)
            {
                m_Targets = FindExecuteMethods();
                m_TreeView.Targets = m_Targets;
                m_TreeView.Reload();

                if (m_SelectedItem != null)
                {
                    m_TreeView.TrySelectByDisplayName(m_SelectedItem);
                }
            }

            if (s_FontSizesStr == null)
            {
                s_FontSizesStr = new string[s_FontSizes.Length];
                for (int i = 0; i < s_FontSizes.Length; ++i)
                {
                    s_FontSizesStr[i] = s_FontSizes[i].ToString();
                }
            }

            if (m_FontSizeIndex == -1)
            {
                m_FontSizeIndex = EditorPrefs.GetInt(kFontSizeIndexPref, 5);
                m_FontSizeIndex = Math.Max(0, m_FontSizeIndex);
                m_FontSizeIndex = Math.Min(m_FontSizeIndex, s_FontSizes.Length - 1);
            }

            if (m_FixedFontStyle == null)
            {
                m_FixedFontStyle = new GUIStyle(GUI.skin.label);
                m_FixedFontStyle.font = Font.CreateDynamicFontFromOSFont("Courier", FontSize);
            }

            if (m_SearchField == null)
            {
                m_SearchField = new SearchField();
            }

            if (m_TextArea == null)
            {
                m_TextArea = new LongTextArea();
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(position.width/3));

            GUILayout.Label("Compile Targets", EditorStyles.boldLabel);

            var newFilter = m_SearchField.OnGUI(m_TreeView.Filter);

            if (newFilter != m_TreeView.Filter)
            {
                m_TreeView.Filter = newFilter;
                m_TreeView.Reload();
            }

            m_TreeView.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)));

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            IList<int> selection = m_TreeView.GetSelection();
            if (selection.Count == 1)
            {
                int id = selection[0];
                var target = m_Targets[id - 1];
                // Stash selected item name to handle domain reloads more gracefully
                m_SelectedItem = target.GetDisplayName();

                GUILayout.BeginHorizontal();

                m_SafetyChecks = GUILayout.Toggle(m_SafetyChecks, "Safety Checks");
                m_Optimizations = GUILayout.Toggle(m_Optimizations, "Optimizations");
                m_FastMath = GUILayout.Toggle(m_FastMath, "Fast Math");
                EditorGUI.BeginDisabledGroup(!target.supportsBurst);
                m_CodeGenOption = EditorGUILayout.Popup(m_CodeGenOption, s_CodeGenOptions);

                GUILayout.Label("Font Size");
                int fsi = EditorGUILayout.Popup(m_FontSizeIndex, s_FontSizesStr);

                bool doRefresh = GUILayout.Button("Refresh Disassembly");
                bool doCopy = GUILayout.Button("Copy to Clipboard");
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                m_Kind = (DisassemblyKind) GUILayout.Toolbar((int) m_Kind, s_DisassemblyKindNames);

                string disasm = target.disassembly != null ? target.disassembly[(int) m_Kind] : null;

                if (doRefresh)
                {
                    StringBuilder options = new StringBuilder();
                    if (!m_SafetyChecks)
                    {
                        options.Append(" -disable-safety-checks");
                    }
                    if (!m_Optimizations)
                    {
                        options.Append(" -disable-optimizations");
                    }
                    if (m_FastMath)
                    {
                        options.Append(" -fast-math");
                    }
                    options.AppendFormat(" -simd={0}", s_CodeGenOptions[m_CodeGenOption]);

                    string baseOptions = options.ToString().Trim(' ');

                    target.disassembly = new string[s_DisasmOptions.Length];

                    for (int i = 0; i < s_DisasmOptions.Length; ++i)
                    {
                        target.disassembly[i] = GetDisassembly(target.method, baseOptions + s_DisasmOptions[i]);
                    }

                    disasm = target.disassembly[(int) m_Kind];
                }

                if (disasm != null)
                {
                    m_TextArea.Text = disasm;
                    scrollPos = GUILayout.BeginScrollView(scrollPos);
                    m_TextArea.Render(m_FixedFontStyle);
                    GUILayout.EndScrollView();
                }

                if (doCopy)
                {
                    EditorGUIUtility.systemCopyBuffer = disasm == null ? "" : disasm;
                }

                if (fsi != m_FontSizeIndex)
                {
                    m_FontSizeIndex = fsi;
                    EditorPrefs.SetInt(kFontSizeIndexPref, fsi);
                    m_FixedFontStyle = null;
                }
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private static string GetDisassembly(MethodInfo method, string options)
        {
            try
            {
                string result = BurstCompilerService.GetDisassembly(method, options);
                return TabsToSpaces(result);
            }
            catch (Exception e)
            {
                return "Failed to compile:\n" + e.Message;
            }
        }

        private static readonly string[] s_CodeGenOptions = new string[]
        {
            "none",
            "sse2",
            "sse4",
            "avx",
            "avx2",
            "avx512",
        };

        private static string TabsToSpaces(string s)
        {
            const int tabSize = 8;
            int lineLength = 0;
            StringBuilder result = new StringBuilder();
            result.Capacity = s.Length;

            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '\n':
                        result.Append(ch);
                        lineLength = 0;
                        break;
                    case '\t':
                        {
                            int spaceCount = tabSize - (lineLength % tabSize);
                            for (int i = 0; i < spaceCount; ++i)
                            {
                                result.Append(' ');
                            }
                            lineLength += spaceCount;
                            break;
                        }

                    default:
                        result.Append(ch);
                        lineLength++;
                        break;
                }
            }

            return result.ToString();
        }
    }

    internal class BurstMethodTreeView : TreeView
    {
        public List<BurstCompileTarget> Targets { get; set; }
        public string Filter { get; set; }

        public BurstMethodTreeView(TreeViewState state) : base(state)
        {
        }

        public BurstMethodTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var allItems = new List<TreeViewItem>();

            if (Targets != null)
            {
                allItems.Capacity = Targets.Count;
                int id = 1;
                string filter = Filter;
                foreach (var t in Targets)
                {
                    var displayName = t.GetDisplayName();
                    if (String.IsNullOrEmpty(filter) || displayName.IndexOf(filter, 0, displayName.Length, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        allItems.Add(new TreeViewItem { id = id, depth = 0, displayName = displayName });
                    }
                    ++id;
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        internal void TrySelectByDisplayName(string name)
        {
            int id = 1;
            foreach (var t in Targets)
            {
                if (t.GetDisplayName() == name)
                {
                    this.SetSelection(new int[] { id });
                    this.FrameItem(id);
                    break;
                }
                else
                {
                    ++id;
                }
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var target = Targets[args.item.id - 1];
            bool wasEnabled = GUI.enabled;
            GUI.enabled = target.supportsBurst;
            base.RowGUI(args);
            GUI.enabled = wasEnabled;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

    }
}