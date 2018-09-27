using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Burst.Compiler.IL;
using Unity.Burst.LowLevel;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Burst.Editor
{
    using static BurstCompilerOptions;

    internal enum DisassemblyKind
    {
        Asm = 0,
        IL = 1,
        UnoptimizedIR = 2,
        OptimizedIR = 3,
        IRPassAnalysis = 4
    }

    internal class BurstInspectorGUI : EditorWindow
    {
        private const string FontSizeIndexPref = "BurstInspectorFontSizeIndex";

        private static readonly string[] DisassemblyKindNames =
        {
            "Assembly",
            ".NET IL",
            "LLVM IR (Unoptimized)",
            "LLVM IR (Optimized)",
            "LLVM IR Optimisation Diagnostics"
        };

        private static readonly string[] DisasmOptions =
        {
            "\n" + GetOption(OptionDump, NativeDumpFlags.Asm),
            "\n" + GetOption(OptionDump, NativeDumpFlags.IL),
            "\n" + GetOption(OptionDump, NativeDumpFlags.IR),
            "\n" + GetOption(OptionDump, NativeDumpFlags.IROptimized),
            "\n" + GetOption(OptionDump, NativeDumpFlags.IRPassAnalysis)
        };

        private static readonly string[] CodeGenOptions =
        {
            "auto",
            "x86_sse2",
            "x86_sse4",
            "x64_sse2",
            "x64_sse4",
            "avx",
            "avx2",
            "avx512",
            "armv7a_neon32",
            "armv8a_aarch64",
            "thumb2_neon32",
        };

        private static readonly int[] FontSizes =
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20
        };

        private static string[] _fontSizesText;

        [SerializeField] private int _codeGenOptions = 0;

        [SerializeField] private DisassemblyKind _disasmKind = DisassemblyKind.Asm;

        [SerializeField] private bool _fastMath = true;

        private GUIStyle _fixedFontStyle;

        private int _fontSizeIndex = -1;

        [SerializeField] private bool _optimizations = true;

        [SerializeField] private bool _safetyChecks = true;
        [SerializeField] private bool _enhancedDisassembly = false;
        private Vector2 _scrollPos;
        private SearchField _searchField;

        [SerializeField] private string _selectedItem;
        private List<BurstCompileTarget> _targets;
        private LongTextArea _textArea;
        private BurstMethodTreeView _treeView;

        private int FontSize => FontSizes[_fontSizeIndex];

        public void OnEnable()
        {
            if (_treeView == null) _treeView = new BurstMethodTreeView(new TreeViewState());
        }

        public void OnGUI()
        {
            if (_targets == null)
            {
                _targets = BurstReflection.FindExecuteMethods(AssembliesType.Editor);
                _treeView.Targets = _targets;
                _treeView.Reload();

                if (_selectedItem != null) _treeView.TrySelectByDisplayName(_selectedItem);
            }

            if (_fontSizesText == null)
            {
                _fontSizesText = new string[FontSizes.Length];
                for (var i = 0; i < FontSizes.Length; ++i) _fontSizesText[i] = FontSizes[i].ToString();
            }

            if (_fontSizeIndex == -1)
            {
                _fontSizeIndex = EditorPrefs.GetInt(FontSizeIndexPref, 5);
                _fontSizeIndex = Math.Max(0, _fontSizeIndex);
                _fontSizeIndex = Math.Min(_fontSizeIndex, FontSizes.Length - 1);
            }

            if (_fixedFontStyle == null)
            {
                _fixedFontStyle = new GUIStyle(GUI.skin.label);
                string fontName;
                if (Application.platform == RuntimePlatform.WindowsEditor)
                  fontName = "Consolas";
                else
                  fontName = "Courier";
                _fixedFontStyle.font = Font.CreateDynamicFontFromOSFont(fontName, FontSize);
            }

            if (_searchField == null) _searchField = new SearchField();

            if (_textArea == null) _textArea = new LongTextArea();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(position.width / 3));

            GUILayout.Label("Compile Targets", EditorStyles.boldLabel);

            var newFilter = _searchField.OnGUI(_treeView.Filter);

            if (newFilter != _treeView.Filter)
            {
                _treeView.Filter = newFilter;
                _treeView.Reload();
            }

            _treeView.OnGUI(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)));

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            var selection = _treeView.GetSelection();
            if (selection.Count == 1)
            {
                var id = selection[0];
                var target = _targets[id - 1];
                // Stash selected item name to handle domain reloads more gracefully
                _selectedItem = target.GetDisplayName();

                GUILayout.BeginHorizontal();

                _enhancedDisassembly = GUILayout.Toggle(_enhancedDisassembly, "Enhanced Disassembly");
                _safetyChecks = GUILayout.Toggle(_safetyChecks, "Safety Checks");
                _optimizations = GUILayout.Toggle(_optimizations, "Optimizations");
                _fastMath = GUILayout.Toggle(_fastMath, "Fast Math");
                EditorGUI.BeginDisabledGroup(!target.SupportsBurst);
                _codeGenOptions = EditorGUILayout.Popup(_codeGenOptions, CodeGenOptions);

                GUILayout.Label("Font Size");
                var fsi = EditorGUILayout.Popup(_fontSizeIndex, _fontSizesText);

                var doRefresh = GUILayout.Button("Refresh Disassembly");
                var doCopy = GUILayout.Button("Copy to Clipboard");
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                _disasmKind = (DisassemblyKind) GUILayout.Toolbar((int) _disasmKind, DisassemblyKindNames);

                var disasm = target.Disassembly != null ? target.Disassembly[(int) _disasmKind] : null;

                if (doRefresh)
                {
                    // TODO: refactor this code with a proper AppendOption to avoid these "\n"
                    var options = new StringBuilder();
                    if (!_safetyChecks)
                        options.Append("\n" + GetOption(OptionDisableSafetyChecks) + "\n" + GetOption(OptionNoAlias));

                    if (_enhancedDisassembly)
                        options.Append("\n" + GetOption(OptionDebug));

                    if (!_optimizations) options.Append("\n" + GetOption(OptionDisableOpt));

                    if (_fastMath) options.Append("\n" + GetOption(OptionFastMath));

                    options.AppendFormat("\n" + GetOption(OptionTarget, CodeGenOptions[_codeGenOptions]));

                    var baseOptions = options.ToString().Trim('\n', ' ');

                    target.Disassembly = new string[DisasmOptions.Length];

                    for (var i = 0; i < DisasmOptions.Length; ++i)
                        target.Disassembly[i] = GetDisassembly(target.Method, baseOptions + DisasmOptions[i]);

                    if (_enhancedDisassembly && (int)DisassemblyKind.Asm < target.Disassembly.Length)
                    {
                        var processor = new BurstDisassembler();
                        target.Disassembly[(int)DisassemblyKind.Asm] = processor.Process(target.Disassembly[(int)DisassemblyKind.Asm]);
                    }

                    disasm = target.Disassembly[(int) _disasmKind];
                }

                if (disasm != null)
                {
                    _textArea.Text = disasm;
                    _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                    _textArea.Render(_fixedFontStyle);
                    GUILayout.EndScrollView();
                }

                if (doCopy) EditorGUIUtility.systemCopyBuffer = disasm == null ? "" : disasm;

                if (fsi != _fontSizeIndex)
                {
                    _fontSizeIndex = fsi;
                    EditorPrefs.SetInt(FontSizeIndexPref, fsi);
                    _fixedFontStyle = null;
                }
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private static string GetDisassembly(MethodInfo method, string options)
        {
            try
            {
                var result = BurstCompilerService.GetDisassembly(method, options);
                return TabsToSpaces(result);
            }
            catch (Exception e)
            {
                return "Failed to compile:\n" + e.Message;
            }
        }

        private static string TabsToSpaces(string s)
        {
            const int tabSize = 8;
            var lineLength = 0;
            var result = new StringBuilder(s.Length);
            foreach (var ch in s)
                switch (ch)
                {
                    case '\n':
                        result.Append(ch);
                        lineLength = 0;
                        break;
                    case '\t':
                    {
                        var spaceCount = tabSize - lineLength % tabSize;
                        for (var i = 0; i < spaceCount; ++i) result.Append(' ');

                        lineLength += spaceCount;
                        break;
                    }

                    default:
                        result.Append(ch);
                        lineLength++;
                        break;
                }

            return result.ToString();
        }
    }

    internal class BurstMethodTreeView : TreeView
    {
        public BurstMethodTreeView(TreeViewState state) : base(state)
        {
        }

        public BurstMethodTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
        }

        public List<BurstCompileTarget> Targets { get; set; }
        public string Filter { get; set; }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var allItems = new List<TreeViewItem>();

            if (Targets != null)
            {
                allItems.Capacity = Targets.Count;
                var id = 1;
                var filter = Filter;
                foreach (var t in Targets)
                {
                    var displayName = t.GetDisplayName();
                    if (string.IsNullOrEmpty(filter) || displayName.IndexOf(filter, 0, displayName.Length, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        allItems.Add(new TreeViewItem {id = id, depth = 0, displayName = displayName});

                    ++id;
                }
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        internal void TrySelectByDisplayName(string name)
        {
            var id = 1;
            foreach (var t in Targets)
                if (t.GetDisplayName() == name)
                {
                    SetSelection(new[] {id});
                    FrameItem(id);
                    break;
                }
                else
                {
                    ++id;
                }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var target = Targets[args.item.id - 1];
            var wasEnabled = GUI.enabled;
            GUI.enabled = target.SupportsBurst;
            base.RowGUI(args);
            GUI.enabled = wasEnabled;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
    }
}
