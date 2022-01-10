using System.Collections.Generic;
using UnityEditor;

namespace Nomnom.FolderImporterPresets.Editor {
	[System.Serializable]
	internal class PresetDictionary: SerializableDictionary<DefaultAsset, List<PresetHolder>> { }
}