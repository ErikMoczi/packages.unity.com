using UnityEditor.Experimental.U2D.Common;
using UnityEditor.Experimental.U2D.Layout;
using UnityEngine;
using UnityEditor.ShortcutManagement;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation
{
    public partial class SkinningModule
    {
        private LayoutOverlay m_LayoutOverlay;
        private BoneToolbar m_BoneToolbar;
        private MeshToolbar m_MeshToolbar;
        private WeightToolbar m_WeightToolbar;

        private InternalEditorBridge.ShortcutContext m_ShortcutContext;

        private static SkinningModule GetModuleFromContext(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sc = args.context as InternalEditorBridge.ShortcutContext;
            if (sc == null)
                return null;

            return sc.context as SkinningModule;
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Toggle Tool Text", typeof(InternalEditorBridge.ShortcutContext), "#`")]
        private static void CollapseToolbar(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null)
            {
                SkinningModuleSettings.compactToolBar = !SkinningModuleSettings.compactToolBar;
            }
        }

        
        [InternalEditorBridge.WrappedShortcut("2D/Animation/Restore Bind Pose", typeof(InternalEditorBridge.ShortcutContext), "#1")]
        private static void DisablePoseModeKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.GetEffectiveSkeleton(sm.skinningCache.selectedSprite).isPosePreview)
            {
                using (sm.skinningCache.UndoScope(TextContent.restorePose))
                {
                    sm.skinningCache.RestoreBindPose();
                    sm.skinningCache.events.shortcut.Invoke("#1");
                }
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Toggle Character Mode", typeof(InternalEditorBridge.ShortcutContext), "#2")]
        private static void ToggleCharacterModeKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.hasCharacter)
            {
                var tool = sm.skinningCache.GetTool(Tools.SwitchMode);

                using (sm.skinningCache.UndoScope(TextContent.setMode))
                {
                    if (tool.isActive)
                        tool.Deactivate();
                    else
                        tool.Activate();
                }
                
                sm.skinningCache.events.shortcut.Invoke("#2");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Preview Pose", typeof(InternalEditorBridge.ShortcutContext), "#q")]
        private static void EditPoseKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.EditPose);
                sm.skinningCache.events.shortcut.Invoke("#q");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Edit Joints", typeof(InternalEditorBridge.ShortcutContext), "#w")]
        private static void EditJointsKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.EditJoints);
                sm.skinningCache.events.shortcut.Invoke("#w");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Create Bone", typeof(InternalEditorBridge.ShortcutContext), "#e")]
        private static void CreateBoneKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.CreateBone);
                sm.skinningCache.events.shortcut.Invoke("#e");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Split Bone", typeof(InternalEditorBridge.ShortcutContext), "#r")]
        private static void SplitBoneKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.SplitBone);
                sm.skinningCache.events.shortcut.Invoke("#r");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Reparent Bone", typeof(InternalEditorBridge.ShortcutContext), "#t")]
        private static void ReparentBoneKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetSkeletonTool(Tools.ReparentBone);
                sm.skinningCache.events.shortcut.Invoke("#t");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Auto Geometry", typeof(InternalEditorBridge.ShortcutContext), "#a")]
        private static void GenerateGeometryKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.GenerateGeometry);
                sm.skinningCache.events.shortcut.Invoke("#a");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Edit Geometry", typeof(InternalEditorBridge.ShortcutContext), "#s")]
        private static void MeshSelectionKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.EditGeometry);
                sm.skinningCache.events.shortcut.Invoke("#s");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Create Vertex", typeof(InternalEditorBridge.ShortcutContext), "#d")]
        private static void CreateVertex(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.CreateVertex);
                sm.skinningCache.events.shortcut.Invoke("#d");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Create Edge", typeof(InternalEditorBridge.ShortcutContext), "#g")]
        private static void CreateEdgeKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.CreateEdge);
                sm.skinningCache.events.shortcut.Invoke("#g");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Split Edge", typeof(InternalEditorBridge.ShortcutContext), "#h")]
        private static void SplitEdge(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetMeshTool(Tools.SplitEdge);
                sm.skinningCache.events.shortcut.Invoke("#h");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Auto Weights", typeof(InternalEditorBridge.ShortcutContext), "#z")]
        private static void GenerateWeightsKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.GenerateWeights);
                sm.skinningCache.events.shortcut.Invoke("#z");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Weight Slider", typeof(InternalEditorBridge.ShortcutContext), "#x")]
        private static void WeightSliderKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.WeightSlider);
                sm.skinningCache.events.shortcut.Invoke("#x");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Weight Brush", typeof(InternalEditorBridge.ShortcutContext), "#c")]
        private static void WeightBrushKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.SetWeightTool(Tools.WeightBrush);
                sm.skinningCache.events.shortcut.Invoke("#c");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Bone Influence", typeof(InternalEditorBridge.ShortcutContext), "#v")]
        private static void BoneInfluenceKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled && sm.skinningCache.mode == SkinningMode.Character)
            {
                sm.SetWeightTool(Tools.BoneInfluence);
                sm.skinningCache.events.shortcut.Invoke("#v");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Paste Panel Weights", typeof(InternalEditorBridge.ShortcutContext), "#b")]
        private static void PastePanelKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.m_HorizontalToggleTools.TogglePasteTool(sm.currentTool);
                sm.skinningCache.events.shortcut.Invoke("#b");
            }
        }

        [InternalEditorBridge.WrappedShortcut("2D/Animation/Visibility Panel", typeof(InternalEditorBridge.ShortcutContext), "#p")]
        private static void VisibilityPanelKey(InternalEditorBridge.WrappedShortcutArguments args)
        {
            var sm = GetModuleFromContext(args);
            if (sm != null && !sm.spriteEditor.editingDisabled)
            {
                sm.m_HorizontalToggleTools.ToggleVisibilityTool(sm.currentTool);
                sm.skinningCache.events.shortcut.Invoke("#p");
            }
        }

        private void AddMainUI(VisualElement mainView)
        {
            var visualTree = Resources.Load("LayoutOverlay") as VisualTreeAsset;
            VisualElement clone = visualTree.CloneTree(null);
            m_LayoutOverlay = clone.Q<LayoutOverlay>("LayoutOverlay");

            mainView.Add(m_LayoutOverlay);
            m_LayoutOverlay.hasScrollbar = true;
            m_LayoutOverlay.StretchToParentSize();

            CreateBoneToolbar();
            CreateMeshToolbar();
            CreateWeightToolbar();

            m_ShortcutContext = InternalEditorBridge.CreateShortcutContext(isFocused);
            m_ShortcutContext.context = this;
            InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
            InternalEditorBridge.AddEditorApplicationProjectLoadedCallback(OnProjectLoaded);
        }

        private void OnProjectLoaded()
        {
            if (m_ShortcutContext != null)
                InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
        }
        
        private void DoViewGUI()
        {
            if (spriteEditor.editingDisabled == m_BoneToolbar.enabledSelf)
            {
                m_BoneToolbar.SetEnabled(!spriteEditor.editingDisabled);
                m_MeshToolbar.SetEnabled(!spriteEditor.editingDisabled);
                m_WeightToolbar.SetEnabled(!spriteEditor.editingDisabled);
            }
        }

        private bool isFocused()
        {
            return spriteEditor != null && (EditorWindow.focusedWindow == spriteEditor as EditorWindow);
        }

        private void CreateBoneToolbar()
        {
            m_BoneToolbar = BoneToolbar.GenerateFromUXML();
            m_BoneToolbar.Setup(skinningCache);
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_BoneToolbar);

            m_BoneToolbar.SetSkeletonTool += SetSkeletonTool;
        }

        private void CreateMeshToolbar()
        {
            m_MeshToolbar = MeshToolbar.GenerateFromUXML();
            m_MeshToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_MeshToolbar);

            m_MeshToolbar.SetMeshTool += SetMeshTool;
        }

        private void CreateWeightToolbar()
        {
            m_WeightToolbar = WeightToolbar.GenerateFromUXML();
            m_WeightToolbar.skinningCache = skinningCache;
            m_LayoutOverlay.verticalToolbar.AddToContainer(m_WeightToolbar);
            m_WeightToolbar.SetWeightTool += SetWeightTool;
        }

        private void SetSkeletonTool(Tools toolType)
        {
            var tool = skinningCache.GetTool(toolType) as SkeletonToolWrapper;

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);

                if (tool.editBindPose)
                    skinningCache.RestoreBindPose();
            }
        }

        private void SetMeshTool(Tools toolType)
        {
            var tool  = skinningCache.GetTool(toolType);

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);
                skinningCache.RestoreBindPose();
                UnselectBones();
            }
        }

        private void SetWeightTool(Tools toolType)
        {
            var tool = skinningCache.GetTool(toolType);

            if (currentTool == tool)
                return;

            using (skinningCache.UndoScope(TextContent.setTool))
            {
                ActivateTool(tool);
            }
        }

        private void ActivateTool(BaseTool tool)
        {
            if (currentTool == tool)
                return;

            if (currentTool != null)
                currentTool.Deactivate();

            currentTool = tool;
            currentTool.Activate();

            UpdateToggleState();
            skinningCache.events.toolChanged.Invoke(currentTool);
        }

        private void UnselectBones()
        {
            skinningCache.skeletonSelection.Clear();
            skinningCache.events.boneSelectionChanged.Invoke();
        }

        private void UpdateToggleState()
        {
            Debug.Assert(m_BoneToolbar != null);
            Debug.Assert(m_MeshToolbar != null);
            Debug.Assert(m_WeightToolbar != null);

            m_BoneToolbar.UpdateToggleState();
            m_MeshToolbar.UpdateToggleState();
            m_WeightToolbar.UpdateToggleState();
        }

        private void RemoveMainUI(VisualElement mainView)
        {
            InternalEditorBridge.RemoveEditorApplicationProjectLoadedCallback(OnProjectLoaded);
            InternalEditorBridge.UnregisterShortcutContext(m_ShortcutContext);
        }
    }
}
