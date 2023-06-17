using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Sirenix.Utilities;
using UnityEngine;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace LYGJ.SaveManagement {
    public sealed class SaveData {
        readonly FileInfo _File;
        JObject           _Data;

        SaveData( FileInfo File, bool ReadImmediate ) {
            _File = File;
            if (ReadImmediate) {
                Read();
            } else {
                _Data = new();
            }
        }

        static readonly JsonSerializerSettings _SerializerSettings = new() {
            Formatting = Formatting.Indented,
            Converters = {
                new StringEnumConverter()
            }
        };
        static readonly JsonSerializer _Serializer = JsonSerializer.Create(_SerializerSettings);

        /// <summary> Creates a new <see cref="SaveData"/> instance. </summary>
        /// <param name="File"> The file to use. </param>
        /// <param name="ReadImmediate"> Whether to read the save data immediately. </param>
        /// <param name="CreateIfNotExists"> Whether to create the file if it does not exist. </param>
        /// <exception cref="FileNotFoundException"> Thrown if the file does not exist and <paramref name="CreateIfNotExists"/> is <see langword="false"/>. </exception>
        public static SaveData Create( FileInfo File, bool ReadImmediate = true, bool CreateIfNotExists = true ) {
            if (!File.Exists) {
                if (!CreateIfNotExists) {
                    throw new FileNotFoundException($"Save file {File.FullName} does not exist.", File.FullName);
                }

                System.IO.File.WriteAllText(File.FullName, "{}");
                return new(File, false); // No reason to read as we just created the file.
            }

            return new(File, ReadImmediate);
        }

        /// <summary> Creates a new <see cref="SaveData"/> instance. </summary>
        /// <param name="File"> The file to use. </param>
        /// <param name="CreateIfNotExists"> Whether to create the file if it does not exist. </param>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> A <see cref="UniTask"/> representing the asynchronous operation. </returns>
        /// <exception cref="FileNotFoundException"> Thrown if the file does not exist and <paramref name="CreateIfNotExists"/> is <see langword="false"/>. </exception>
        public static async UniTask<SaveData> CreateAsync( FileInfo File, bool CreateIfNotExists = true, CancellationToken Token = default ) {
            if (!File.Exists) {
                if (!CreateIfNotExists) {
                    throw new FileNotFoundException($"Save file {File.FullName} does not exist.", File.FullName);
                }

                await System.IO.File.WriteAllTextAsync(File.FullName, "{}", Token);
                return new(File, false); // No reason to read as we just created the file.
            }

            SaveData Data = new(File, false);
            await Data.ReadAsync(Token);
            return Data;
        }

        /// <summary> Reads the save data from the local file. </summary>
        public void Read() {
            if (!_File.Exists) {
                Debug.LogWarning($"Save file {_File.FullName} does not exist. Creating new save file.");
                _Data = new();
                return;
            }

            using StreamReader Reader = File.OpenText(_File.FullName);
            using JsonReader   Json   = new JsonTextReader(Reader);

            try {
                _Data = JObject.Load(Json);
            } catch (JsonReaderException Ex) {
                Debug.LogError($"Failed to read save file {_File.FullName}. See subsequent exception for details.");
                Debug.LogException(Ex);
                _Data = new();
            }
        }

        /// <summary> Asynchronously reads the save data from the local file. </summary>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> A <see cref="UniTask"/> representing the asynchronous operation. </returns>
        public async UniTask ReadAsync( CancellationToken Token = default ) {
            if (!_File.Exists) {
                Debug.LogWarning($"Save file {_File.FullName} does not exist. Creating new save file.");
                _Data = new();
                return;
            }

            using StreamReader Reader = File.OpenText(_File.FullName);
            using JsonReader   Json   = new JsonTextReader(Reader);

            try {
                _Data = await JObject.LoadAsync(Json, Token);
            } catch (JsonReaderException Ex) {
                Debug.LogError($"Failed to read save file {_File.FullName}. See subsequent exception for details.");
                Debug.LogException(Ex);
                _Data = new();
            }
        }

        /// <summary> Writes the save data to the local file. </summary>
        public void Write() {
            using StreamWriter Writer = File.CreateText(_File.FullName);
            using JsonWriter   Json   = new JsonTextWriter(Writer);

            _Serializer.Serialize(Json, _Data);
        }

        /// <summary> Asynchronously writes the save data to the local file. </summary>
        /// <param name="Token"> The cancellation token to use. </param>
        /// <returns> A <see cref="UniTask"/> representing the asynchronous operation. </returns>
        public async UniTask WriteAsync( CancellationToken Token = default ) {
            await using StreamWriter Writer = File.CreateText(_File.FullName);
            using JsonWriter         Json   = new JsonTextWriter(Writer);

            _Serializer.Serialize(Json, _Data);
        }

        /// <summary> Attempts to get the variable with the specified name. </summary>
        /// <param name="Name"> The name of the variable to get. </param>
        /// <param name="Value"> The value of the variable, if found. </param>
        /// <returns> <see langword="true"/> if the variable was found, <see langword="false"/> otherwise. </returns>
        public bool TryGet( [LocalizationRequired(false)] string Name, [NotNullWhen(true)] out JToken? Value ) {
            Value = _Data[Name.ToLowerInvariant()];
            return Value != null;
        }

        /// <inheritdoc cref="TryGet(string,out JToken?)"/>
        /// <typeparam name="T"> The type of the variable to get. </typeparam>
        public bool TryGet<T>( [LocalizationRequired(false)] string Name, [NotNullWhen(true)] out T? Value ) where T : notnull {
            if (TryGet(Name, out JToken? Token)) {
                T? TypedValue = Token.ToObject<T>(_Serializer);
                if (TypedValue is not null) {
                    Value = TypedValue;
                    return true;
                }

                Debug.LogError($"Failed to convert {Token} to {typeof(T).GetNiceName()}. Actual type: {Token.Type}");
            }

            Value = default;
            return false;
        }

        /// <summary> Gets the variable with the specified name. </summary>
        /// <param name="Name"> The name of the variable to get. </param>
        /// <returns> The value of the variable, if found. </returns>
        /// <exception cref="KeyNotFoundException"> Thrown if the variable was not found. </exception>
        public JToken Get( [LocalizationRequired(false)] string Name ) {
            if (TryGet(Name, out JToken? Value)) {
                return Value;
            }

            throw new KeyNotFoundException($"Failed to find variable with name {Name}.");
        }

        /// <inheritdoc cref="Get(string)"/>
        /// <typeparam name="T"> The type of the variable to get. </typeparam>
        /// <exception cref="KeyNotFoundException"> Thrown if the variable was not found. </exception>
        /// <exception cref="InvalidCastException"> Thrown if the variable could not be converted to the specified type. </exception>
        public T Get<T>( [LocalizationRequired(false)] string Name ) where T : notnull {
            JToken Value      = Get(Name.ToLowerInvariant());
            T?     TypedValue = Value.ToObject<T>(_Serializer);
            return TypedValue is not null ? TypedValue : throw new InvalidCastException($"Failed to convert {Value} to {typeof(T).GetNiceName()}. Actual type: {Value.Type}");
        }

        /// <summary> Gets the variable with the specified name, returning a fallback value if it was not found. </summary>
        /// <param name="Name"> The name of the variable to get. </param>
        /// <param name="Fallback"> The fallback value to return if the variable was not found. </param>
        /// <returns> The value of the variable, if found, or the fallback value otherwise. </returns>
        public JToken GetOrDefault( [LocalizationRequired(false)] string Name, Func<JToken> Fallback ) {
            if (TryGet(Name.ToLowerInvariant(), out JToken? Value)) {
                return Value;
            }

            return Fallback();
        }

        /// <inheritdoc cref="GetOrDefault(string,System.Func{Newtonsoft.Json.Linq.JToken})"/>
        public JToken GetOrDefault( [LocalizationRequired(false)] string Name, JToken Fallback ) => GetOrDefault(Name, () => Fallback);

        /// <inheritdoc cref="GetOrDefault(string,System.Func{Newtonsoft.Json.Linq.JToken})"/>
        public T GetOrDefault<T>( [LocalizationRequired(false)] string Name, Func<T> Fallback ) where T : notnull {
            if (TryGet(Name, out T? Value)) {
                return Value;
            }

            return Fallback();
        }

        /// <inheritdoc cref="GetOrDefault(string,System.Func{Newtonsoft.Json.Linq.JToken})"/>
        public T GetOrDefault<T>( [LocalizationRequired(false)] string Name, T Fallback ) where T : notnull => GetOrDefault(Name, () => Fallback);

        /// <summary> Gets the variable with the specified name, creating it if it was not found. </summary>
        /// <param name="Name"> The name of the variable to get. </param>
        /// <param name="Creator"> The function to create the variable if it was not found. </param>
        /// <returns> The value of the variable. </returns>
        public JToken GetOrCreate( [LocalizationRequired(false)] string Name, Func<JToken> Creator ) {
            Name = Name.ToLowerInvariant();
            if (TryGet(Name, out JToken? Value)) {
                return Value;
            }

            JToken CreatedValue = Creator();
            _Data[Name] = CreatedValue;
            return CreatedValue;
        }

        /// <inheritdoc cref="GetOrCreate(string,Func{JToken})"/>
        public JToken GetOrCreate( [LocalizationRequired(false)] string Name, JToken Creator ) => GetOrCreate(Name, () => Creator);

        /// <inheritdoc cref="GetOrCreate(string,Func{JToken})"/>
        public T GetOrCreate<T>( [LocalizationRequired(false)] string Name, Func<T> Creator ) where T : notnull {
            Name = Name.ToLowerInvariant();
            if (TryGet(Name, out T? Value)) {
                return Value;
            }

            T CreatedValue = Creator();
            _Data[Name] = JToken.FromObject(CreatedValue, _Serializer);
            return CreatedValue;
        }

        /// <inheritdoc cref="GetOrCreate{T}(string,Func{T})"/>
        public T GetOrCreate<T>( [LocalizationRequired(false)] string Name, T Creator ) where T : notnull => GetOrCreate(Name, () => Creator);

        /// <summary> Sets the variable with the specified name. </summary>
        /// <param name="Name"> The name of the variable to set. </param>
        /// <param name="Value"> The value to set the variable to. </param>
        public void Set( [LocalizationRequired(false)] string Name, JToken Value ) => _Data[Name.ToLowerInvariant()] = Value;

        /// <inheritdoc cref="Set(string,JToken)"/>
        /// <typeparam name="T"> The type of the variable to set. </typeparam>
        public void Set<T>( [LocalizationRequired(false)] string Name, T Value ) where T : notnull => _Data[Name.ToLowerInvariant()] = JToken.FromObject(Value, _Serializer);
        
        /// <summary> Removes the variable with the specified name. </summary>
        /// <param name="Name"> The name of the variable to remove. </param>
        /// <returns> <see langword="true"/> if the variable was removed, <see langword="false"/> otherwise. </returns>
        public bool Remove( [LocalizationRequired(false)] string Name ) => _Data.Remove(Name.ToLowerInvariant());

        /// <summary> Gets or sets the variable with the specified name. </summary>
        /// <param name="Name"> The name of the variable to get or set. </param>
        public JToken this[ [LocalizationRequired(false)] string Name ] {
            get => Get(Name);
            set => Set(Name, value);
        }

        /// <summary> Gets the name of a variable with the specified segments. </summary>
        /// <param name="Segments"> The segments of the variable name. </param>
        /// <returns> The name of the variable. </returns>
        [Pure, MustUseReturnValue]
        [return: LocalizationRequired(false)]
        public static string GetName( [LocalizationRequired(false)] params string[] Segments ) => string.Join('.', Segments).ToLowerInvariant();

        /// <summary> Gets or sets the variable with the specified name. </summary>
        /// <param name="Segments"> The name segments of the variable to get or set. </param>
        public JToken this[ [LocalizationRequired(false)] params string[] Segments ] {
            get => Get(GetName(Segments));
            set => Set(GetName(Segments), value);
        }

    }
}
