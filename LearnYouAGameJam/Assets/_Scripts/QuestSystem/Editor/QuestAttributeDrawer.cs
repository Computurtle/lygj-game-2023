using System;
using System.Diagnostics;
using JetBrains.Annotations;
using LYGJ.Common;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LYGJ.QuestSystem {
    [UsedImplicitly]
    public sealed class QuestAttributeDrawer : OdinAttributeDrawer<QuestAttribute, string> {

        static readonly ResettableLazy<GUIContent>
            _DropdownButton = new(() => new(EditorGUIUtility.IconContent("_Menu").image, "Select a quest."));

        [ExecuteOnReload]
        static void Cleanup() => _DropdownButton.Reset();

        /// <summary> Draws a picker button for selecting a quest. </summary>
        /// <param name="Rect"> The rect to draw the button in. </param>
        /// <param name="Current"> The current quest. </param>
        /// <param name="Changed"> The callback to invoke when the quest is changed. </param>
        /// <param name="AllowNone"> Whether or not to allow the user to select &quot;None&quot; as an option. </param>
        public static void DrawPickerButton( Rect Rect, string Current, Action<string> Changed, bool AllowNone = false ) {
            if (GUI.Button(Rect, _DropdownButton.Value, EditorStyles.iconButton)) {
                GenericMenu Menu = CreateMenu(Current, Changed, AllowNone);
                Menu.ShowAsContext();
            }
        }

        /// <summary> Creates a menu for selecting a quest. </summary>
        /// <param name="Current"> The current quest. </param>
        /// <param name="Changed"> The callback to invoke when the quest is changed. </param>
        /// <param name="AllowNone"> Whether or not to allow the user to select &quot;None&quot; as an option. </param>
        /// <returns> The created menu. </returns>
        public static GenericMenu CreateMenu( string Current, Action<string> Changed, bool AllowNone ) {
            GenericMenu Menu = new();
            if (AllowNone) {
                void ChangeToNone() => Changed(string.Empty);
                Menu.AddItem(new("None", "No quest"), string.IsNullOrEmpty(Current), ChangeToNone);
                Menu.AddSeparator(string.Empty);
            }

            foreach (Quest Q in Quests.All) {
                void Callback() => Changed(Q.Key);
                Menu.AddItem(new(Q.Name, Q.Key), string.Equals(Current, Q.Key, StringComparison.OrdinalIgnoreCase), Callback);
            }

            return Menu;
        }

        #region Overrides of OdinAttributeDrawer<QuestAttribute,string>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            Rect Rc = EditorGUILayout.GetControlRect(Lbl is not null);
            if (Lbl is not null) {
                Rc = EditorGUI.PrefixLabel(Rc, Lbl);
            }

            const float PickerWidth = 20f;
            const float Padding     = 2f;
            Rect ButtonRc = new(Rc) { x = Rc.xMin, width = PickerWidth };
            Rect FieldRc  = new(Rc) { x = Rc.xMin + PickerWidth + Padding, width = Rc.width - PickerWidth - Padding };

            string Current = ValueEntry.SmartValue;
            void Changed( string S ) {
                ValueEntry.SmartValue = S;
                ValueEntry.ApplyChanges();
            }
            DrawPickerButton(ButtonRc, Current, Changed, Attribute.AllowNone);
            EditorGUI.BeginChangeCheck();
            string NewValue = EditorGUI.TextField(FieldRc, Current);
            if (EditorGUI.EndChangeCheck()) {
                Changed(NewValue);
            }
        }

        #endregion

    }
}
