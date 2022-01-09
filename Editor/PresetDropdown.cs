using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Nomnom.FolderImporterPresets.Editor {
	internal class PresetDropdown: AdvancedDropdown {
		private static readonly Dictionary<string, Icon> _textures =
			new Dictionary<string, Icon> {
				{ "TextureImporter", GetIcon("IHVImageFormatImporter Icon", "d_IHVImageFormatImporter Icon") },
				{ "AudioImporter", GetIcon("AudioImporter Icon", "d_AudioImporter Icon") },
			};

		private static Icon GetIcon(string light, string dark) {
			return new Icon(GetIcon(light), GetIcon(dark));
		}

		private static Texture2D GetIcon(string name) {
			return (Texture2D) EditorGUIUtility.IconContent(name).image;
		}

		public PresetDropdown(AdvancedDropdownState state) : base(state) { }
		
		protected override AdvancedDropdownItem BuildRoot() {
			var root = new AdvancedDropdownItem("Add Preset Type");

			addChild("Texture Importer");
			addChild("Audio Importer");

			return root;

			void addChild(string name) {
				root.AddChild(new AdvancedDropdownItem(name) {
					icon = _textures[name.Replace(" ", string.Empty)].Get()
				});
			}
		}
		
		internal class Icon {
			public Texture2D Light;
			public Texture2D Dark;

			public Icon(Texture2D light, Texture2D dark) {
				Light = light;
				Dark = dark;
			}

			public Texture2D Get() => EditorGUIUtility.isProSkin ? Dark : Light;
		}
	}
}