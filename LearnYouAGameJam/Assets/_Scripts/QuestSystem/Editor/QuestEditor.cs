using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.QuestSystem {
    public static class QuestEditor {
        [MenuItem("LYGJ/Quests/Create Missing...", false)]
        static void CreateMissingQuests() {
            HashSet<Type> ToCreate = new();
            ToCreate.AddRange(TypeCache.GetTypesDerivedFrom<Quest>());
            foreach (Quest Q in Quests.All) {
                ToCreate.Remove(Q.GetType());
            }

            if (ToCreate.Count > 0) {
                string Folder = $"Assets/Resources/{Quests.ResourcesPath}";
                if (!AssetDatabase.IsValidFolder(Folder)) {
                    AssetDatabase.CreateFolder("Assets/Resources", Quests.ResourcesPath);
                }
                foreach (Type T in ToCreate) {
                    Quest Q = (Quest)ScriptableObject.CreateInstance(T);
                    AssetDatabase.CreateAsset(Q, $"{Folder}/{Q.Key}.asset");
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"Created {ToCreate.Count} missing quests.\n\t{string.Join("\n\t", ToCreate.Select(T => T.Name))}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder);
            }
        }
        [MenuItem("LYGJ/Quests/Create Missing...", true)]
        static bool CreateMissingQuestsValidate() {
            HashSet<Type> ToCreate = new();
            ToCreate.AddRange(TypeCache.GetTypesDerivedFrom<Quest>());
            foreach (Quest Q in Quests.All) {
                ToCreate.Remove(Q.GetType());
            }

            return ToCreate.Count > 0;
        }

        abstract class PickerWindow<T> : EditorWindow {
            T          _Value;
            GUIContent _Label;
            Action<T>  _Callback;
            bool       _RanCallback;
            float      _LabelW;

            GUIContent _Button;

            protected static void Setup( PickerWindow<T> Window, string Label, T Initial, Action<T> Callback ) {
                Window._Value       = Initial;
                Window._Label       = new(Label);
                Window._LabelW      = EditorStyles.label.CalcSize(Window._Label).x + 10f;
                Window.titleContent = Window._Label;
                Window._Callback    = Callback;

                Window._Button = new("OK", EditorIcons.Checkmark.Active);

                Window.maxSize = new(300, EditorGUIUtility.singleLineHeight + 10f);
                Window.ShowUtility();
            }

            /// <summary> Draws the field. </summary>
            /// <param name="Rc"> The rect to draw the field in. </param>
            /// <param name="Current"> The current value of the field. </param>
            /// <returns> The new value of the field. </returns>
            protected abstract T DrawField(Rect Rc, T Current);

            void OnGUI() {
                EditorGUILayout.BeginHorizontal();
                Rect Rc    = EditorGUILayout.GetControlRect();
                Rect LblRc = new(Rc) { width = _LabelW };

                EditorGUI.HandlePrefixLabel(Rc, LblRc, _Label);
                Rect FldRc = new(Rc) { x = LblRc.xMax + 2f, width = Rc.width - 4f - 50f };

                _Value = DrawField(FldRc, _Value);
                if (GUILayout.Button(_Button, GUILayout.Width(50), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
                    Finalise();
                }
                EditorGUILayout.EndHorizontal();
            }

            protected void Finalise( T Value ) {
                _Value = Value;
                Finalise();
            }

            void Finalise() {
                _RanCallback = true;
                _Callback(_Value);
                Close();
            }

            /// <summary> Gets the value to return when the window is closed. </summary>
            protected abstract T CloseValue { get; }

            void OnDestroy() {
                if (!_RanCallback) {
                    _RanCallback = true;
                    _Callback(CloseValue);
                }
            }
        }

        sealed class QuestPickerWindow : PickerWindow<string> {

            #region Overrides of PickerWindow<string>

            /// <inheritdoc />
            protected override string DrawField( Rect Rc, string Current ) {
                const float ButtonW = 20f, Padding = 2f;
                Rect        ButtonR = new(Rc) { x = Rc.x, width = ButtonW };
                Rc.xMin = ButtonR.xMax + Padding;
                QuestAttributeDrawer.DrawPickerButton(ButtonR, Current, Finalise, AllowNone: true);
                Current = EditorGUI.TextField(Rc, Current);
                return Current;
            }

            /// <inheritdoc />
            protected override string CloseValue => string.Empty;

            #endregion

            /// <summary> Shows the picker window. </summary>
            /// <param name="Label"> The label to display. </param>
            /// <param name="Initial"> The initial value. </param>
            /// <param name="Callback"> The callback to run when the window is closed. </param>
            public static void Show( string Label, string Initial, Action<string> Callback ) {
                QuestPickerWindow Window = GetWindow<QuestPickerWindow>();
                Setup(Window, Label, Initial, Callback);
            }

        }

        [MenuItem("LYGJ/Quests/Start Quest", false)]
        static void StartQuest() {
            // Prompt user to enter quest ID
            QuestPickerWindow.Show("Quest ID", string.Empty, ID => {
                if (string.IsNullOrEmpty(ID)) { return; }

                if (Quests.Exists(ID)) {
                    Quests.Start(ID);
                } else {
                    EditorUtility.DisplayDialog("Error", $"Quest '{ID}' does not exist.", "OK");
                }
            });
        }

        [MenuItem("LYGJ/Quests/Start Quest", true)]
        static bool StartQuestValidation() => Application.isPlaying;
    }
}
