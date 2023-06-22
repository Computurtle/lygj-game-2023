using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace LYGJ.InventoryManagement.Editor {
    [UsedImplicitly]
    public sealed class ItemGroupOrNoneValueDrawer : OdinValueDrawer<ItemGroupOrNone> {

        #region Overrides of OdinValueDrawer<ItemGroupOrNone>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            Debug.Assert(!GUI.enabled, "GUI.enabled is true. The drawer should only be used in read-only mode.");

            Rect Rect = EditorGUILayout.GetControlRect(Lbl is not null);
            if (Lbl is not null) {
                Rect = EditorGUI.PrefixLabel(Rect, Lbl);
            }

            GUIContent Value = EditorGUIUtility.TrTempContent(ValueEntry.SmartValue.TryPickT0(out ItemGroup Group) ? Group.ToString() : "All");

            EditorGUI.LabelField(Rect, Value, EditorStyles.popup);
        }

        #endregion

    }
}
