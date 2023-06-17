using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common.FileTypes {
    public sealed class ArchiveFolder : ScriptableObject {
        // Only contains filenames of local files, and pointers to other local folders
        // Built like a DLL; has a pointer to its parent too.

        [Tooltip("The name of this folder")]
        public string Name = string.Empty;

        [Tooltip("The local files in this folder")]
        public string[] Files = Array.Empty<string>();

        [Tooltip("The local folders in this folder")]
        public ArchiveFolder[] Folders = Array.Empty<ArchiveFolder>();

        [Tooltip("The parent folder of this folder")]
        public ArchiveFolder? Parent = null;

        /// <summary> The full path of this folder. </summary>
        public string Path => Parent == null ? Name : $"{Parent.Path}/{Name}";

        public delegate bool ExtractPredicate( string Path, bool IsFolder );

        static bool DefaultExtractPredicate( string Path, bool IsFolder ) => true;

        /// <summary> Extracts this folder and all its contents to the given path. </summary>
        /// <param name="Archive"> The archive to extract from. </param>
        /// <param name="Destination"> The path to extract to. </param>
        /// <param name="Overwrite"> Whether to overwrite existing files. </param>
        /// <param name="NewFolder"> Whether to create a new folder for this folder, as a subfolder of the destination (<see langword="true"/>), or to extract directly to the destination (<see langword="false"/>). </param>
        /// <param name="Predicate"> A predicate to filter which files/folders to extract. If <see langword="null"/>, all files will be extracted. </param>
        /// <returns> The path to the extracted folder. </returns>
        public DirectoryInfo Extract( ZipArchive Archive, DirectoryInfo Destination, bool Overwrite = false, bool NewFolder = true, ExtractPredicate? Predicate = null ) {
            Predicate ??= DefaultExtractPredicate;
            bool   HasParent  = Parent != null;
            string ParentPath = HasParent ? Parent!.Path : string.Empty;

            // 1. Create the destination folder (+ subfolder, if specified)
            Destination = NewFolder
                ? Destination.CreateSubdirectory(System.IO.Path.GetFileNameWithoutExtension(Name))
                : Destination;

            // 2. Extract all files in this folder
            foreach (string File in Files) {
                // 2.1. Confirm with predicate
                string FilePath = HasParent ? $"{ParentPath}/{File}" : File;
                if (!Predicate(FilePath, false)) { continue; }

                // 2.2. Find the file in the archive
                ZipArchiveEntry? Entry = Archive.GetEntry(File);
                if (Entry == null) {
                    Debug.LogError($"Could not find file {File} in folder {ParentPath} in archive.");
                    continue;
                }

                // 2.3. Extract the file
                string EntryDest = System.IO.Path.Combine(Destination.FullName, File);
                Entry.ExtractToFile(EntryDest, Overwrite);
                // Debug.Log($"Extracted {Entry.FullName} to {EntryDest}");
            }

            // 3. Extract all subfolders in this folder
            foreach (ArchiveFolder Subfolder in Folders) {
                // 3.1. Confirm with predicate
                string SubfolderPath = HasParent ? $"{ParentPath}/{Subfolder.Name}" : Subfolder.Name;
                if (!Predicate(SubfolderPath, true)) { continue; }

                // 3.2. Create the subfolder
                DirectoryInfo SubfolderDestination = Destination.CreateSubdirectory(Subfolder.Name);

                // 3.3. Extract the subfolder
                Subfolder.Extract(Archive, SubfolderDestination, Overwrite, false);
                // Debug.Log($"Extracted folder {Subfolder.Name} to {SubfolderDestination.FullName}");
            }

            return Destination;
        }

        /// <summary> Creates an <see cref="ArchiveFolder"/> from the given archive file. </summary>
        /// <param name="Archive"> The archive to create the folder from. </param>
        /// <param name="Name"> The name of the folder. </param>
        /// <returns> The created folder. </returns>
        public static ArchiveFolder Create( ZipArchive Archive, string Name ) {
            // 1. Create the folder
            ArchiveFolder Folder = CreateInstance<ArchiveFolder>();
            Folder.Name = System.IO.Path.GetFileNameWithoutExtension(Name);

            // 2. Find all files and folders in the archive
            foreach (ZipArchiveEntry Entry in Archive.Entries) {
                // Debug.Log($"Found entry {Entry.FullName} in archive.");
                // 2.1. Split the entry path into parts
                string[] Parts   = Entry.FullName.Split('/');
                int      PartsLn = Parts.Length;

                switch (PartsLn) {
                    // 2.2. Check if the entry is in this folder
                    case > 1 when Parts[0] != Name:
                        Debug.LogWarning($"Entry {Entry.FullName} is not in folder {Name}.");
                        continue;
                    // 2.3. Check if the entry is a file or folder
                    case 1:
                        // 2.3.1. Add the file to the folder
                        Array.Resize(ref Folder.Files, Folder.Files.Length + 1);
                        Folder.Files[^1] = Parts[0];
                        // Debug.Log($"Added file {Parts[0]} to folder {Name}.");
                        break;
                    default:
                        // 2.3.2. Add the folder to the folder
                        Array.Resize(ref Folder.Folders, Folder.Folders.Length + 1);
                        Folder.Folders[^1]        = Create(Archive, Parts[1]);
                        Folder.Folders[^1].Parent = Folder;
                        // Debug.Log($"Added folder {Parts[1]} to folder {Name}.");
                        break;
                }
            }

            return Folder;
        }

        /// <inheritdoc cref="Create(ZipArchive, string)"/>
        public static ArchiveFolder Create( FileInfo Archive ) {
            using ZipArchive ArchiveFile = ZipFile.OpenRead(Archive.FullName);
            return Create(ArchiveFile, Archive.Name);
        }
    }
}
