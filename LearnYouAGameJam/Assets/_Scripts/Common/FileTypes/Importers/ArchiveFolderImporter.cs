using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace LYGJ.Common.FileTypes.Importers {
    [ScriptedImporter(1, ".zip")]
    public sealed class ArchiveFolderImporter : ScriptedImporter {

        #region Overrides of ScriptedImporter

        /// <inheritdoc />
        public override void OnImportAsset( AssetImportContext Ctx ) {
            // Use ArchiveFolder.Create(FileInfo) to create the asset
            // Then find all 'Folder' references (recursively) and add them as subobjects

            FileInfo      File   = new(Ctx.assetPath);
            ArchiveFolder Folder = ArchiveFolder.Create(File);
            Ctx.AddObjectToAsset("MainAsset", Folder);

            // Add subobjects
            void AddSubobjects( ArchiveFolder Folder ) {
                foreach (ArchiveFolder Subfolder in Folder.Folders) {
                    Ctx.AddObjectToAsset(Subfolder.Name, Subfolder);
                    AddSubobjects(Subfolder);
                }
            }
            AddSubobjects(Folder);

            Ctx.SetMainObject(Folder);
        }

        #endregion

    }

    [CustomEditor(typeof(ArchiveFolderImporter))]
    public sealed class ArchiveFolderImporterEditor : ScriptedImporterEditor {
        static readonly GUIContent
            _ShowInExplorer = new("Show in Explorer", "Opens the folder containing this asset in Windows Explorer."),
            _Extract        = new("Extract to...", "Extracts the contents of this archive to a folder.");

        /// <inheritdoc />
        public override void OnInspectorGUI() {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            // EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button(_ShowInExplorer, EditorStyles.miniButton)) {
                string? ArchivePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath((ArchiveFolder)assetTarget));
                if (ArchivePath != null) {
                    System.Diagnostics.Process.Start(ArchivePath);
                }
            }

            if (GUILayout.Button(_Extract, EditorStyles.miniButton)) {
                ArchiveFolder Target = (ArchiveFolder)assetTarget;

                // Get the archive file
                string   ArchivePath = AssetDatabase.GetAssetPath(Target);
                FileInfo Archive     = new(ArchivePath);

                // Open a folder browser
                string? Destination = EditorUtility.OpenFolderPanel("Extract to...", Archive.DirectoryName, string.Empty);

                // If the user selected a folder, extract the archive to it
                if (!string.IsNullOrEmpty(Destination)) {
                    // Edge-case: If the directory is the same as the archive, create a subfolder
                    //            Don't forget that 'Destination' is a relative path, whilst 'Archive.DirectoryName' is an absolute path
                    if (string.Equals(Path.GetFullPath(Destination), Archive.DirectoryName, StringComparison.OrdinalIgnoreCase)) {
                        Destination = Path.Combine(Destination, Path.GetFileNameWithoutExtension(Archive.Name));
                    }

                    DirectoryInfo DestinationDir = new(Destination);
                    if (!DestinationDir.Exists) {
                        DestinationDir.Create();
                    } else {
                        // If folder isn't empty, ask user to confirm if they wish to continue.
                        if (DestinationDir.EnumerateFileSystemInfos().Any()) {
                            if (!EditorUtility.DisplayDialog("Extract to...", "The selected folder is not empty. Are you sure you wish to continue?", "Yes", "No")) {
                                // EditorGUILayout.EndVertical();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.EndVertical();
                                return;
                            }
                        }
                    }

                    // Extract the archive
                    using ZipArchive ArchiveFile = ZipFile.OpenRead(Archive.FullName);
                    Target.Extract(ArchiveFile, DestinationDir, true, false);

                    // Refresh the asset database
                    AssetDatabase.Refresh();

                    // Select the first file/folder in the destination folder, if any
                    string[] Files = AssetDatabase.FindAssets(string.Empty, new[] { Destination });
                    try {
                        if (Files.Length > 0) {
                            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(Files[0]));
                        }
                    } catch { } // Just ignore exceptions here; it's a QoL feature, not a necessity.
                }
            }
            // EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        #region Overrides of AssetImporterEditor

        /// <inheritdoc />
        protected override bool needsApplyRevert => false;

        #endregion

    }
}
