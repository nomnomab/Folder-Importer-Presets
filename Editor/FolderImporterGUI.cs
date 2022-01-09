using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.FolderImporterPresets.Editor {
	[CustomEditor(typeof(FolderImporter))]
	internal class FolderImporterGUI: UnityEditor.Editor {
		private List<Group> _groups = new List<Group>();
		private Object _currentSelection;
		private bool _showGuide;

		private void OnEnable() {
			UpdateGroups();
		}

		protected override bool ShouldHideOpenButton() {
			return true;
		}

		public override void OnInspectorGUI() {
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical();
			GUILayout.Space(13);
			if (GUILayout.Button("Add Preset")) {
				EditorGUIUtility.ShowObjectPicker<Preset>(null, false, "t:Preset", 1);
			}
			EditorGUILayout.EndVertical();

			GUI.backgroundColor = Color.clear;
			GUILayout.Box(new GUIContent(EditorGUIUtility.IconContent("d_Dropdown Icon").image, "Drag-n-drop presets here to add them!"), "Box", GUILayout.Height(40));
			GUI.backgroundColor = Color.white;

			Rect rect = GUILayoutUtility.GetLastRect();
			Event e = Event.current;
			rect.x = 0;
			rect.width = Screen.width;

			switch (e.type) {
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (rect.Contains(e.mousePosition) && !GetTarget().Contains(DragAndDrop.objectReferences)) {
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			
						if (e.type == EventType.DragPerform) {
							DragAndDrop.AcceptDrag();
			
							foreach (Object obj in DragAndDrop.objectReferences) {
								if (!(obj is Preset preset)) {
									continue;
								}
			
								AddPreset(preset);
							}
						}
					}
			
					break;
			}
			
			EditorGUILayout.EndHorizontal();

			GUI.enabled = GetTarget().Presets.Count > 0;
			if (GUILayout.Button(new GUIContent("Force Apply Filters", "This will force a reimport of this folder and all subdirectories.\n\nUse at your own risk!"))) {
				// collect all assets in folders
				var assets = AssetDatabase.FindAssets(string.Empty, new []{ Path.GetDirectoryName(AssetDatabase.GetAssetPath(GetTarget())) })
					.Select(AssetDatabase.GUIDToAssetPath)
					.Select(AssetDatabase.LoadAssetAtPath<Object>)
					.Where(obj => !(obj is FolderImporter));

				foreach (Object asset in assets) {
					if (!AssetImporter.PreCheckForAsset(asset)) {
						continue;
					}
					
					// nuke the meta file
					string absolutePath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{AssetDatabase.GetAssetPath(asset)}";

					if (!Path.HasExtension(absolutePath)) {
						continue;
					}
					
					string absolutePathMeta = $"{absolutePath}.meta";

					if (File.Exists(absolutePathMeta)) {
						File.Delete(absolutePathMeta);
					}
					
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset), ImportAssetOptions.ForceUpdate);
				}
			}

			GUI.enabled = true;
			
			GUILayout.Space(8);

			HandleObjectPicker();
			RenderPresets();
		}

		private void HandleObjectPicker() {
			Event e = Event.current;
			
			if (EditorGUIUtility.GetObjectPickerControlID() == 1) {
				switch (e.commandName) {
					case "ObjectSelectorUpdated":
						_currentSelection = EditorGUIUtility.GetObjectPickerObject();
						break;
					case "ObjectSelectorClosed":
						if (_currentSelection is Preset preset) {
							AddPreset(preset);
						}
						
						_currentSelection = null;
						break;
				}
			}
		}

		private void AddPreset(Preset preset) {
			FolderImporter obj = GetTarget();
			
			foreach (PresetHolder presetHolder in obj.Presets) {
				if (presetHolder.Preset == preset) {
					return;
				}
			}
							
			// add preset to list
			obj.Presets.Add(new PresetHolder(preset));
							
			UpdateGroups();
			MarkDirty();
		}

		private void RenderPresets() {
			for (int i = 0; i < _groups.Count; i++) {
				Group group = _groups[i];
				// header
				GUI.backgroundColor = Color.black * 0.75f;
				EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
				GUI.backgroundColor = Color.clear;
				GUILayout.Box(group.ImporterIcon, GUILayout.Width(24), GUILayout.Height(24));
				GUI.backgroundColor = Color.white;

				EditorGUILayout.BeginVertical();
				GUILayout.Space(3);
				EditorGUILayout.LabelField(group.ImporterName);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				// info header
				Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight));
				EditorGUILayout.BeginVertical();
				GUILayout.Space(EditorGUIUtility.singleLineHeight);
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				Rect line1Rect = rect;
				line1Rect.x += 38;
				line1Rect.width = 8;
				line1Rect.height -= 2;
				EditorGUI.LabelField(line1Rect, GUIContent.none, "EyeDropperVerticalLine");

				Rect label1Rect = line1Rect;
				label1Rect.x += 8;
				label1Rect.width = 40;
				EditorGUI.LabelField(label1Rect, "Filter", EditorStyles.miniBoldLabel);

				GUI.backgroundColor = Color.clear;
				Rect helpIcon = label1Rect;
				helpIcon.x += 26;
				helpIcon.y -= 1;
				helpIcon.width = 26;
				helpIcon.height = 26;
				GUI.Box(helpIcon, new GUIContent(EditorGUIUtility.IconContent("d__Help").image, "Usage guide" +
					"\n\n" +
					"n: file names\n" +
					"- e.g. n:UI_*" +
					"\n" +
					"e: file extensions\n" +
					"- e.g. e:*.png" +
					"\n" +
					"t: object type\n" +
					"- e.g. t:UnityEngine.Texture2D" +
					"\n" +
					"l: asset labels\n" +
					"- e.g. l:2d,3d,car" +
					"\n\n" +
					"These filters can be chained with | \nThe first filter to be true will validate the entire input\n" +
					"- e.g. n:UI_*|e:*.png|t:UnityEngine.Texture2D"));
				GUI.backgroundColor = Color.white;

				Rect line2Rect = label1Rect;
				line2Rect.x = rect.width * 0.5f + 34;
				line2Rect.width = rect.width - line1Rect.width - label1Rect.width - 38;
				EditorGUI.LabelField(line2Rect, GUIContent.none, "EyeDropperVerticalLine");

				Rect label2Rect = line2Rect;
				label2Rect.x += 8;
				label2Rect.width = 40;
				EditorGUI.LabelField(label2Rect, "Preset", EditorStyles.miniBoldLabel);

				// presets
				group.ReorderableList.DoLayoutList();
			}
		}

		private void UpdateGroups() {
			_groups.Clear();
			
			IEnumerable<IGrouping<PresetType, PresetHolder>> groups = GetTarget().Presets
				.GroupBy(preset => preset.Preset.GetPresetType());

			foreach (var grouping in groups) {
				_groups.Add(new Group(grouping.Key, grouping.ToList(), CollectPresets));
			}
		}

		private void CollectPresets() {
			IEnumerable<PresetHolder> presets = _groups.SelectMany(group => group.Presets);
			GetTarget().Presets = presets.ToList();
			UpdateGroups();
			
			MarkDirty();
		}

		private void MarkDirty() {
			EditorUtility.SetDirty(GetTarget());
		}
		
		private FolderImporter GetTarget() => (FolderImporter) target;

		private class Group {
			public PresetType Importer;
			public Texture2D ImporterIcon;
			public string ImporterName;
			public Type ImporterType;
			public List<PresetHolder> Presets;
			public ReorderableList ReorderableList;

			public Group(PresetType importer, List<PresetHolder> presets, Action onChanged) {
				Importer = importer;
				Presets = presets;

				ImporterIcon = (Texture2D)importer.GetType().GetMethod("GetIcon", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(importer, null);
				ImporterName = Importer.GetManagedTypeName();
				ImporterName = ImporterName.Substring(ImporterName.LastIndexOf('.') + 1);
				ImporterType = typeof(TextureImporter).Assembly.GetType(Importer.GetManagedTypeName());
				
				ReorderableList = new ReorderableList(presets, typeof(PresetHolder), true, false, false, true);
				ReorderableList.drawElementCallback = (rect, index, isActive, isFocused) => {
					rect.y += 2;
					rect.height = EditorGUIUtility.singleLineHeight;

					Rect toggleRect = rect;
					toggleRect.width = 18;

					Rect filterRect = rect;
					filterRect.x += toggleRect.width + 2;
					filterRect.width -= toggleRect.width + 4;
					filterRect.width /= 2;

					Rect presetRect = filterRect;
					presetRect.x += filterRect.width + 2;

					PresetHolder holder = (PresetHolder) ReorderableList.list[index];
					EditorGUI.BeginChangeCheck();
					holder.Enabled = EditorGUI.Toggle(toggleRect, holder.Enabled);
					holder.Filter = EditorGUI.TextField(filterRect, holder.Filter, EditorStyles.toolbarSearchField);
					holder.Preset = (Preset)EditorGUI.ObjectField(presetRect, holder.Preset, typeof(Preset));

					if (EditorGUI.EndChangeCheck()) {
						onChanged();
					}
				};
				ReorderableList.onChangedCallback = _ => onChanged();
			}
		}
	}
}