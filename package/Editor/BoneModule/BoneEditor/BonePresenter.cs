using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Experimental.U2D;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class BoneEditorState : ICloneable
    {
        public List<IBone> selectedBones { get; set; }
        public bool normalCreating { get; set; }
        public bool freeCreating { get; set; }
        public bool parenting { get; set; }
        public bool freeMoving { get; set; }
        public bool multiselecting { get; set; }
        public bool freeCreatingBone { get; set; }
        public bool normalCreatingRoot { get; set; }

        internal BoneEditorState()
        {
            Reset();
        }

        public void Reset(bool keepSelectedBone = false)
        {
            if (!keepSelectedBone)
                selectedBones = new List<IBone>();

            normalCreating = false;
            freeCreating = false;
            freeMoving = false;
            parenting = false;
            multiselecting = false;
            freeCreatingBone = false;
            normalCreatingRoot = false;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public override bool Equals(object other)
        {
            var otherState = other as BoneEditorState;
            var boolStateEqual = normalCreating == otherState.normalCreating
                && freeCreating == otherState.freeCreating
                && parenting == otherState.parenting
                && freeMoving == otherState.freeMoving
                && multiselecting == otherState.multiselecting
                && freeCreatingBone == otherState.freeCreatingBone
                && normalCreatingRoot == otherState.normalCreatingRoot;

            if (boolStateEqual)
            {
                if (selectedBones.Count != otherState.selectedBones.Count)
                    return false;

                for (var i = 0; i < selectedBones.Count; ++i)
                {
                    if (selectedBones[i] != otherState.selectedBones[i])
                        return false;
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
    internal class BonePresenter
    {
        private IBoneModel m_Model;
        private IBoneHierarchyView m_View;
        private IBoneToolView m_ToolView;
        private IBoneInfoView m_InfoView;
        
        private Dictionary<IBone, List<IBone>> m_ChildrenOfSelected = new Dictionary<IBone, List<IBone>>();
        private Dictionary<IBone, List<IBone>> m_SiblingToMoveTogether = new Dictionary<IBone, List<IBone>>();
        private Dictionary<IBone, List<IBone>> m_OffspringOfSelected = new Dictionary<IBone, List<IBone>>();
        private Dictionary<IBone, List<IBone>> m_OffspringOfParent = new Dictionary<IBone, List<IBone>>();
        private Dictionary<IBone, bool> m_UpdateParentTip = new Dictionary<IBone, bool>();
        private List<IBone> m_ValidToMultipleSelectMove = new List<IBone>();

        public BoneEditorState state { get; set; }

        private bool selectingBone { get { return state.selectedBones.Any(); } }
        private bool selectingMultipleBone { get { return state.selectedBones.Count > 1; } }
        private bool creatingBone { get { return state.normalCreating || state.freeCreating; } }

        public BonePresenter(IBoneModel model, IBoneHierarchyView view, IBoneToolView toolView, IBoneInfoView infoView)
        {
            m_Model = model;
            m_View = view;
            m_ToolView = toolView;
            m_InfoView = infoView;

            state = new BoneEditorState();

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        ~BonePresenter()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void UndoRedoPerformed()
        {
            // Cancel all creating state if undo was performed
            if (creatingBone)
            {
                state.normalCreating = false;
                state.freeCreating = false;
                state.freeCreatingBone = false;
            }
        }

        // Manipulation and display of bones.
        //   - rect : Workspace position and size.
        public void DoBone(Rect rect)
        {
            // Define the working area.
            m_View.SetRect(rect);
            
            if (creatingBone)
                HandleCreation();
            else
            {
                HandleSelection();

                // Early out movement handling for performance.
                if (selectingBone && !state.parenting)
                    HandleMovement();
            }
            
            Draw();
        }

        // Toolbar buttons and shortcut keys to change state
        //   - rect : Workspace position and size.
        public void DoTool(Rect rect)
        {
            // Define the working area.
            m_ToolView.SetRect(rect);

            // Layout and detect input via tool view.
            var normalCreating = m_ToolView.HandleCreate(state.normalCreating, !selectingMultipleBone);
            var freeCreating = m_ToolView.HandleFreeCreate(state.freeCreating, !selectingMultipleBone);
            var freeMoving = m_ToolView.HandleMove(state.freeMoving, selectingBone);
            var parenting = m_ToolView.HandleParent(state.parenting, selectingBone);
            var splitActivated = m_ToolView.HandleSplit(selectingBone);
            var deleteActivated = m_ToolView.HandleDelete(selectingBone);
            
            // Just entered another toggled mode this frame.
            var normalCreationToggled = normalCreating && !state.normalCreating;
            var freeCreationToggled = freeCreating && !state.freeCreating;
            var freeMoveToggled = freeMoving && !state.freeMoving;
            var parentingToggled = parenting && !state.parenting;
            
            // Forward the states
            state.normalCreating = normalCreating;
            state.freeCreating = freeCreating;
            state.freeMoving = freeMoving;
            state.parenting = parenting;
            
            // Restore all state, cancel has highest priority.
            if (m_ToolView.HandleGlobalCancel())
            {
                state.normalCreating = false;
                state.freeCreating = false;
                state.freeCreatingBone = false;
                state.parenting = false;
                state.freeMoving = false;
            }

            // Multiple selection is disable during creation mode.
            if (!state.normalCreating && !state.freeCreating)
                state.multiselecting = m_ToolView.HandleMultipleSelection();

            if (state.normalCreating && normalCreationToggled)
            {
                state.Reset(true);
                state.normalCreating = true;

                if (m_Model.bones.Any())
                {
                    // Always select root if no bone is selected when entering creation mode, if bones existed in the sprite.
                    if (!selectingBone)
                        state.selectedBones.Add(m_Model.bones.First());
                }
                else
                    // Blank state (no bone created in the sprite)
                    state.normalCreatingRoot = true;
            }

            if (state.freeCreating && freeCreationToggled)
            {
                state.Reset(true);
                state.freeCreating = true;
                state.freeCreatingBone = true;

                if (m_Model.bones.Any())
                {
                    // Always select root if no bone is selected when entering creation mode, if bones existed in the sprite.
                    if (!selectingBone)
                        state.selectedBones.Add(m_Model.bones.First());
                }
            }

            if (state.freeMoving && freeMoveToggled)
            {
                state.Reset(true);
                state.freeMoving = true;
            }

            if (state.parenting && parentingToggled)
            {
                state.Reset(true);
                state.parenting = true;
            }

            if (splitActivated)
            {
                state.Reset(true);

                foreach (var bone in state.selectedBones)
                {
                    RecordUndo(bone, "bone split");
                    SplitBone(bone);
                }
            }

            if (deleteActivated)
            {
                state.Reset(true);

                foreach (var bone in state.selectedBones)
                {
                    RecordUndo(bone, "bone delete");
                    DeleteBone(bone);
                }
                state.selectedBones.Clear();
            }
        }

        // Name display and editing
        //   - rect : Workspace position and size.
        public void DoInfoPanel(Rect rect)
        {
            // Early out, don't display if no bone or multiple bones are selected.
            if (!selectingBone || selectingMultipleBone)
                return;

            // Define the working area.
            m_InfoView.SetRect(rect);

            // Display name, handle rename.
            var selectedBone = state.selectedBones[0];
            var newName = selectedBone.name;
            if (m_InfoView.HandleName(ref newName))
                m_Model.SetBoneName(selectedBone, newName);

            if (IsConflictWithOtherBoneNames(selectedBone))
                m_InfoView.DisplayDuplicateBoneNameWarning();

            // Detect if user asked for a "select next bone for renaming"
            if (m_InfoView.HandleNextSelection())
            {
                if (state.selectedBones.Count == 1 && !state.normalCreating && !state.freeCreating)
                {
                    // Cycle to the next bone...
                    var nextBone = m_Model.bones.First();
                    var takeTheNext = false;
                    foreach (var n in m_Model.bones)
                    {
                        if (takeTheNext)
                        {
                            nextBone = n;
                            break;
                        }
                        if (selectedBone == n)
                            takeTheNext = true;
                    }

                    // ... select it.
                    state.selectedBones.Clear();
                    state.selectedBones.Add(nextBone);
                }
            }
        }

        // Bridging SetRawData for IHierarchy
        public void SetRawData(List<UniqueSpriteBone> rawBoneDatas, Vector3 offset)
        {
            m_Model.SetRawData(rawBoneDatas, offset);

            // This usually mean a fresh sprite rect selected, we should reset the state.
            state.Reset();
        }

        // Bridging GetRawData for IHierarchy
        public List<UniqueSpriteBone> GetRawData()
        {
            return m_Model.GetRawData();
        }
        
        // Repopulate the relationship hash tables for current selected bones.
        public void ResetRelationship()
        {
            m_ChildrenOfSelected.Clear();
            m_SiblingToMoveTogether.Clear();
            m_UpdateParentTip.Clear();
            m_OffspringOfSelected.Clear();
            m_OffspringOfParent.Clear();

            foreach (var bone in state.selectedBones)
            {
                List<IBone> childrenOfSelected = new List<IBone>();
                List<IBone> childrenToMoveTogether = new List<IBone>();
                List<IBone> siblingToMoveTogether = new List<IBone>();
                List<IBone> offspringOfSelected = new List<IBone>();
                List<IBone> offspringOfParent = new List<IBone>();

                // Traverse all bones to find relationships.
                foreach (var otherBone in m_Model.bones)
                {
                    // Itself
                    if (otherBone == bone)
                    {
                        if (!bone.isRoot)
                            // Add itself as offspring of its parent. Made sense, right?
                            offspringOfParent.Add(otherBone);
                        continue;
                    }

                    // Immediate child.
                    if (otherBone.parent == bone)
                    {
                        childrenOfSelected.Add(otherBone);

                        if (ShouldSnap(bone.position, otherBone.position))
                            // If close enough, they should move together in normal movement.
                            childrenToMoveTogether.Add(otherBone);
                    }

                    // Sibling.
                    if (otherBone.parent == bone.parent)
                    {
                        if (ShouldSnap(bone.position, otherBone.position))
                            // If close enough, they should move together in normal movement.
                            siblingToMoveTogether.Add(otherBone);
                    }

                    // Child, grandchild, grand(^n)child.
                    if (BoneUtility.IsOffspringOf(otherBone, bone))
                        offspringOfSelected.Add(otherBone);

                    // Parent's child, grandchild, grand(^n)child.
                    if (!bone.isRoot && BoneUtility.IsOffspringOf(otherBone, bone.parent))
                        offspringOfParent.Add(otherBone);
                }

                // If bone is close to parent's tip, it will update parent's tip position when moved (normal move).
                var updateParentTip = !bone.isRoot && ShouldSnap(bone.position, bone.parent.tip);

                m_OffspringOfSelected.Add(bone, offspringOfSelected);
                m_ChildrenOfSelected.Add(bone, childrenOfSelected);
                m_SiblingToMoveTogether.Add(bone, siblingToMoveTogether);
                m_UpdateParentTip.Add(bone, updateParentTip);
                m_OffspringOfParent.Add(bone, offspringOfParent);
            }

            // When multiple bones are selected, movement handling of offspring is disabled if ancestor is selected (normal move).
            m_ValidToMultipleSelectMove = new List<IBone>();
            foreach (var bone in state.selectedBones)
            {
                var offspringOfOtherSelected = false;
                foreach (var c in m_OffspringOfSelected)
                {
                    if (c.Value.Contains(bone))
                    {
                        offspringOfOtherSelected = true;
                        break;
                    }
                }
                if (!offspringOfOtherSelected)
                    m_ValidToMultipleSelectMove.Add(bone);
            }
        }

        // Global mouse click to create bone, then tip, then another bone, then tip, until user cancel.
        private void HandleCreation()
        {
            // Change cursor looks.
            m_View.ShowCreationCursor();

            var position = Vector3.zero;
            // This function return true if user clicked, and fill in the position (rect space).
            if (m_View.HandleFullViewCursor(ref position))
            {
                // Normal creating is a smooth creation series of bones, each is a child of the previous bone. 
                // And looks continuous, meaning the tip of parent always point to the child.
                if (state.normalCreating)
                {
                    RecordUndo(null, "bone create children");

                    if (state.normalCreatingRoot)
                    {
                        if (selectingBone)
                        {
                            var selectedBone = state.selectedBones[0];

                            // Normal creation state > creating root state > selected a bone (root) > define the root's tip.
                            m_Model.MoveTip(selectedBone, position);

                            // Exiting the normal creating root state.
                            state.normalCreatingRoot = false;
                        }
                        else
                        {
                            // Normal creation state > creating root state > nothing selected yet > create the root's bone.
                            var newBone = m_Model.CreateNewRoot(position);
                            SelectSingleBone(newBone);
                        }
                    }
                    else if (selectingBone)
                    {
                        var selectedBone = state.selectedBones[0];

                        // Create a new bone at the tip of current selected bone. 
                        var newBone = m_Model.CreateNewChildBone(selectedBone, selectedBone.tip);

                        // Define the tip of the newly created bone at the position user clicked.
                        m_Model.MoveTip(newBone, position);

                        // Continue creating bones.
                        SelectSingleBone(newBone);
                    }
                    else
                        throw new InvalidOperationException("While not creating a root, there should always be a selected bone.");
                }
                // Free creating is an alternative way to create direct children of selected bone.
                // Each newly created bone is a sibling to each other.
                // The creation is disjointed, meaning the tip of the parent will not follow the newly created child.
                else if (state.freeCreating)
                {
                    if (state.freeCreatingBone)
                    {
                        RecordUndo(null, "bone free create");

                        if (selectingBone)
                        {
                            var selectedBone = state.selectedBones[0];

                            // Free creation state > creating bone state > selected > create the child bone.
                            var newBone = m_Model.CreateNewChildBone(selectedBone, position);
                            SelectSingleBone(newBone);
                        }
                        else
                        {
                            // Free creation state > creating bone state > nothing selected yet > create the root's bone.
                            var newBone = m_Model.CreateNewRoot(position);
                            SelectSingleBone(newBone);
                        }

                        // Exit the creating bone state, next should be defining the tip for this newly created bone.
                        state.freeCreatingBone = false;
                    }
                    else if (selectingBone)
                    {
                        var selectedBone = state.selectedBones[0];

                        // Define the tip of the newly created bone at the position user clicked.
                        m_Model.MoveTip(selectedBone, position);

                        // Reselect the parent (unless the newly created is a root bone).
                        // We are creating direct child remember?
                        if (state.freeCreating && !selectedBone.isRoot)
                            SelectSingleBone(selectedBone.parent);

                        // Goes back to creating bone state, tip is done.
                        state.freeCreatingBone = true;
                    }
                    else
                        throw new InvalidOperationException("While not creating a root, there should always be a selected bone.");
                }

                // There's a click and potentially new stuff on screen, repaint.
                m_View.Refresh();
            }
        }

        // Click on bone body and tip. Handle parenting state here as well.
        private void HandleSelection()
        {
            // Tip first. If a bone near the tip is clicked later, the bone will be selected instead of tip.
            foreach (var bone in m_Model.bones)
            {
                if (m_View.HandleTipSelect(bone))
                {
                    SelectSingleBone(bone);
                    m_View.Refresh();

                    // Always reset relationship after a new selection.
                    ResetRelationship();

                    m_InfoView.SelectionChanged();

                    break;
                }
            }

            // Bone second. Replace selected tip, if bone is clicked on as well.
            foreach (var bone in m_Model.bones)
            {
                if (m_View.HandleBoneSelect(bone))
                {
                    // During parenting state, a click on a bone will not select the bone, but mark this bone as new parent.
                    if (state.parenting)
                    {
                        foreach (var selectedBone in state.selectedBones)
                        {
                            if (bone == selectedBone)
                                throw new InvalidOperationException("Cannot parent a bone to itself.");
                            if (BoneUtility.IsOffspringOf(bone, selectedBone))
                                throw new InvalidOperationException(string.Format("Cannot parent {0} to {1}. This will create a loop.", bone.name, selectedBone.name));
                        }

                        RecordUndo(bone, "bone parent");

                        foreach (var selectedBone in state.selectedBones)
                            m_Model.Parent(selectedBone, bone);

                        // Exit parenting state.
                        state.parenting = false;

                        m_View.Refresh();
                        break;
                    }
                    else
                    {
                        if (state.multiselecting)
                            state.selectedBones.Add(bone);
                        else if (!state.selectedBones.Contains(bone))
                            SelectSingleBone(bone);

                        m_View.Refresh();

                        // Always reset relationship after a new selection.
                        ResetRelationship();

                        m_InfoView.SelectionChanged();

                        if (!state.multiselecting)
                            break;
                    }
                }
            }
        }

        // Drag on bone.
        private void HandleMovement()
        {
            foreach (var selectedBone in state.selectedBones)
            {
                // Drag on the bone (head) of the bone.
                var d = m_View.HandleBoneNodeDrag(selectedBone);
                {
                    if (d.magnitude > 0.0f)
                        MoveBoneNode(selectedBone, d);
                }

                // Drag on the tip (tail) of the bone.
                var t = m_View.HandleBoneTipDrag(selectedBone);
                {
                    if (t.magnitude > 0.0f)
                        MoveBoneTip(selectedBone, t);
                }

                // Drag on the body (middle portion of head to tail) of the bone.
                var b = m_View.HandleBoneDrag(selectedBone);
                {
                    if (b.magnitude > 0.0f)
                    {
                        if (selectingMultipleBone)
                        {
                            // MoveBoneBody take absolute position for each bone.
                            // Using the delta of the movement, we recalculate absolute position for each selected bone later.
                            var delta = b - selectedBone.position;
                            foreach (var validBone in m_ValidToMultipleSelectMove)
                                MoveBone(validBone, validBone.position + delta, true);
                        }
                        else
                            MoveBone(selectedBone, b, false);
                    }
                }
            }
        }
        
        private void MoveBoneNode(IBone bone, Vector3 d)
        {
            RecordUndo(bone, "bone bone move");

            // In normal movement mode, moving a bone node will move only the node (head) but keep the tip intact (retaining tip's position).
            // All its offspring will not be affected by this movement.
            // If the parent tip is snapped to this bone, the parent tip will move together with this bone.
            // If any sibling(s) bone is snapped to this bone, those will move together too.
            if (!state.freeMoving)
            {
                var offspringOfSelected = m_OffspringOfSelected[bone];
                var offspringOfParent = m_OffspringOfParent[bone];
                var siblingToMoveTogether = m_SiblingToMoveTogether[bone];
                
                // Backup the old tip's position, moving the bone will alter the tip, since tip is calculated from bone's position.
                var oldtip = bone.tip;

                // Move the bone to the destinate position, snap if necessary.
                var snapped = false;
                if (!bone.isRoot && ShouldSnap(d, bone.parent.tip))
                {
                    m_Model.MoveBone(bone, bone.parent.tip);
                    snapped = true;
                }
                else if (!bone.isRoot && ShouldSnap(d, bone.parent.position))
                {
                    m_Model.MoveBone(bone, bone.parent.position);

                    snapped = true;
                }
                if (!snapped)
                    m_Model.MoveBone(bone, d);

                // Restore the tip to its old position
                m_Model.MoveTip(bone, oldtip);

                // Sibling bones should move together if they snapped to the bone before.
                foreach (var siblingBone in siblingToMoveTogether)
                {
                    // Move, restore tip.
                    var oldChildTip = siblingBone.tip;
                    m_Model.MoveBone(siblingBone, d);
                    m_Model.MoveTip(siblingBone, oldChildTip);
                }

                // Update parent's tip position if this bone was snapped to the tip before.
                if (m_UpdateParentTip[bone])
                {
                    m_Model.MoveTip(bone.parent, d);
                }
            }
            // In free movement mode, moving a bone will move the head but keep the tip intact.
            // All its offspring will not be affected by this movement.
            // Unlike normal movement mode, sibling and parent tip will not be affected by this movement.
            else
            {
                var oldtip = bone.tip;
                m_Model.MoveBone(bone, d);
                m_Model.MoveTip(bone, oldtip);
            }

            m_View.Refresh();
        }

        private void MoveBoneTip(IBone bone, Vector3 t)
        {
            RecordUndo(bone, "bone tip move");

            // In both movement mode, moving the tip will keep the bone intact in original position, only move the tip.
            // All its offspring will not be affected by this movement.

            if (!state.freeMoving)
            {
                // The only different between 2 mode in moving tip, normal movement mode will snap the tip to its children.
                var childrenToSnapToTip = m_ChildrenOfSelected[bone];
                var snapped = false;
                foreach (var childBone in childrenToSnapToTip)
                {
                    if (ShouldSnap(t, childBone.position))
                    {
                        m_Model.MoveTip(bone, childBone.position);
                        snapped = true;
                    }
                }
                if (!snapped)
                    m_Model.MoveTip(bone, t);
            }
            else
            {
                m_Model.MoveTip(bone, t);
            }

            m_View.Refresh();
        }

        private void MoveBone(IBone bone, Vector3 b, bool disableSnap)
        {
            RecordUndo(bone, "bone body move");

            // In normal movement mode, moving a bone body will move it, and all its offspring.
            // If the parent tip is snapped to this bone, the parent tip will move together with this bone.
            // If any sibling(s) bone is snapped to this bone, those bone will move together, but just the bone, tip and offspring remain intact.
            if (!state.freeMoving)
            {
                var snapped = false;
                if (!bone.isRoot && ShouldSnap(b, bone.parent.tip) && !disableSnap)
                {
                    m_Model.MoveBone(bone, bone.parent.tip, false);
                    snapped = true;
                }
                else if (!bone.isRoot && ShouldSnap(b, bone.parent.position) && !disableSnap)
                {
                    m_Model.MoveBone(bone, bone.parent.position, false);
                    snapped = true;
                }
                if (!snapped)
                    m_Model.MoveBone(bone, b, false);
                
                // There are more works to do if we need to update the parent's tip
                if (m_UpdateParentTip[bone])
                {
                    var siblingToMoveTogether = m_SiblingToMoveTogether[bone];
                    var offspringOfSelected = m_OffspringOfSelected[bone];
                    var offspringOfParent = m_OffspringOfParent[bone];
                    
                    // Move the sibling alongside with this bone.
                    foreach (var siblingBone in siblingToMoveTogether)
                    {
                        var oldChildTip = siblingBone.tip;
                        m_Model.MoveBone(siblingBone, bone.position);

                        // Maintain the previous tip position.
                        m_Model.MoveTip(siblingBone, oldChildTip);
                    }
                    
                    // Move the parent's tip, all offspring of it are in updated position and marked to retain it.
                    m_Model.MoveTip(bone.parent, b);
                }
            }
            // In free movement mode, just move the bone, all offspring are not affected.
            // No sibling movement, no parent tip movement too.
            else
            {
                m_Model.MoveBone(bone, b);
            }

            m_View.Refresh();
        }

        // Becomes 2 bones. 
        // Original one is the parent, tip shorten to middle point.
        // New bone start at the middle point, parented to original bone.
        // All direct children of original bone belongs to the new bone.
        private void SplitBone(IBone bone)
        {
            List<IBone> directChildren = new List<IBone>();
            foreach (var otherBone in m_Model.bones)
            {
                if (otherBone.parent == bone)
                    directChildren.Add(otherBone);
            }

            var middlePoint = Vector3.Lerp(bone.position, bone.tip, 0.5f);
            var middleBone = m_Model.CreateNewChildBone(bone, middlePoint);
            m_Model.MoveTip(middleBone, bone.tip);
            m_Model.MoveTip(bone, middlePoint);

            foreach (var oldChild in directChildren)
                m_Model.Parent(oldChild, middleBone);
        }
        
        private void DeleteBone(IBone bone)
        {
            // Remove the whole bone hierarchy.
            if (bone.isRoot)
            {
                // Delete everything. (Less efficient code, but performance is not crucial at this time).
                foreach (var otherBone in m_Model.bones.Reverse())
                    m_Model.DeleteBone(otherBone);
            }
            // Remove just the selected bone.
            // All direct children will belong to the parent of deleted bone.
            else
            {
                List<IBone> directChildOfDeletingBone = new List<IBone>();
                foreach (var otherBone in m_Model.bones)
                {
                    if (otherBone.parent == bone)
                        directChildOfDeletingBone.Add(otherBone);
                }

                foreach(var child in directChildOfDeletingBone)
                    m_Model.Parent(child, bone.parent);

                // Remove the bone when all children transfered.
                m_Model.DeleteBone(bone);
            }
        }

        // Only drawing calls. No input, no control ids.
        private void Draw()
        {
            foreach (var bone in m_Model.bones)
            {
                if (state.selectedBones.Contains(bone))
                {
                    m_View.DrawBone(bone, true);
                    m_View.DrawTip(bone);
                    if (!bone.isRoot && !ShouldSnap(bone.position, bone.parent.tip))
                        m_View.DrawLinkToParent(bone, true);
                }
                else
                {
                    m_View.DrawBone(bone, false);
                    if (!bone.isRoot && !ShouldSnap(bone.position, bone.parent.tip))
                        m_View.DrawLinkToParent(bone, false);
                }
            }

            if (creatingBone && selectingBone)
            {
                if (state.normalCreating || !state.freeCreatingBone)
                {
                    // Draw a preview bone to the position of mouse cursor
                    m_View.DrawPreviewTipFromTip(state.selectedBones[0]);

                    // Keep refreshing while previewing, since mouse cursor will move.
                    m_View.Refresh();
                }
                else if (state.freeCreatingBone)
                {
                    m_View.DrawPreviewLinkFromBone(state.selectedBones[0]);
                    m_View.Refresh();
                }
            }
        }

        private void RecordUndo(IBone bone, string operationName)
        {
            m_Model.RecordUndo(bone, operationName);
        }
        
        private void SelectSingleBone(IBone bone)
        {
            state.selectedBones.Clear();
            state.selectedBones.Add(bone);
        }

        private bool ShouldSnap(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) <= m_View.GetBoneRadius();
        }

        private bool IsConflictWithOtherBoneNames(IBone bone)
        {
            foreach (var otherBone in m_Model.bones)
            {
                if (bone != otherBone && bone.name == otherBone.name)
                    return true;
            }
            return false;
        }
    }
}
