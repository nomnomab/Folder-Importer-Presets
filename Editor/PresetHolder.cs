using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nomnom.FolderImporterPresets.Editor {
	[System.Serializable]
	internal class PresetHolder {
		public Preset Preset;
		public bool Enabled;
		public string Filter;

		public PresetHolder(Preset preset) {
			Preset = preset;
			Enabled = true;
			Filter = string.Empty;
		}

		public bool IsFilterValid(Object obj, UnityEditor.AssetImporter assetImporter, string path) {
			bool canBeAppliedTo = Preset.CanBeAppliedTo(assetImporter);

			if (!canBeAppliedTo || !Enabled) {
				return false;
			}
			
			// no filter, use 100%
			if (string.IsNullOrEmpty(Filter)) {
				return true;
			}
			
			FileInfo fileInfo = new FileInfo(path);
			DirectoryInfo directoryInfo = fileInfo.Directory;
			string[] filters = Filter.Split('|');

			// n: file name
			// e: extension
			// t: object type
			// l: asset label

			foreach (string filter in filters) {
				// handle filter with above criteria
				if (string.IsNullOrEmpty(filter)) {
					continue;
				}

				string inputFilter = getInitFilter(filter);
				FileInfo[] files;

				switch (filter[0]) {
					case 'n': // file name
					case 'e': // extension
						files = directoryInfo.GetFiles(inputFilter);
						foreach (FileInfo file in files) {
							if (file.FullName.Equals(fileInfo.FullName, StringComparison.InvariantCultureIgnoreCase)) {
								return true;
							}
						}
						break;
					case 't': // object type
						if (obj.GetType().FullName.EndsWith(inputFilter, StringComparison.InvariantCultureIgnoreCase)) {
							return true;
						}
						break;
					case 'l': // asset label
						string[] storedLabels = AssetDatabase.GetLabels(obj);
						string[] inputLabels = inputFilter.Split(',');

						if (inputLabels.Length == 0 && !string.IsNullOrEmpty(inputFilter)) {
							inputLabels = new [] {inputFilter};
						}

						foreach (string label in storedLabels) {
							foreach (string inputLabel in inputLabels) {
								if (label.Equals(inputLabel, StringComparison.InvariantCultureIgnoreCase)) {
									return true;
								}
							}
						}
						break;
				}
			}

			string getInitFilter(string filter) {
				return filter.Substring(2);
			}

			return false;
		}
	}
}