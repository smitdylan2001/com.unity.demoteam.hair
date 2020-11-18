﻿using UnityEngine;
using UnityEditor;

namespace Unity.DemoTeam.Hair
{
	using static HairGUILayout;

	[CustomEditor(typeof(Groom))]
	public class GroomEditor : Editor
	{
		Editor groomAssetEditor;

		SerializedProperty _groomAsset;
		SerializedProperty _groomAssetQuickEdit;

		SerializedProperty _settingsRoots;
		SerializedProperty _settingsRoots_rootsAttached;
		SerializedProperty _settingsStrands;

		SerializedProperty _settingsSolver;
		SerializedProperty _settingsVolume;
		SerializedProperty _settingsDebug;

		void OnEnable()
		{
			_groomAsset = serializedObject.FindProperty("groomAsset");
			_groomAssetQuickEdit = serializedObject.FindProperty("groomAssetQuickEdit");

			_settingsRoots = serializedObject.FindProperty("settingsRoots");
			_settingsRoots_rootsAttached = _settingsRoots.FindPropertyRelative("rootsAttached");
			_settingsStrands = serializedObject.FindProperty("settingsStrands");

			_settingsSolver = serializedObject.FindProperty("solverSettings");
			_settingsVolume = serializedObject.FindProperty("volumeSettings");
			_settingsDebug = serializedObject.FindProperty("debugSettings");
		}

		void OnDisable()
		{
			if (groomAssetEditor != null)
			{
				DestroyImmediate(groomAssetEditor);
			}
		}

