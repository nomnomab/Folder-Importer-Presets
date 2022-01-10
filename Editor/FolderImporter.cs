using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace Nomnom.FolderImporterPresets.Editor {
	[CreateAssetMenu(menuName = "Nomnom/Folder Importer", fileName = "Folder Importer", order = int.MaxValue - 1)]
	internal class FolderImporter: ScriptableObject {
		// public List<PresetHolder> Presets = new List<PresetHolder>();
		public PresetDictionary Presets = new PresetDictionary();

		public bool Contains(DefaultAsset folder, Object[] objectReferences) {
			foreach (Object objectReference in objectReferences) {
				if (!(objectReference is Preset preset)) {
					continue;
				}

				foreach (PresetHolder presetHolder in Presets[folder]) {
					if (presetHolder.Preset == preset) {
						return true;
					}
				}
			}

			return false;
		}
	}
}