using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneOf;
using OneOf.Types;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.SaveManagement {
    public sealed class SaveEditor : EditorWindow {

        static void DrawJsonObject( JObject Object, IDictionary<string, bool> Foldouts ) {
            foreach ((string Key, JToken? JToken) in Object) {
                DrawJsonToken(Foldouts, JToken, Key);
            }
        }

        static void DrawJsonArray( JArray Array, IDictionary<string, bool> Foldouts ) {
            for (int Index = 0; Index < Array.Count; Index++) {
                DrawJsonToken(Foldouts, Array[Index], Index.ToString());
            }
        }

        static void DrawJsonToken( IDictionary<string, bool> Foldouts, JToken? JToken, string Key ) {
            switch (JToken) {
                case JObject Obj: {
                    if (!Foldouts.TryGetValue(Key, out bool Foldout)) {
                        Foldout = false;
                    }

                    Foldouts[Key] = EditorGUILayout.Foldout(Foldout, Key, true);
                    if (Foldout) {
                        EditorGUI.indentLevel++;
                        DrawJsonObject(Obj, Foldouts);
                        EditorGUI.indentLevel--;
                    }

                    break;
                }
                case JArray Arr: {
                    if (!Foldouts.TryGetValue(Key, out bool Foldout)) {
                        Foldout = false;
                    }

                    Foldouts[Key] = EditorGUILayout.Foldout(Foldout, Key, true);
                    if (Foldout) {
                        EditorGUI.indentLevel++;
                        DrawJsonArray(Arr, Foldouts);
                        EditorGUI.indentLevel--;
                    }

                    break;
                }
                case null:
                    EditorGUILayout.LabelField(Key, "null");
                    break;
                case JValue Value:
                    // EditorGUILayout.LabelField(Key, Value.ToString());
                    switch (Value.Type) {
                        case JTokenType.String:
                            EditorGUILayout.TextField(Key, Value.ToString());
                            break;
                        case JTokenType.Integer:
                            EditorGUILayout.LongField(Key, Value.ToObject<long>());
                            break;
                        case JTokenType.Float:
                            EditorGUILayout.DoubleField(Key, Value.ToObject<double>());
                            break;
                        case JTokenType.Boolean:
                            EditorGUILayout.Toggle(Key, Value.ToObject<bool>());
                            break;
                        case JTokenType.Null:
                            EditorGUILayout.LabelField(Key, "null");
                            break;
                        default:
                            EditorGUILayout.LabelField(Key, $"Unknown type: {Value.Type}");
                            break;
                    }

                    break;
                default:
                    EditorGUILayout.LabelField(Key, $"Unknown type: {JToken.GetType().GetNiceName()}");
                    break;
            }
        }

        static bool DrawSaveEditor( FileInfo File, string Text, in JsonOrException Json, ref bool ShowJson, ref bool ShowText, ref Vector2 Scroll, IDictionary<string, bool> JsonFoldouts ) {
            SirenixEditorGUI.BeginBox();

            SirenixEditorGUI.BeginBoxHeader();
            EditorGUILayout.LabelField(File.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            GUIContent  PathLbl     = EditorGUIUtility.TrTextContent(File.FullName);
            GUIStyle    Style       = EditorStyles.centeredGreyMiniLabel;
            float       Width       = Style.CalcSize(PathLbl).x;
            const float MaxRelWidth = 0.5f;
            float AvailWidth = EditorGUIUtility.currentViewWidth - 20;
            Width = Mathf.Min(Width, AvailWidth * MaxRelWidth);
            EditorGUILayout.LabelField(PathLbl, Style, GUILayout.Width(Width));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Open", EditorIcons.Folder.Active), EditorStyles.toolbarButton, GUILayout.Width(60))) {
                HighlightFile(File);
            }
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Edit", EditorIcons.Pen.Active), EditorStyles.toolbarButton, GUILayout.Width(50))) {
                EditorUtility.OpenWithDefaultApp(File.FullName);
            }
            if (GUILayout.Button(EditorIcons.X.ActiveGUIContent, EditorStyles.toolbarButton, GUILayout.Width(20))) {
                SirenixEditorGUI.EndBoxHeader();
                SirenixEditorGUI.EndBox();
                return false;
            }
            SirenixEditorGUI.EndBoxHeader();

            Scroll = EditorGUILayout.BeginScrollView(Scroll);

            ShowJson = EditorGUILayout.BeginFoldoutHeaderGroup(ShowJson, EditorGUIUtility.TrTempContent("Json"));
            if (ShowJson) {
                using (new EditorGUI.IndentLevelScope()) {
                    if (Json.IsT0) {
                        if (Json.Count > 0) {
                            using (new EditorGUI.DisabledScope(true)) {
                                DrawJsonObject(Json.AsT0, JsonFoldouts);
                            }
                        } else {
                            EditorGUILayout.HelpBox("No data", MessageType.Info);
                        }
                    } else if (Json.IsT1) {
                        EditorGUILayout.HelpBox(Json.AsT1.Message, MessageType.Error);
                    } else {
                        Debug.Assert(Json.IsT2);
                        EditorGUILayout.HelpBox("No data", MessageType.Info);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            ShowText = EditorGUILayout.BeginFoldoutHeaderGroup(ShowText, EditorGUIUtility.TrTempContent("Text"));
            if (ShowText){
                using (new EditorGUI.IndentLevelScope()) {
                    using (new EditorGUI.DisabledScope(true)) {
                        _ = EditorGUILayout.TextArea(Text);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndScrollView();
            SirenixEditorGUI.EndBox();
            return true;
        }

        static void OpenFolder( DirectoryInfo Dir ) {
            if (Application.platform == RuntimePlatform.WindowsEditor) {
                Process.Start(Dir.FullName);
            } else {
                EditorUtility.RevealInFinder(Dir.FullName);
            }
        }

        static void HighlightFile( FileInfo File ) {
            if (Application.platform == RuntimePlatform.WindowsEditor) {
                Process.Start("explorer", $"/select,\"{File.FullName}\"");
            } else {
                EditorUtility.RevealInFinder(File.FullName);
            }
        }

        static FileInfo? DrawFilePicker( DirectoryInfo Directory, ref Vector2 Scroll, Action? DrawExtraOptions = null ) {
            FileInfo? Chosen = null;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(300));
            if (Directory.Exists) {
                if (EditorGUILayout.LinkButton(Directory.FullName)) {
                    OpenFolder(Directory);
                }

                GUILayout.FlexibleSpace();
                Scroll = EditorGUILayout.BeginScrollView(Scroll);
                bool Any = false;
                foreach (FileInfo File in Directory.EnumerateFiles("*.json")) {
                    Any = true;
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(File.Name, EditorStyles.toolbarButton)) {
                        Chosen = File;
                    }

                    if (GUILayout.Button(EditorIcons.X.ActiveGUIContent, EditorStyles.toolbarButton, GUILayout.Width(20))) {
                        File.Delete();
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (!Any) {
                    EditorGUILayout.LabelField("No files found", EditorStyles.centeredGreyMiniLabel);
                    if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Open Folder", EditorIcons.Folder.Active), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                        OpenFolder(Directory);
                    }
                }

                GUILayout.Space(10);
                EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("..."), EditorStyles.centeredGreyMiniLabel);
                DrawExtraOptions?.Invoke();
                EditorGUILayout.EndScrollView();
                GUILayout.FlexibleSpace();
            } else {
                EditorGUILayout.LabelField("No directory found", EditorStyles.centeredGreyMiniLabel);
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Create Directory", EditorIcons.Folder.Active), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                    Directory.Create();
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return Chosen;
        }

        (FileInfo File, string Text, JsonOrException Json, FileSystemWatcher Watcher)? _Current;

        bool    _ShowJson = true, _ShowText = false;
        Vector2 _Scroll;

        readonly Dictionary<string, bool> _JsonFoldouts = new();

        void OnDestroy() {
            if (_Current is (_, _, _, { } Watcher)) {
                Watcher.Dispose();
            }
        }

        static void DrawExtraOptions() {
            if (!File.Exists(Saves.AppFile.FullName) && GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Create Application Settings", EditorIcons.SettingsCog.Active), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                _ = Saves.App;
            }

            if (Saves.TryGetNewSaveIndex(out int Index)) {
                GUILayout.Space(10);
                if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Create New Game", EditorIcons.Play.Active), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                    _ = Saves.Get(Index);
                }
            }
        }

        void OnGUI() {
            if (_Current is ({ } Fl, { } Text, { } Json, { } Watcher)) {
                if (!DrawSaveEditor(Fl, Text, Json, ref _ShowJson, ref _ShowText, ref _Scroll, _JsonFoldouts)) {
                    ResetView();
                }
            } else {
                FileInfo? NewCurrent = DrawFilePicker(Saves.Directory, ref _Scroll, DrawExtraOptions);
                if (NewCurrent is not null) {
                    _Scroll = Vector2.zero;
                    Watcher = new(NewCurrent.DirectoryName!, NewCurrent.Name) {
                        NotifyFilter        = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };
                    Watcher.Changed += ( _, _ ) => UpdateCurrent(NewCurrent, Watcher);
                    Watcher.Deleted += ( _, _ ) => ResetView();
                    UpdateCurrent(NewCurrent, Watcher);
                }
            }
        }

        public sealed class JsonOrException : OneOfBase<JObject, Exception, None> {
            /// <inheritdoc />
            JsonOrException( OneOf<JObject, Exception, None> Input ) : base(Input) { }

            /// <summary> Parses a string into a <see cref="JsonOrException"/>. </summary>
            public static JsonOrException Parse( string Text ) {
                try {
                    JObject? Object = JsonConvert.DeserializeObject<JObject>(Text, SaveData.SerialiserSettings);
                    return Object is not null ? new(Object) : new(new None());
                } catch (Exception E) {
                    return new(E);
                }
            }

            /// <summary> The number of elements in the JSON object. </summary>
            public int Count => Match(Json => Json.Count, _ => 0, _ => 0);
        }

        void UpdateCurrent( FileInfo NewCurrent, FileSystemWatcher Watcher ) {
            string          Text = File.ReadAllText(NewCurrent.FullName);
            JsonOrException Json = JsonOrException.Parse(Text);
            _ShowJson = Json.Count > 0;
            _ShowText = !_ShowJson;
            _Current  = (NewCurrent, Text, Json, Watcher);
        }

        void ResetView() {
            if (_Current is (_, _, var Json, { } Watcher)) {
                Watcher.Dispose();
                _ShowJson = Json.Count > 0;
                _ShowText = !_ShowJson;
                _Current  = null;
            } else {
                _ShowJson = true;
                _ShowText = false;
            }
            _JsonFoldouts.Clear();
            _Scroll = Vector2.zero;
        }

        [MenuItem("LYGJ/Save Editor")]
        public static void ShowWindow() {
            SaveEditor Window = GetWindow<SaveEditor>();
            Window.titleContent = EditorGUIUtility.TrTextContentWithIcon("Save Editor", EditorIcons.SettingsCog.Active);
            Window.Show();
        }
    }
}
