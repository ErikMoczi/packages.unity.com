using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.SettingsManagement;

namespace UnityEditor.ProBuilder.Actions
{
	sealed class SelectMaterial : MenuAction
	{
		GUIContent gc_restrictToSelection = new GUIContent("Current Selection", "Optionally restrict the matches to only those faces on currently selected objects.");
		Pref<bool> m_RestrictToSelectedObjects = new Pref<bool>("SelectMaterial.restrictToSelectedObjects", false, SettingScope.Project);

		public override ToolbarGroup group
		{
			get { return ToolbarGroup.Selection; }
		}

		public override Texture2D icon
		{
			get { return IconUtility.GetIcon("Toolbar/Selection_SelectByMaterial", IconSkin.Pro); }
		}

		public override TooltipContent tooltip
		{
			get { return s_Tooltip; }
		}

		static readonly TooltipContent s_Tooltip = new TooltipContent
		(
			"Select by Material",
			"Selects all faces matching the selected materials."
		);

		public override SelectMode validSelectModes
		{
			get { return SelectMode.Face | SelectMode.Texture; }
		}

		public override bool enabled
		{
			get { return base.enabled && MeshSelection.selectedFaceCount > 0; }
		}

		protected override MenuActionState optionsMenuState
		{
			get { return MenuActionState.VisibleAndEnabled; }
		}

		protected override void OnSettingsGUI()
		{
			GUILayout.Label("Select Material Options", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			m_RestrictToSelectedObjects.value = EditorGUILayout.Toggle(gc_restrictToSelection, m_RestrictToSelectedObjects);

			if (EditorGUI.EndChangeCheck())
				ProBuilderSettings.Save();

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Select Faces with Material"))
			{
				DoAction();
				SceneView.RepaintAll();
			}
		}

		public override ActionResult DoAction()
		{
			UndoUtility.RecordSelection(MeshSelection.TopInternal(), "Select Faces with Material");

			bool restrictToSelection = m_RestrictToSelectedObjects;

			HashSet<Material> sel = new HashSet<Material>(MeshSelection.TopInternal().SelectMany(x => x.selectedFacesInternal.Select(y => y.material).Where(z => z != null)));
			List<GameObject> newSelection = new List<GameObject>();

			foreach (ProBuilderMesh pb in restrictToSelection ? MeshSelection.TopInternal() : Object.FindObjectsOfType<ProBuilderMesh>())
			{
				IEnumerable<Face> matches = pb.facesInternal.Where(x => sel.Contains(x.material));

				if (matches.Count() > 0)
				{
					newSelection.Add(pb.gameObject);
					pb.SetSelectedFaces(matches);
				}
			}

			Selection.objects = newSelection.ToArray();

			ProBuilderEditor.Refresh();

			return new ActionResult(ActionResult.Status.Success, "Select Faces with Material");
		}
	}
}
