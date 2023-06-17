using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LYGJ.Common.FileTypes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.AudioManagement {
    [CreateAssetMenu(menuName = "The Deliverer/Audio/Music", fileName = "New Music", order = 1001)]
    public sealed class Music : ScriptableObject {

        /// <summary> Gets or sets the melody stem. </summary>
        [field: SerializeField, Title("Stems"), Tooltip("The melody stem.")] public AudioClip? Melody { get; set; } = null;

        /// <summary> Gets or sets the instruments stem. </summary>
        [field: SerializeField, Tooltip("The instruments stem.")] public AudioClip? Instruments { get; set; } = null;

        /// <summary> Gets or sets the bass stem. </summary>
        [field: SerializeField, Tooltip("The bass stem.")] public AudioClip? Bass { get; set; } = null;

        /// <summary> Gets or sets the drums stem. </summary>
        [field: SerializeField, Tooltip("The drums stem.")] public AudioClip? Drums { get; set; } = null;

        #if UNITY_EDITOR
        // Context menu for .zip files to extract the stems.
        //
        // For example:
        // "ES_Struggle in the Jungle - Kikoru.zip", which contains:
        // - "ES_Struggle in the Jungle STEMS BASS.wav"
        // - "ES_Struggle in the Jungle STEMS DRUMS.wav"
        // - "ES_Struggle in the Jungle STEMS INSTRUMENTS.wav"
        // - "ES_Struggle in the Jungle STEMS MELODY.wav"
        //
        // 1. Extract the .zip file to a folder. Schema '{Artist} - {Song Name}/' (referred to henceforth as 'Root Folder')
        // 2. Delete the .zip file.
        // 3. Rename the .wav files to the following schema: 'Root Folder/{Artist} - {Song Name}_{Stem Name}.wav'
        // 4. Create a new Music scriptable object, assigning the stems. File name schema: 'Root Folder/{Artist} - {Song Name}.asset'
        //    Use ProjectWindowUtil to allow the user to rename the file upon creation.

        [MenuItem("CONTEXT/ArchiveFolder/Convert to Music")]
        static void ExtractStems() {
            // 1. Check if the asset is a .zip file.
            ArchiveFolder? Asset     = Selection.activeObject as ArchiveFolder;
            string         AssetPath = AssetDatabase.GetAssetPath(Asset);

            if (Asset == null) {
                Debug.LogError("No asset selected.");
                return;
            }

            // 1.1. Get song name and artist.
            string SongName = System.IO.Path.GetFileNameWithoutExtension(AssetPath), Artist = string.Empty;
            if (SongName.Contains(" - ")) {
                string[] Split = SongName.Split(" - ");
                Artist   = Split[1];
                SongName = Split[0];
            }
            if (SongName.StartsWith("ES_")) {
                SongName = SongName[3..];
            }

            // 2. Extract files.
            DirectoryInfo Destination;
            using (ZipArchive Archive = ZipFile.OpenRead(AssetPath)) {
                // 2.1. Perform extraction.
                DirectoryInfo RootFolder = new(System.IO.Path.GetDirectoryName(AssetPath)!);
                Debug.Log($"Extracting {AssetPath} to {RootFolder.FullName}");
                bool EntryPredicate( string Path, bool IsFolder ) {
                    if (IsFolder) { return false; }

                    string FileName = System.IO.Path.GetFileNameWithoutExtension(Path);
                    return FileName.EndsWith("STEMS MELODY") || FileName.EndsWith("STEMS INSTRUMENTS") || FileName.EndsWith("STEMS BASS") || FileName.EndsWith("STEMS DRUMS");
                }
                Destination = Asset.Extract(Archive, RootFolder, true, true, EntryPredicate);
                // Debug.Log($"Extracted {AssetPath} to {Destination.FullName}");

                // 2.2. Rename the folder to the schema.
                string NewPath = System.IO.Path.Combine(RootFolder.FullName, $"{Artist} - {SongName}");
                //      Perform the move. If the folder already exists, delete it first.
                if (Directory.Exists(NewPath)) {
                    Directory.Delete(NewPath, true);
                }
                Directory.Move(Destination.FullName, NewPath);
                Destination = new(NewPath);
            }
            // 2.3. Delete the .zip file.
            File.Delete(AssetPath);

            // 3. Rename the .wav files to the following schema: 'Root Folder/{Artist} - {Song Name}_{Stem Name}.wav'
            string[] Files  = Directory.GetFiles(Destination.FullName, "*.wav", SearchOption.AllDirectories);
            string?  Melody = null, Instruments = null, Bass = null, Drums = null;
            foreach (string File in Files) {
                string FileName = System.IO.Path.GetFileNameWithoutExtension(File);
                if (FileName.EndsWith("STEMS MELODY")) {
                    Melody = File;
                } else if (FileName.EndsWith("STEMS INSTRUMENTS")) {
                    Instruments = File;
                } else if (FileName.EndsWith("STEMS BASS")) {
                    Bass = File;
                } else if (FileName.EndsWith("STEMS DRUMS")) {
                    Drums = File;
                }
            }

            if (Melody != null) {
                File.Move(Melody, System.IO.Path.Combine(Destination.FullName, $"{Artist} - {SongName}_Melody.wav"));
            }
            if (Instruments != null) {
                File.Move(Instruments, System.IO.Path.Combine(Destination.FullName, $"{Artist} - {SongName}_Instruments.wav"));
            }
            if (Bass != null) {
                File.Move(Bass, System.IO.Path.Combine(Destination.FullName, $"{Artist} - {SongName}_Bass.wav"));
            }
            if (Drums != null) {
                File.Move(Drums, System.IO.Path.Combine(Destination.FullName, $"{Artist} - {SongName}_Drums.wav"));
            }

            // 4. Create a new Music scriptable object, assigning the stems. File name schema: 'Root Folder/{Artist} - {Song Name}.asset'
            //    Use ProjectWindowUtil to allow the user to rename the file upon creation.

            // 4.1. Refresh the asset database to find the audio clips.
            AssetDatabase.Refresh();

            // 4.2. Create the scriptable object.
            Music Music = CreateInstance<Music>();

            // 4.3. Assign the stems.
            AudioClip? MelodyClip = Melody == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(System.IO.Path.Combine(Destination.FullName, Melody));
            if (MelodyClip != null) { Music.Melody = MelodyClip; }

            AudioClip? InstrumentsClip = Instruments == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(System.IO.Path.Combine(Destination.FullName, Instruments));
            if (InstrumentsClip != null) { Music.Instruments = InstrumentsClip; }

            AudioClip? BassClip = Bass == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(System.IO.Path.Combine(Destination.FullName, Bass));
            if (BassClip != null) { Music.Bass = BassClip; }

            AudioClip? DrumsClip = Drums == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(System.IO.Path.Combine(Destination.FullName, Drums));
            if (DrumsClip != null) { Music.Drums = DrumsClip; }

            // 4.4. Save the scriptable object.
            string Path = System.IO.Path.Combine(Destination.FullName, $"{Artist} - {SongName}.asset");
            Path = Path[(Application.dataPath.Length - 6)..];
            Path = AssetDatabase.GenerateUniqueAssetPath(Path);
            Debug.Log($"Saving {Path}");
            ProjectWindowUtil.CreateAsset(Music, Path);
        }

        [MenuItem("CONTEXT/Music/Populate Stems")] // Just find AudioClips containing 'Melody', 'Instruments', 'Bass' and 'Drums' in the same folder as the Music asset.
        static void PopulateStems() {
            Music? Music = Selection.activeObject as Music;
            if (Music == null) {
                Debug.LogError("No asset selected.");
                return;
            }

            string Path  = AssetDatabase.GetAssetPath(Music);
            int    Index = Path.LastIndexOf('/');
            Path = Path[..Index];

            Undo.RecordObject(Music, "Populate Stems");
            foreach (string Relative in AssetDatabase.FindAssets("t:AudioClip", new[] { Path })) {
                string    Absolute = AssetDatabase.GUIDToAssetPath(Relative);
                AudioClip Clip     = AssetDatabase.LoadAssetAtPath<AudioClip>(Absolute)!;
                if (Clip.name.Contains("melody", StringComparison.OrdinalIgnoreCase)) {
                    Music.Melody = Clip;
                } else if (Clip.name.Contains("instruments", StringComparison.OrdinalIgnoreCase)) {
                    Music.Instruments = Clip;
                } else if (Clip.name.Contains("bass", StringComparison.OrdinalIgnoreCase)) {
                    Music.Bass = Clip;
                } else if (Clip.name.Contains("drums", StringComparison.OrdinalIgnoreCase)) {
                    Music.Drums = Clip;
                }
            }
        }

        #endif

        /// <summary> Determines the summated samples of multiple <see cref="AudioClip"/>s that play simultaneously. </summary>
        /// <param name="Clips"> The <see cref="AudioClip"/>s to play simultaneously. </param>
        /// <param name="Length"> The length of the summated samples. </param>
        /// <param name="Channels"> The number of channels of the summated samples. </param>param>
        /// <returns> The summated samples of the <see cref="AudioClip"/>s. </returns>
        public static float[] GetJoinedData( IEnumerable<AudioClip> Clips, int Length, int Channels ) {
            // Merge audio from each clip into the new clip
            float[] Data = new float[Length * Channels];
            foreach (AudioClip Clip in Clips) {
                // Get audio data from clip
                float[] ClipData = new float[Clip.samples * Clip.channels];
                Clip.GetData(ClipData, 0);

                // Mix audio data into merged clip
                for (int J = 0; J < ClipData.Length; J++) {
                    Data[J] += ClipData[J];
                }
            }

            return Data;
        }

        /// <summary> Determines the length, channels, and frequency of the summated samples of multiple <see cref="AudioClip"/>s that play simultaneously. </summary>
        /// <param name="Clips"> The <see cref="AudioClip"/>s to play simultaneously. </param>
        /// <param name="Length"> The length of the summated samples. </param>
        /// <param name="Channels"> The number of channels of the summated samples. </param>
        /// <param name="Frequency"> The frequency of the summated samples. </param>
        public static void GetJoinedDataInfo( IEnumerable<AudioClip> Clips, out int Length, out int Channels, out int Frequency ) {
            Length    = 0;
            Channels  = 0;
            Frequency = 0;

            // Calculate total length, number of channels, and frequency
            foreach (AudioClip Clip in Clips) {
                Length    = Mathf.Max(Length, Clip.samples);
                Channels  = Mathf.Max(Channels, Clip.channels);
                Frequency = Mathf.Max(Frequency, Clip.frequency);
            }
        }

        /// <summary> Creates an <see cref="AudioClip"/> which plays the given data. </summary>
        /// <param name="Data"> The summated samples of the <see cref="AudioClip"/>s. </param>
        /// <param name="Name"> The name of the new <see cref="AudioClip"/>. </param>
        /// <param name="Length"> The length of the summated samples. </param>
        /// <param name="Channels"> The number of channels of the summated samples. </param>
        /// <param name="Frequency"> The frequency of the summated samples. </param>
        /// <returns> The new <see cref="AudioClip"/>. </returns>
        public static AudioClip Join( float[] Data, string Name, int Length, int Channels, int Frequency ) {
            AudioClip Clip = AudioClip.Create(Name, Length, Channels, Frequency, false);
            Clip.SetData(Data, 0);
            return Clip;
        }

        /// <summary> Creates an <see cref="AudioClip"/> which plays the given <see cref="AudioClip"/>s simultaneously. </summary>
        /// <param name="Clips"> The <see cref="AudioClip"/>s to play simultaneously. </param>
        /// <returns> The new <see cref="AudioClip"/>. </returns>
        public static AudioClip? Join( params AudioClip[] Clips ) {
            switch (Clips.Length) {
                case 0: return null;
                case 1: return Clips[0];
            }

            GetJoinedDataInfo(Clips, out int Length, out int Channels, out int Frequency);
            float[] Data = GetJoinedData(Clips, Length, Channels);
            return Join(Data, string.Join(" + ", Clips.Select(Clip => Clip.name)), Length, Channels, Frequency);
        }

        class PreloadedAudioData {
            readonly float[] _Data;

            public int Position;

            public PreloadedAudioData( AudioClip Clip ) {
                _Data = new float[Clip.samples * Clip.channels];
                Clip.GetData(_Data, 0);
            }

            public int Sum( float[] Buffer ) {
                int BufferLength = Buffer.Length;
                int DataLength   = _Data.Length;
                if (Position + BufferLength > DataLength) {
                    BufferLength = DataLength - Position;
                }

                for (int I = 0; I < BufferLength; I++) {
                    Buffer[I] += _Data[Position + I];
                }

                Position += BufferLength;
                return BufferLength;
            }
        }

        /// <summary> Creates a new <see cref="AudioClip"/> which plays the given stems simultaneously, using a PCM reader callback for streaming instead of loading the entire audio data into memory. </summary>
        /// <param name="Clips"> The <see cref="AudioClip"/>s to play simultaneously. </param>
        /// <param name="Name"> The name of the new <see cref="AudioClip"/>. </param>
        /// <param name="Length"> The length of the summated samples. </param>
        /// <param name="Channels"> The number of channels of the summated samples. </param>
        /// <param name="Frequency"> The frequency of the summated samples. </param>
        /// <returns> The new <see cref="AudioClip"/>. </returns>
        public static AudioClip? JoinStreamed( IReadOnlyList<AudioClip> Clips, string Name, int Length, int Channels, int Frequency ) {
            switch (Clips.Count) {
                case 0: return null;
                case 1: return Clips[0];
            }

            PreloadedAudioData[] Data = new PreloadedAudioData[Clips.Count];
            for (int I = 0; I < Clips.Count; I++) {
                Data[I] = new(Clips[I]);
            }

            // Callback to read audio data from the merged clips
            // float[] => void PCMReaderCallback
            void Read( float[] Buffer ) {
                int BufferLength = Buffer.Length;
                for (int I = 0; I < BufferLength; I++) {
                    Buffer[I] = 0f;
                }
                foreach (PreloadedAudioData D in Data) { D.Sum(Buffer); }
            }
            // int => void PCMSetPositionCallback
            void SetPosition( int Position ) {
                foreach (PreloadedAudioData D in Data) { D.Position = Position; }
            }

            // Create a new audio clip to hold the merged audio and dynamically stream it
            return AudioClip.Create(Name, Length, Channels, Frequency, true, Read, SetPosition);
        }

        /// <summary> Creates a new <see cref="AudioClip"/> which plays the given stems simultaneously, using a PCM reader callback for streaming instead of loading the entire audio data into memory. </summary>
        /// <param name="Clips"> The <see cref="AudioClip"/>s to play simultaneously. </param>
        /// <returns> The new <see cref="AudioClip"/>. </returns>
        public static AudioClip? JoinStreamed( params AudioClip[] Clips ) {
            switch (Clips.Length) {
                case 0: return null;
                case 1: return Clips[0];
            }

            GetJoinedDataInfo(Clips, out int Length, out int Channels, out int Frequency);
            return JoinStreamed(Clips, $"{string.Join(" + ", Clips.Select(Clip => Clip.name))} (Streamed)", Length, Channels, Frequency);
        }

        /// <summary> Gets the <see cref="AudioClip"/>s for the given stems. </summary>
        /// <param name="Stems"> The stems to get the <see cref="AudioClip"/>s for. </param>
        /// <returns> The <see cref="AudioClip"/>s for the given stems. </returns>
        public IReadOnlyList<AudioClip> GetAudioClips( Stems Stems ) {
            List<AudioClip> Clips = new(4);
            if ((Stems & Stems.Melody)      != 0 && Melody != null)      { Clips.Add(Melody); }
            if ((Stems & Stems.Instruments) != 0 && Instruments != null) { Clips.Add(Instruments); }
            if ((Stems & Stems.Bass)        != 0 && Bass != null)        { Clips.Add(Bass); }
            if ((Stems & Stems.Drums)       != 0 && Drums != null)       { Clips.Add(Drums); }
            return Clips.ToArray();
        }

        /// <summary> Creates a new <see cref="AudioClip"/> which plays the given stems simultaneously. </summary>
        /// <param name="Stems"> The stems to play simultaneously. </param>
        /// <param name="Streamed"> Whether to use a PCM reader callback for streaming instead of loading the entire audio data into memory. </param>
        /// <returns> The new <see cref="AudioClip"/>. </returns>
        public AudioClip? CreateAudioClip( Stems Stems, bool Streamed = false ) =>
            Streamed
                ? JoinStreamed(GetAudioClips(Stems).ToArray())
                : Join(GetAudioClips(Stems).ToArray());

        /// <summary> Plays the music. </summary>
        /// <param name="Immediate"> Whether to play the music immediately, or first fade out the current music. <br/>
        /// Note, even if this is <see langword="false"/>, if no other music is playing, the music will be played immediately without fading. </param>
        public void Play( bool Immediate = false ) => Audio.PlayMusic(this, Immediate);

        /// <summary> Plays the music. </summary>
        /// <param name="Stems"> The stems to play simultaneously. </param>
        /// <param name="Immediate"> Whether to play the music immediately, or first fade out the current music. <br/>
        /// Note, even if this is <see langword="false"/>, if no other music is playing, the music will be played immediately without fading. </param>
        public void Play( Stems Stems, bool Immediate = false ) {
            Play(Immediate);
            this[Stems] = 1f;
        }

        /// <summary> Gets or sets the volume of the given stem(s). </summary>
        /// <param name="Stems"> The stem(s) to set the volume of. </param>
        /// <value> The volume of the given stem(s). </value>
        /// <returns> The volume of the given stem(s). </returns>
        public float this[ Stems Stems ] {
            get => Audio.Instance[Stems];
            set => Audio.Instance[Stems] = value;
        }

        /// <summary> Stops the music. </summary>
        /// <param name="Immediate"> Whether to stop the music immediately, or first fade out the music. </param>
        public void Stop( bool Immediate = false ) => Audio.StopMusic(Immediate);
    }

    // Music is split into stems: Melody, Instruments, Bass and Drums.
}
