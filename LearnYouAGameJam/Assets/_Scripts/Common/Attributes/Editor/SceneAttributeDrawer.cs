using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SysPath = System.IO.Path;
using PureAttribute = System.Diagnostics.Contracts.PureAttribute;

namespace LYGJ.Common.Attributes {
    [UsedImplicitly]
    public sealed class SceneAttributeDrawer : OdinAttributeDrawer<SceneAttribute, string> {

        /// <summary> Gets the name of the scene. </summary>
        /// <param name="Path"> The path to the scene. </param>
        /// <returns> The name of the scene. </returns>
        public static string GetName( string Path ) => SysPath.GetFileNameWithoutExtension(Path);

        /// <summary> Checks if a scene exists. </summary>
        /// <param name="Scene"> The scene to check. </param>
        /// <returns> <see langword="true"/> if the scene exists; otherwise, <see langword="false"/>. </returns>
        [Pure] public static bool SceneExists( [LocalizationRequired(false)] string Scene ) {
            foreach (EditorBuildSettingsScene S in EditorBuildSettings.scenes) {
                if (string.Equals(GetName(S.path), Scene, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary> Attempts to get the path of the given scene name. </summary>
        /// <param name="Scene"> The scene to get the path for. </param>
        /// <param name="Path"> The path of the scene. </param>
        /// <returns> <see langword="true"/> if the scene was found; otherwise, <see langword="false"/>. </returns>
        [Pure] public static bool TryGetPath( [LocalizationRequired(false)] string Scene, [NotNullWhen(true), LocalizationRequired(false)] out string? Path ) {
            foreach (EditorBuildSettingsScene S in EditorBuildSettings.scenes) {
                if (string.Equals(GetName(S.path), Scene, StringComparison.OrdinalIgnoreCase)) {
                    Path = S.path;
                    return true;
                }
            }
            Path = string.Empty;
            return false;
        }

        /// <summary> Gets a label for the given scene. </summary>
        /// <param name="Scene"> The scene to get the label for. </param>
        /// <param name="Icon"> An optional override icon. </param>
        /// <returns> The label for the given scene. </returns>
        public static GUIContent GetSceneLabel( string Scene, Texture2D? Icon = null ) =>
            new(
                text: SysPath.GetFileNameWithoutExtension(Scene),
                tooltip: Scene,
                image: Icon == null ? AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset)) : Icon
            );

        /// <summary> Draws a scene picker. </summary>
        /// <param name="Rect"> The rect to draw the scene picker in. </param>
        /// <param name="Label"> The label to draw. Can be <see langword="null"/>. </param>
        /// <param name="Scene"> The current scene. </param>
        /// <param name="Changed"> The callback to invoke when the selected scene changes. </param>
        /// <param name="IncludeNone"> Whether to include a &quot;None&quot; option. Even if not allowed, an invalid scene may still be returned (i.e. if it was deleted). </param>
        static void DrawScenePicker( Rect Rect, GUIContent? Label, string Scene, Action<string> Changed, bool IncludeNone = false ) {
            if (Label is not null) { Rect = EditorGUI.PrefixLabel(Rect, Label); }
            GUIContent Current = GetSceneLabel(Scene, SceneExists(Scene) ? null : EditorIcons.UnityWarningIcon);

            void NoScene() => Changed(string.Empty);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Rect.Contains(Event.current.mousePosition)) {
                Event.current.Use();
                GenericMenu Menu = new();
                Menu.AddItem(new("Clear", "Force the field to be cleared."), string.IsNullOrEmpty(Scene), NoScene);
                void ThisScene() => Changed(GetName(SceneManager.GetActiveScene().path));
                GUIContent ThisLbl = new("This Scene", "Set the field to the current scene.");
                if (SceneManager.sceneCount > 1) {
                    Menu.AddDisabledItem(ThisLbl);
                } else {
                    Menu.AddItem(ThisLbl, string.Equals(Scene, GetName(SceneManager.GetActiveScene().path), StringComparison.OrdinalIgnoreCase), ThisScene);
                }
                Menu.ShowAsContext();
                return;
            }

            if (GUI.Button(Rect, Current, EditorStyles.popup)) {
                GenericMenu Menu = new();
                if (IncludeNone) {
                    Menu.AddItem(new("None", "No scene."), string.IsNullOrEmpty(Scene), NoScene);
                    Menu.AddSeparator(string.Empty);
                }

                foreach (EditorBuildSettingsScene S in EditorBuildSettings.scenes) {
                    GUIContent Lbl = GetSceneLabel(S.path);
                    string Nm = GetName(S.path);
                    void Callback() => Changed(Nm);
                    Menu.AddItem(Lbl, string.Equals(Nm, Scene, StringComparison.OrdinalIgnoreCase), Callback);
                }
                Menu.DropDown(Rect);
            }
        }

        #region Overrides of OdinAttributeDrawer<SceneAttribute,string>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            void Callback( string Name ) {
                ValueEntry.SmartValue = Name;
                ValueEntry.ApplyChanges();
            }

            bool HasLbl = Lbl is not null;
            Rect Ln = EditorGUILayout.GetControlRect(HasLbl);
            DrawScenePicker(Ln, Lbl, ValueEntry.SmartValue, Callback);
            if (TryGetPath(ValueEntry.SmartValue, out string? Path)) {
                Rect NextLn = EditorGUILayout.GetControlRect(HasLbl, GUILayout.Height(EditorGUIUtility.singleLineHeight * 0.65f));
                if (HasLbl) {
                    NextLn.xMin += EditorGUIUtility.labelWidth;
                }
                EditorGUI.LabelField(NextLn, Path, EditorStyles.centeredGreyMiniLabel);
            }
        }

        #endregion

    }
}
