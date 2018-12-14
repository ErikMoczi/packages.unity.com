using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.U2D.Layout;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class CopyTool : MeshToolWrapper
    {
        private CopyToolView m_CopyToolView;

        public float pixelsPerUnit
        {
            private get;
            set;
        }

        internal override void OnCreate()
        {
            m_CopyToolView = new CopyToolView();
            m_CopyToolView.onPasteActivated += OnPasteActivated;
        }

        public override void Initialize(LayoutOverlay layout)
        {
            m_CopyToolView.Initialize(layout);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            m_CopyToolView.Show();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            m_CopyToolView.Hide();
        }

        private void CopyMeshFromSpriteCache(SpriteCache sprite, SkinningCopySpriteData skinningSpriteData)
        {
            meshTool.SetupSprite(sprite);
            skinningSpriteData.vertices = meshTool.mesh.vertices;
            skinningSpriteData.indices = meshTool.mesh.indices;
            skinningSpriteData.edges = meshTool.mesh.edges;
            skinningSpriteData.boneWeightNames = new List<string>();
            foreach (var bone in meshTool.mesh.bones)
            {
                skinningSpriteData.boneWeightNames.Add(bone.name);
            }
        }

        public void OnCopyActivated()
        {
            SkinningCopyData skinningCopyData;
            var selectedSprite = skinningCache.selectedSprite;
            if (selectedSprite == null)
            {
                skinningCopyData = CopyAll();
            }
            else
            {
                skinningCopyData = CopySingle();
            }

            if (skinningCopyData != null)
                EditorGUIUtility.systemCopyBuffer = SkinningCopyUtility.SerializeSkinningCopyDataToString(skinningCopyData);
            skinningCache.events.copy.Invoke();
        }

        public SkinningCopyData CopyAll()
        {
            var skinningCopyData = new SkinningCopyData();
            skinningCopyData.pixelsPerUnit = pixelsPerUnit;

            var sprites = skinningCache.GetSprites();
            foreach (var sprite in sprites)
            {
                var skinningSpriteData = new SkinningCopySpriteData();
                skinningSpriteData.spriteName = sprite.name;

                var skeleton = skinningCache.GetEffectiveSkeleton(sprite);
                if (skeleton.BoneCount > 0)
                {
                    if (skinningCache.hasCharacter)
                        skinningSpriteData.spriteBones = skeleton.bones.ToSpriteBone(Matrix4x4.identity).ToList();    
                    else
                        skinningSpriteData.spriteBones = GetSpriteBoneDataRecursively(skeleton.bones[0]);
                }
                if (meshTool != null)
                {
                    CopyMeshFromSpriteCache(sprite, skinningSpriteData);
                }
                skinningCopyData.copyData.Add(skinningSpriteData);
            }

            if (meshTool != null)
            {
                meshTool.SetupSprite(null);
            }

            return skinningCopyData;
        }

        public SkinningCopyData CopySingle()
        {
            var skinningCopyData = new SkinningCopyData();
            skinningCopyData.pixelsPerUnit = pixelsPerUnit;

            BoneCache rootBone;
            var selectedSprite = skinningCache.selectedSprite;
            var selection = skinningCache.skeletonSelection;
            if (selection.Count == 0)
            {
                if (selectedSprite == null || (skinningCache.mode == SkinningMode.SpriteSheet && skinningCache.hasCharacter))
                    return null;
                var skeleton = skinningCache.GetEffectiveSkeleton(selectedSprite);
                if (skeleton.BoneCount == 0)
                    return null;

                rootBone = skeleton.bones[0];
            }
            else
            {
                rootBone = selection.root;
            }

            var skinningSpriteData = new SkinningCopySpriteData();
            skinningCopyData.copyData.Add(skinningSpriteData);

            skinningSpriteData.spriteBones = GetSpriteBoneDataRecursively(rootBone);
            if (selectedSprite != null && skinningCache.mode == SkinningMode.Character)
            {
                // Offset the bones based on the currently selected Sprite in Character mode
                var characterPart = selectedSprite.GetCharacterPart();
                if (characterPart != null)
                {
                    var offset = characterPart.position;
                    var rootSpriteBone = skinningSpriteData.spriteBones[0];
                    rootSpriteBone.position = rootSpriteBone.position - offset;
                    skinningSpriteData.spriteBones[0] = rootSpriteBone;
                }
            }

            if (selectedSprite != null && meshTool != null)
            {
                CopyMeshFromSpriteCache(selectedSprite, skinningSpriteData);
            }
            return skinningCopyData;
        }
        
        private List<SpriteBone> GetSpriteBoneDataRecursively(BoneCache bone)
        {
            var bones = new List<SpriteBone>(bone.skeleton.BoneCount);
            AppendSpriteBoneDataRecursively(bones, bone, -1);
            return bones;
        }

        private void AppendSpriteBoneDataRecursively(List<SpriteBone> spriteBones, BoneCache bone, int parentIndex)
        {
            int currentParentIndex = spriteBones.Count;

            var spriteBone = new SpriteBone();
            spriteBone.name = bone.name;
            spriteBone.parentId = parentIndex;
            spriteBone.position = bone.localPosition;
            spriteBone.position = new Vector3(spriteBone.position.x, spriteBone.position.y, bone.depth);
            spriteBone.rotation = bone.localRotation;
            spriteBone.length = bone.localLength;
            spriteBones.Add(spriteBone);

            foreach (var child in bone)
            {
                var childBone = child as BoneCache;

                if (childBone != null)
                    AppendSpriteBoneDataRecursively(spriteBones, childBone, currentParentIndex);
            }
        }

        public void OnPasteActivated(bool bone, bool mesh, bool flipX, bool flipY)
        {
            var copyBuffer = EditorGUIUtility.systemCopyBuffer;
            if (!SkinningCopyUtility.CanDeserializeStringToSkinningCopyData(copyBuffer))
            {
                Debug.LogError(TextContent.copyError1);
                return;
            }

            var skinningCopyData = SkinningCopyUtility.DeserializeStringToSkinningCopyData(copyBuffer);
            if (skinningCopyData == null || skinningCopyData.copyData.Count == 0)
            {
                Debug.LogError(TextContent.copyError2);
                return;
            }

            var scale = 1f;
            if (skinningCopyData.pixelsPerUnit > 0f)
                scale = pixelsPerUnit / skinningCopyData.pixelsPerUnit;

            var sprites = skinningCache.GetSprites();
            var copyMultiple = skinningCopyData.copyData.Count > 1;
            if (copyMultiple && skinningCopyData.copyData.Count != sprites.Length && mesh)
            {
                Debug.LogError(String.Format(TextContent.copyError3, sprites.Length, skinningCopyData.copyData.Count));
                return;
            }

            using (skinningCache.UndoScope(TextContent.pasteData))
            {
                BoneCache[] newBones = null;
                if (copyMultiple && skinningCache.hasCharacter)
                {
                    var skinningSpriteData = skinningCopyData.copyData[0];
                    newBones = skinningCache.CreateBoneCacheFromSpriteBones(skinningSpriteData.spriteBones.ToArray(), scale);
                    var skeleton = skinningCache.character.skeleton;
                    skeleton.SetBones(newBones);
                    skinningCache.events.skeletonTopologyChanged.Invoke(skeleton);
                }

                foreach (var skinningSpriteData in skinningCopyData.copyData)
                {
                    SpriteCache sprite;
                    if (!String.IsNullOrEmpty(skinningSpriteData.spriteName))
                    {
                        sprite = sprites.FirstOrDefault(x => x.name == skinningSpriteData.spriteName);
                    }
                    else
                    {
                        sprite = skinningCache.selectedSprite;
                    }
                    if (sprite == null)
                        continue;

                    if (bone && (!skinningCache.hasCharacter || !copyMultiple))
                    {
                        newBones = PasteSkeletonBones(sprite, skinningSpriteData.spriteBones, flipX, flipY, scale);
                    }

                    if (mesh && meshTool != null)
                    {
                        PasteMesh(sprite, skinningSpriteData, flipX, flipY, scale, newBones);
                    }
                }

                if (newBones != null)
                {
                    skinningCache.skeletonSelection.elements = newBones;
                    skinningCache.events.boneSelectionChanged.Invoke();
                }
            }
            skinningCache.events.paste.Invoke(bone, mesh, flipX, flipY);
        }

        private Vector3 GetFlippedBonePosition(BoneCache bone, Vector2 startPosition, Rect spriteRect
            , bool flipX, bool flipY)
        {
            Vector3 position = startPosition;
            if (flipX)
            {
                position.x += spriteRect.width - bone.position.x;
            }
            else
            {
                position.x += bone.position.x;
            }

            if (flipY)
            {
                position.y += spriteRect.height - bone.position.y;
            }
            else
            {
                position.y += bone.position.y;
            }

            position.z = bone.position.z;
            return position;
        }

        private Quaternion GetFlippedBoneRotation(BoneCache bone, bool flipX, bool flipY)
        {
            var euler = bone.rotation.eulerAngles;
            if (flipX)
            {
                if (euler.z <= 180)
                {
                    euler.z = 180 - euler.z;
                }
                else
                {
                    euler.z = 540 - euler.z;
                }
            }
            if (flipY)
            {
                euler.z = 360 - euler.z;
            }
            return Quaternion.Euler(euler);
        }

        public BoneCache[] PasteSkeletonBones(SpriteCache sprite, List<SpriteBone> spriteBones, bool flipX, bool flipY, float scale = 1.0f)
        {
            var newBones = skinningCache.CreateBoneCacheFromSpriteBones(spriteBones.ToArray(), scale);
            if (newBones.Length == 0)
                return null;

            if (sprite == null || (skinningCache.mode == SkinningMode.SpriteSheet && skinningCache.hasCharacter))
                return null;

            var spriteRect = sprite.textureRect;
            var skeleton = skinningCache.GetEffectiveSkeleton(sprite);

            var rectPosition = spriteRect.position;
            if (skinningCache.mode == SkinningMode.Character)
            {
                var characterPart = sprite.GetCharacterPart();
                if (characterPart == null)
                    return null;
                rectPosition = characterPart.position;
            }

            var newPositions = new Vector3[newBones.Length];
            var newRotations = new Quaternion[newBones.Length];
            for (var i = 0; i < newBones.Length; ++i)
            {
                newPositions[i] = GetFlippedBonePosition(newBones[i], rectPosition, spriteRect, flipX, flipY);
                newRotations[i] = GetFlippedBoneRotation(newBones[i], flipX, flipY);
            }
            for (var i = 0; i < newBones.Length; ++i)
            {
                newBones[i].position = newPositions[i];
                newBones[i].rotation = newRotations[i];
            }

            if (skinningCache.mode == SkinningMode.SpriteSheet)
            {
                skeleton.SetBones(newBones);
            }
            else
            {
                skeleton.AddBones(newBones);

                var bones = skeleton.bones;

                // Update names of all newly pasted bones
                foreach (var bone in newBones)
                    bone.name = SkeletonController.AutoBoneName(bone.parentBone, bones);
                
                skeleton.SetDefaultPose();
            }

            skinningCache.events.skeletonTopologyChanged.Invoke(skeleton);
            return newBones;
        }

        public void PasteMesh(SpriteCache sprite, SkinningCopySpriteData skinningSpriteData, bool flipX, bool flipY, float scale, BoneCache[] newBones)
        {
            if (sprite == null)
                return;

            meshTool.SetupSprite(sprite);
            meshTool.mesh.vertices = skinningSpriteData.vertices;
            if (!Mathf.Approximately(scale, 1f) || flipX || flipY)
            {
                var spriteRect = sprite.textureRect;
                foreach (var vertex in meshTool.mesh.vertices)
                {
                    var position = vertex.position;
                    if (!Mathf.Approximately(scale, 1f))
                        position = position * scale;
                    if (flipX)
                        position.x = spriteRect.width - vertex.position.x;
                    if (flipY)
                        position.y = spriteRect.height - vertex.position.y;
                    vertex.position = position;
                }
            }
            meshTool.mesh.indices = skinningSpriteData.indices;
            meshTool.mesh.edges = skinningSpriteData.edges;

            if (newBones != null)
            {
                // Update bone weights with new bone indices
                int[] copyBoneToNewBones = new int[skinningSpriteData.boneWeightNames.Count];
                for (int i = 0; i < skinningSpriteData.boneWeightNames.Count; ++i)
                {
                    copyBoneToNewBones[i] = -1;
                    var boneName = skinningSpriteData.boneWeightNames[i];
                    for (int j = 0; j < skinningSpriteData.spriteBones.Count; ++j)
                    {
                        if (skinningSpriteData.spriteBones[j].name == boneName)
                        {
                            copyBoneToNewBones[i] = j;
                            break;
                        }
                    }
                }

                // Remap new bone indexes from copied bone indexes
                foreach (var vertex in meshTool.mesh.vertices)
                {
                    var editableBoneWeight = vertex.editableBoneWeight;

                    for (var i = 0; i < editableBoneWeight.Count; ++i)
                    {
                        if (!editableBoneWeight[i].enabled)
                            continue;

                        var boneIndex = copyBoneToNewBones[editableBoneWeight[i].boneIndex];
                        
                        if (boneIndex != -1)
                            editableBoneWeight[i].boneIndex = boneIndex;
                    }
                }

                // Update associated bones for mesh
                meshTool.mesh.SetCompatibleBoneSet(newBones);
                meshTool.mesh.bones = newBones; // Fixes weights for bones that do not exist

                // Update associated bones for character
                if (skinningCache.hasCharacter)
                {
                    var characterPart = sprite.GetCharacterPart();
                    if (characterPart != null)
                    {
                        characterPart.bones = newBones;
                        skinningCache.events.characterPartChanged.Invoke(characterPart);
                    }
                }
            }
            meshTool.UpdateMesh();
        }
    }

    internal class CopyToolView
    {
        private PastePanel m_PastePanel;

        public event Action<bool, bool, bool, bool> onPasteActivated = (bone, mesh, flipX, flipY) => {};

        public void Show()
        {
            m_PastePanel.SetHiddenFromLayout(false);
        }

        public void Hide()
        {
            m_PastePanel.SetHiddenFromLayout(true);
        }

        public void Initialize(LayoutOverlay layoutOverlay)
        {
            m_PastePanel = PastePanel.GenerateFromUXML();
            BindElements();
            layoutOverlay.rightOverlay.Add(m_PastePanel);
            m_PastePanel.SetHiddenFromLayout(true);
        }

        void BindElements()
        {
            m_PastePanel.onPasteActivated += OnPasteActivated;
        }

        void OnPasteActivated(bool bone, bool mesh, bool flipX, bool flipY)
        {
            onPasteActivated(bone, mesh, flipX, flipY);
        }
    }
}