		public override void OnInspectorGUI()
		{
			var groom = target as Groom;
			if (groom == null)
				return;

			EditorGUILayout.LabelField("Instance", EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.BeginVertical(HairGUIStyles.settingsBox);
			{
				DrawAssetGUI();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Strand settings", EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.BeginVertical(HairGUIStyles.settingsBox);
			{
				DrawStrandSettingsGUI();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Simulation settings", EditorStyles.centeredGreyMiniLabel);
			EditorGUILayout.BeginVertical(HairGUIStyles.settingsBox);
			{
				DrawSimulationSettingsGUI();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(groom.componentGroupsChecksum, EditorStyles.centeredGreyMiniLabel);
		}

		public void DrawAssetGUI()
		{
			var groom = target as Groom;
			if (groom == null)
				return;

			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.PropertyField(_groomAsset);
				_groomAssetQuickEdit.boolValue = GUILayout.Toggle(_groomAssetQuickEdit.boolValue, "Quick Edit", EditorStyles.miniButton);
			}

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}

			var groomAsset = groom.groomAsset;
			if (groomAsset != null && _groomAssetQuickEdit.boolValue)
			{
				Editor.CreateCachedEditor(groomAsset, null, ref groomAssetEditor);
				EditorGUILayout.BeginVertical(HairGUIStyles.settingsBox);
				{
					(groomAssetEditor as GroomAssetEditor).DrawImporterGUI();
				}
				EditorGUILayout.EndVertical();
			}

			if (GUILayout.Button("Reload"))
			{
				groom.componentGroupsChecksum = string.Empty;
			}
		}

		public void DrawStrandSettingsGUI()
		{
			var groom = target as Groom;
			if (groom == null)
				return;

			EditorGUI.BeginChangeCheck();
			{
				StructPropertyFieldsWithHeader(_settingsRoots);
				if (_settingsRoots_rootsAttached == null)
				{
					using (new EditorGUI.IndentLevelScope())
					{
						EditorGUILayout.HelpBox("Root attachments require package: 'com.unity.demoteam.digital-human'.", MessageType.None, wide: true);
					}
				}

				EditorGUILayout.Space();
				StructPropertyFieldsWithHeader(_settingsStrands);
			}

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		public void DrawSimulationSettingsGUI()
		{
			var groom = target as Groom;
			if (groom == null)
				return;

			if (groom.solverData != null && groom.solverData.Length > 0)
			{
				var strandSolver = groom.solverSettings.method;
				var strandMemoryLayout = groom.solverData[0].memoryLayout;
				var strandParticleCount = groom.solverData[0].cbuffer._StrandParticleCount;

				switch (strandSolver)
				{
					case HairSim.SolverSettings.Method.GaussSeidelReference:
						EditorGUILayout.HelpBox("Performance warning: Using slow reference solver.", MessageType.Warning, wide: true);
						break;

					case HairSim.SolverSettings.Method.GaussSeidel:
						if (strandMemoryLayout != GroomAsset.MemoryLayout.Interleaved)
						{
							EditorGUILayout.HelpBox("Performance warning: Gauss-Seidel solver performs better with memory layout 'Interleaved'.\nConsider changing memory layout in the asset.", MessageType.Warning, wide: true);
						}
						break;

					case HairSim.SolverSettings.Method.Jacobi:
						if (strandParticleCount != 16 &&
							strandParticleCount != 32 &&
							strandParticleCount != 64 &&
							strandParticleCount != 128)
						{
							EditorGUILayout.HelpBox("Configuration error: Jacobi solver requires strand particle count of 16, 32, 64 or 128.\nUsing slow reference solver as fallback.\nConsider resampling curves in asset.", MessageType.Error, wide: true);
						}
						else if (strandMemoryLayout != GroomAsset.MemoryLayout.Sequential)
						{
							EditorGUILayout.HelpBox("Performance warning: Jacobi solver performs better with memory layout 'Sequential'.\nConsider changing memory layout in the asset.", MessageType.Warning, wide: true);
						}
						break;
				}
			}

			EditorGUI.BeginChangeCheck();
			{
				StructPropertyFieldsWithHeader(_settingsSolver, "Settings Solver");

				EditorGUILayout.Space();
				StructPropertyFieldsWithHeader(_settingsVolume, "Settings Volume");
				using (new EditorGUI.IndentLevelScope())
				{
					var countCapsule = groom.volumeData.cbuffer._BoundaryCapsuleCount;
					var countSphere = groom.volumeData.cbuffer._BoundarySphereCount;
					var countTorus = groom.volumeData.cbuffer._BoundaryTorusCount;
					var countPack = countCapsule + countSphere + countTorus;
					var countTxt = countPack + " active shapes (" + countCapsule + " capsule, " + countSphere + " sphere, " + countTorus + " torus)";

					var rectHeight = GUI.skin.box.CalcHeight(new GUIContent(string.Empty), 0.0f);
					var rect = GUILayoutUtility.GetRect(0.0f, rectHeight, GUILayout.ExpandWidth(true));

					var color = GUI.color;
					GUI.color = Color.white;
					GUI.Box(EditorGUI.IndentedRect(rect), countTxt);
					GUI.color = color;

					var discarded = groom.volumeData.boundaryPrevCountDiscarded;
					if (discarded > -1)
					{
						rect = GUILayoutUtility.GetRect(0.0f, rectHeight, GUILayout.ExpandWidth(true));

						GUI.color = Color.red;
						GUI.Box(EditorGUI.IndentedRect(rect), discarded + " discarded (due to limit of " + HairSim.MAX_BOUNDARIES + ")");
						GUI.color = color;
					}
				}

				EditorGUILayout.Space();
				StructPropertyFieldsWithHeader(_settingsDebug, "Settings Debug");
				using (new EditorGUI.IndentLevelScope())
				{
					var divider = _settingsDebug.FindPropertyRelative("drawSliceDivider").floatValue;
					var dividerBase = Mathf.Floor(divider);
					var dividerFrac = divider - Mathf.Floor(divider);

					var rect = GUILayoutUtility.GetRect(0.0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));

					rect = EditorGUI.IndentedRect(rect);

					var rect0 = new Rect(rect);
					var rect1 = new Rect(rect);

					rect0.width = (rect.width) * (1.0f - dividerFrac);
					rect1.width = (rect.width) * dividerFrac;
					rect1.x += rect0.width;

					string DividerLabel(int index)
					{
						switch (index)
						{
							case 0: return "density";
							case 1: return "velocity";
							case 2: return "divergence";
							case 3: return "pressure";
							case 4: return "grad(pressure)";
						}
						return "unknown";
					}

					EditorGUILayout.BeginHorizontal();
					{
						GUI.Box(rect0, DividerLabel((int)dividerBase + 0), EditorStyles.helpBox);
						GUI.Box(rect1, DividerLabel((int)dividerBase + 1), EditorStyles.helpBox);
					}
					EditorGUILayout.EndHorizontal();
				}
			}

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
