﻿
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor
{
    [System.Serializable]
    internal class SystemInclusionList
    {
        private readonly List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>> cachedMatches = new List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>>();
        private readonly Dictionary<ComponentGroup, ComponentGroupGUIControl> cachedControls = new Dictionary<ComponentGroup, ComponentGroupGUIControl>();
        private bool repainted = true;

        [SerializeField] private bool showSystems;

        public void OnGUI(World world, Entity entity)
        {
            ++EditorGUI.indentLevel;
            GUILayout.BeginVertical(GUI.skin.box);
            showSystems = EditorGUILayout.Foldout(showSystems, "Used by Systems");

            if (showSystems)
            {
                if (repainted == true)
                {
                    cachedMatches.Clear();
                    WorldDebuggingTools.MatchEntityInComponentGroups(world, entity, cachedMatches);
                    foreach (var pair in cachedMatches)
                    {
                        foreach (var componentGroup in pair.Item2)
                        {
                            if (!cachedControls.ContainsKey(componentGroup))
                            {
                                cachedControls.Add(componentGroup, new ComponentGroupGUIControl(componentGroup.GetQueryTypes(), false));
                            }
                        }
                    }
                    repainted = false;
                }

                foreach (var pair in cachedMatches)
                {
                    var type = pair.Item1.GetType();
                    GUILayout.Label(new GUIContent(type.Name, type.AssemblyQualifiedName));
                    ++EditorGUI.indentLevel;
                    foreach (var componentGroup in pair.Item2)
                    {
                        cachedControls[componentGroup].OnGUILayout(EditorGUIUtility.currentViewWidth - 60f);
                        if (GUILayout.Button("Show", GUILayout.ExpandWidth(false)))
                        {
                            EntityDebugger.SetAllSelections(world, pair.Item1 as ComponentSystemBase, new EntityListQuery(componentGroup), entity);
                        }
                    }

                    --EditorGUI.indentLevel;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    repainted = true;
                }
            }
            GUILayout.EndVertical();

            --EditorGUI.indentLevel;
        }
    }
}
