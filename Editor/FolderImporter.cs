using System.Collections.Generic;
using UnityEditor.Presets;
using UnityEngine;

namespace Nomnom.FolderImporterPresets.Editor {
	[CreateAssetMenu(menuName = "Nomnom/Folder Importer", fileName = "Folder Importer", order = int.MaxValue - 1)]
	internal class FolderImporter: ScriptableObject {
		public List<PresetHolder> Presets = new List<PresetHolder>();

		public bool Contains(Object[] objectReferences) {
			foreach (Object objectReference in objectReferences) {
				if (!(objectReference is Preset preset)) {
					continue;
				}

				foreach (PresetHolder presetHolder in Presets) {
					if (presetHolder.Preset == preset) {
						return true;
					}
				}
			}

			return false;
		}
	}
}