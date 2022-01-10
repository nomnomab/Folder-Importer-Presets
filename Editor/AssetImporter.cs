﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Nomnom.FolderImporterPresets.Editor {
	internal class AssetImporter: AssetPostprocessor {
		private static HashSet<string> _ignorePaths = new HashSet<string>();
		private static HashSet<string> _metaMissing = new HashSet<string>();
		private static FolderImporter _folderImporter;

		private void OnPreprocessAsset() {
			if (assetImporter.importSettingsMissing) {
				_metaMissing.Add(assetPath);
			}
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
			string[] movedFromAssetPaths) {
			bool isDirty = false;
			
			FolderImporter[] importers = AssetDatabase.FindAssets($"t:{typeof(FolderImporter).FullName}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<FolderImporter>)
				.ToArray();

			if (importers == null || importers.Length == 0) {
				return;
			}

			FolderImporter importer = importers[0];

			foreach (string path in importedAssets) {
				if (_ignorePaths.Contains(path)) {
					continue;
				}

				// string absolutePath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{path}";
				// string absolutePathMeta = $"{absolutePath}.meta";
				// check meta file
				if (!_metaMissing.Contains(path)) {
					continue;
				}

				_metaMissing.Remove(path);

				Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				UnityEditor.AssetImporter assetImporter = UnityEditor.AssetImporter.GetAtPath(path);
				
				// only touch assets that are missing a .meta file
				// get a backwards path to root
				var backwardPath = CollectPathToRoot(path);
				foreach (string folder in backwardPath) {
					// get any folder importers
					IEnumerable<DefaultAsset> folders =
						string.IsNullOrEmpty(folder) 
							? AssetDatabase.FindAssets($"t:{typeof(DefaultAsset).FullName}")
								.Select(AssetDatabase.GUIDToAssetPath)
								.Where(path => string.IsNullOrEmpty(Path.GetDirectoryName(path)))
								.Select(AssetDatabase.LoadAssetAtPath<DefaultAsset>) 
							: AssetDatabase.FindAssets($"t:{typeof(DefaultAsset).FullName}", new [] {folder})
							.Select(AssetDatabase.GUIDToAssetPath)
							.Where(path => Path.GetDirectoryName(path) == folder)
							.Select(AssetDatabase.LoadAssetAtPath<DefaultAsset>);

					bool isDone = false;
					
					foreach (DefaultAsset defaultFolder in folders) {
						if (isDone) {
							break;
						}

						// get a valid preset
						foreach (PresetHolder holder in importer.Presets[defaultFolder]) {
							bool isValid = holder.IsFilterValid(obj, assetImporter, path, AssetDatabase.GetAssetPath(defaultFolder));

							if (!isValid) {
								continue;
							}
							
							// found a valid filter
							holder.Preset.ApplyTo(assetImporter);

							isDirty = true;

							isDone = true;
							break;
						}
					}

					if (isDone) {
						break;
					}
				}
			}

			if (!isDirty) {
				return;
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			
			EditorApplication.delayCall += () => {
				foreach (string importedAsset in importedAssets) {
					if (_ignorePaths.Contains(importedAsset)) {
						_ignorePaths.Remove(importedAsset);
						continue;
					}

					_ignorePaths.Add(importedAsset);

					string path = importedAsset;
					AssetDatabase.ImportAsset(path);
				}
				
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				
				_ignorePaths.Clear();
			};
		}

		public static bool PreCheckForAsset(Object obj) {
			string path = AssetDatabase.GetAssetPath(obj);
			UnityEditor.AssetImporter assetImporter = UnityEditor.AssetImporter.GetAtPath(path);
			
			FolderImporter[] importers = AssetDatabase.FindAssets($"t:{typeof(FolderImporter).FullName}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<FolderImporter>)
				.ToArray();

			if (importers == null || importers.Length == 0) {
				return false;
			}

			FolderImporter importer = importers[0];
				
			// only touch assets that are missing a .meta file
			// get a backwards path to root
			var backwardPath = CollectPathToRoot(path);
			foreach (string folder in backwardPath) {
				// get any folder importers
				// get any folder importers
				IEnumerable<DefaultAsset> folders =
					string.IsNullOrEmpty(folder) 
						? AssetDatabase.FindAssets($"t:{typeof(DefaultAsset).FullName}")
							.Select(AssetDatabase.GUIDToAssetPath)
							.Where(path => string.IsNullOrEmpty(Path.GetDirectoryName(path)))
							.Select(AssetDatabase.LoadAssetAtPath<DefaultAsset>) 
						: AssetDatabase.FindAssets($"t:{typeof(DefaultAsset).FullName}", new [] {folder})
							.Select(AssetDatabase.GUIDToAssetPath)
							.Where(path => Path.GetDirectoryName(path) == folder)
							.Select(AssetDatabase.LoadAssetAtPath<DefaultAsset>);
					
				foreach (DefaultAsset defaultFolder in folders) {
					// get a valid preset
					foreach (PresetHolder holder in importer.Presets[defaultFolder]) {
						bool isValid = holder.IsFilterValid(obj, assetImporter, path, AssetDatabase.GetAssetPath(defaultFolder));

						if (!isValid) {
							continue;
						}
							
						// found a valid filter
						return true;
					}
				}
			}

			return false;
		}

		private static List<string> CollectPathToRoot(string path) {
			string[] split = path.Split('/');
			List<string> directories = new List<string>();

			string currentPath = string.Empty;
			for (int i = 0; i < split.Length - 1; i++) {
				directories.Add(currentPath);

				currentPath += $"{(i != 0 ? "\\" : string.Empty)}{split[i]}";
			}
			
			directories.Add(currentPath);
			directories.Reverse();

			return directories;
		}
	}
}