using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LYGJ.Common;
using UnityEngine;

namespace LYGJ.SaveManagement {
    public static class Saves {
        /// <summary> The maximum number of saves. </summary>
        public const int Max = 3;

        /// <summary> The index of the first save. </summary>
        const int _FirstSave = 1;

        const string
            _SavePrefix    = "save-",
            _Ext           = ".json",
            _SaveFormat    = _SavePrefix + "{0}" + _Ext,
            _AppSavePrefix = "app-",
            _AppSaveFormat = _AppSavePrefix + _Ext;

        static readonly DirectoryInfo _SaveDirectory = new(Path.Combine(Application.persistentDataPath, "Saves"));

        static ResettableLazy<SaveDataProxy> CreateSaveProxy( string FileName ) =>
            new(
                () => {
                    FileInfo File = _SaveDirectory.CreateSubfile(FileName);
                    SaveData Data = SaveData.Create(File, ReadImmediate: true, CreateIfNotExists: true);

                    return new(Data);
                }
            );
        static ResettableLazy<SaveDataProxy> CreateSaveProxy( int Index ) => CreateSaveProxy(string.Format(_SaveFormat, Index));

        static readonly ResettableLazy<SaveDataProxy>[] _Saves = Enumerable.Range(_FirstSave, Max).Select(CreateSaveProxy).ToArray(Max);

        static readonly ResettableLazy<SaveDataProxy> _App = CreateSaveProxy(_AppSaveFormat);

        [ExecuteOnReload]
        static void Cleanup() {
            _App.Reset();
            foreach (ResettableLazy<SaveDataProxy> Save in _Saves) {
                Save.Reset();
            }
        }

        /// <summary> Gets the save data for application settings. </summary>
        /// <remarks> This is not a save file, but rather a file for application settings. <br/>
        /// Values such as the current language, volume, graphics, etc. are stored here. </remarks>
        public static SaveDataProxy App => _App.Value;

        const string _CurrentSaveKey = "current-save";
        static int CurrentSaveIndex {
            get => App.GetOrCreate(_CurrentSaveKey, _FirstSave);
            set {
                if (value is >= _FirstSave and <= Max) {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Save index out of range.");
                }

                if (App.TryGet(_CurrentSaveKey, out int Current)) {
                    ResettableLazy<SaveDataProxy> Lazy = GetSave(Current);
                    if (Lazy.IsValueCreated) {
                        Lazy.Value.Save();
                    }
                }
                App.Set(_CurrentSaveKey, value);
            }
        }

        /// <summary> Gets the current save data. </summary>
        /// <remarks> This is the save data for the save currently selected in the main menu. <br/>
        /// General gameplay data is stored here. </remarks>
        /// <returns> The current save data. </returns>
        public static SaveDataProxy Current => GetSave(CurrentSaveIndex).Value;

        static ResettableLazy<SaveDataProxy> GetSave( int Index ) => _Saves[Index - _FirstSave];

        /// <summary> Gets the save data for the specified index. </summary>
        /// <param name="Index"> The index of the save. </param>
        /// <returns> The save data for the specified index. </returns>
        public static SaveDataProxy Get( int Index ) {
            Debug.Assert(Index is >= _FirstSave and <= Max, "Save index out of range.");
            return GetSave(Index).Value;
        }
    }

    public sealed class SaveDataProxy {
        readonly SaveData _Data;

        internal SaveDataProxy( SaveData Data ) => _Data = Data;

        /// <inheritdoc cref="SaveData.Get{T}(string)"/>
        public T Get<T>( [LocalizationRequired(false)] string Key ) where T : notnull => _Data.Get<T>(Key);

        /// <inheritdoc cref="SaveData.TryGet"/>
        public bool TryGet<T>( [LocalizationRequired(false)] string Key, [NotNullWhen(true)] out T? Value ) where T : notnull => _Data.TryGet(Key, out Value);

        /// <inheritdoc cref="SaveData.GetOrDefault{T}(string,T)"/>
        public T GetOrDefault<T>( [LocalizationRequired(false)] string Key, T Fallback ) where T : notnull => _Data.GetOrDefault(Key, Fallback);

        /// <inheritdoc cref="SaveData.GetOrDefault{T}(string,T)"/>
        public T GetOrDefault<T>( [LocalizationRequired(false)] string Key, Func<T> Fallback ) where T : notnull => _Data.GetOrDefault(Key, Fallback);

        /// <inheritdoc cref="SaveData.GetOrCreate{T}(string,T)"/>
        public T GetOrCreate<T>( [LocalizationRequired(false)] string Key, T Fallback ) where T : notnull => _Data.GetOrCreate(Key, Fallback);

        /// <inheritdoc cref="SaveData.GetOrCreate{T}(string,T)"/>
        public T GetOrCreate<T>( [LocalizationRequired(false)] string Key, Func<T> Fallback ) where T : notnull => _Data.GetOrCreate(Key, Fallback);

        /// <inheritdoc cref="SaveData.Set{T}(string,T)"/>
        public void Set<T>( [LocalizationRequired(false)] string Key, T Value ) where T : notnull => _Data.Set(Key, Value);

        /// <inheritdoc cref="SaveData.Write"/>
        public void Save() => _Data.Write();

        /// <inheritdoc cref="SaveData.WriteAsync"/>
        public UniTask SaveAsync( CancellationToken Token = default ) => _Data.WriteAsync(Token);

        /// <inheritdoc cref="SaveData.Read"/>
        public void Load() => _Data.Read();

        /// <inheritdoc cref="SaveData.ReadAsync"/>
        public UniTask LoadAsync( CancellationToken Token = default ) => _Data.ReadAsync(Token);

    }
}
