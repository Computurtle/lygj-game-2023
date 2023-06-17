using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using LYGJ.Common;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LYGJ.SceneManagement.Attributes {
    [UsedImplicitly]
    public sealed class TeleportMarkerAttributeDrawer : OdinAttributeDrawer<TeleportMarkerAttribute, string> {

        public static GUIContent NoMarker => new("None", EditorIcons.X.Active, "No marker.");

        /// <summary> Gets the <see cref="GUIContent"/> for the given marker. </summary>
        /// <param name="Marker"> The marker to get the <see cref="GUIContent"/> for. </param>
        /// <returns> The <see cref="GUIContent"/> for the given marker. </returns>
        public static GUIContent GetContent( string Marker ) => string.IsNullOrEmpty(Marker) ? NoMarker : new(Marker.ConvertNamingConvention(NamingConvention.TitleCase), EditorIcons.Marker.Active, Marker);

        /// <summary> Creates a marker picker menu. </summary>
        /// <param name="Clicked"> The action to perform when a marker is clicked. </param>
        /// <param name="Selected"> The currently selected marker. </param>
        /// <param name="IncludeNone"> Whether or not to include a &quot;None&quot; option. </param>
        /// <returns> The created marker picker menu. </returns>
        public static GenericMenu CreateMenu( Action<string> Clicked, string? Selected = null, bool IncludeNone = true ) {
            GenericMenu Menu = new();
            if (IncludeNone) {
                Menu.AddItem(NoMarker, Selected == null, () => Clicked(string.Empty));
                Menu.AddSeparator(string.Empty);
            }

            foreach (string Marker in Teleports.All.Select(T => T.name).OrderBy(T => T)) {
                Menu.AddItem(GetContent(Marker), string.Equals(Marker, Selected, StringComparison.OrdinalIgnoreCase), () => Clicked(Marker));
            }
            return Menu;
        }

        sealed class CustomMarkerWindow : EditorWindow {
            public string Marker = string.Empty;

            Action<string> _Clicked = _ => { };

            GUIContent _Field, _Button;

            void OnGUI() {
                Rect        Rc      = EditorGUILayout.GetControlRect();
                const float LblW    = 50f;
                const float BtnW    = 50f;
                const float Padding = 2f;

                Rect LblRc = new(Rc) { width = LblW };
                Rect BtnRc = new(Rc) { x = Rc.xMax - BtnW, width = BtnW };
                Rect FldRc = new(Rc) { x = LblRc.xMax + Padding, width = Rc.width - LblW - BtnW - Padding * 2f };

                EditorGUI.HandlePrefixLabel(Rc, LblRc, _Field);
                Marker = EditorGUI.TextField(FldRc, Marker);

                if (GUI.Button(BtnRc, _Button)) {
                    _Clicked(Marker);
                    Close();
                }
            }

            public static void Show( string Current, Action<string> Clicked ) {
                CustomMarkerWindow Window = CreateInstance<CustomMarkerWindow>();
                Window.Marker   = Current;
                Window._Clicked = Clicked;

                Window.titleContent = new("Custom Marker", "Set a custom marker.");
                Window._Field       = new("Marker", "The name of the marker to pick.");
                Window._Button      = new("OK", EditorIcons.Checkmark.Active, "Set the marker.");

                Window.maxSize = new(
                    x: 300,
                    y: EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f
                );
                Window.minSize = Window.maxSize;

                Window.ShowUtility();
            }
        }

        #region Overrides of OdinAttributeDrawer<TeleportMarkerAttribute,string>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            string     Current    = ValueEntry.SmartValue;
            GUIContent CurrentLbl = GetContent(Current);

            Rect Rc;
            if (Lbl is not null) {
                Rc = EditorGUILayout.GetControlRect();
                Rc = EditorGUI.PrefixLabel(Rc, Lbl);
            } else {
                Rc = EditorGUILayout.GetControlRect(false);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Rc.Contains(Event.current.mousePosition)) {
                Event.current.Use();
                GenericMenu Menu = new();

                void SetNone() {
                    ValueEntry.SmartValue = string.Empty;
                    ValueEntry.ApplyChanges();
                }
                Menu.AddItem(new("Clear", "Force the field to be cleared."), false, SetNone);

                void SetCustom( string Custom ) {
                    ValueEntry.SmartValue = Custom;
                    ValueEntry.ApplyChanges();
                }
                void ShowCustomWindow() => CustomMarkerWindow.Show(ValueEntry.SmartValue, SetCustom);
                Menu.AddItem(new("Custom...", "Set a custom marker."), false, ShowCustomWindow);
                Menu.ShowAsContext();
                return;
            }

            if (GUI.Button(Rc, CurrentLbl, EditorStyles.popup)) {
                CreateMenu(M => {
                    ValueEntry.SmartValue = M;
                    ValueEntry.ApplyChanges();
                }, Current).ShowAsContext();
            }
        }

        #endregion

    }
}
