using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common.Attributes {
    [UsedImplicitly]
    public sealed class AnimParamAttributeDrawer : OdinAttributeDrawer<AnimParamAttribute, string> {

        static readonly ResettableLazy<GUIContent>
            _DropdownButton = new(() => new(EditorGUIUtility.IconContent("_Menu").image, "Select a parameter from the animator component."));

        [ExecuteOnReload]
        static void Cleanup() => _DropdownButton.Reset();

        const float _DropdownWidth = 20f, _Padding = 2f;

        static bool TryFindAnimator( Component Target, RelativeComponentSource Source, [NotNullWhen(true)] out Animator? Found ) {
            switch (Source) {
                case RelativeComponentSource.This:
                    return Target.TryGetComponent(out Found);
                case RelativeComponentSource.Children:
                    Found = Target.GetComponentInChildren<Animator>();
                    return Found != null;
                case RelativeComponentSource.Parent:
                    Found = Target.GetComponentInParent<Animator>();
                    return Found != null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Source), Source, null);
            }
        }

        static bool TryGetValue<T>( IPropertyValueEntry? ValueEntry, [NotNullWhen(true)] out T? Value ) {
            if (ValueEntry is { WeakSmartValue: T V }) {
                Value = V;
                return true;
            }
            Value = default;
            return false;
        }

        static bool TryGetTarget( InspectorProperty Property, [NotNullWhen(true)] out Component? Target ) {
            InspectorProperty? Parent = Property.Parent;
            while (Parent != null) {
                if (TryGetValue(Parent.ValueEntry, out Target)) {
                    return true;
                }

                Parent = Parent.Parent;
            }

            Target = null;
            return false;
        }

        static IEnumerable<string> GetParameters( Animator? Anim ) {
            if (Anim == null) {
                return Enumerable.Empty<string>();
            }

            RuntimeAnimatorController? Ctrl = Anim.runtimeAnimatorController;
            if (Ctrl is AnimatorOverrideController Override) {
                Ctrl = Override.runtimeAnimatorController;
            }

            if (Ctrl is AnimatorController Controller) {
                return Controller.parameters.Select(P => P.name);
            }

            if (Ctrl != null) {
                Debug.LogWarning($"Animator controller of type {(Ctrl == null ? "null" : Ctrl.GetType().GetNiceName())} is not supported.");
            }
            return Enumerable.Empty<string>();
        }

        #region Overrides of OdinAttributeDrawer<AnimParamAttribute,string>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            // base.DrawPropertyLayout(Lbl);
            Rect Rc = EditorGUILayout.GetControlRect();
            if (Lbl is not null) {
                Rc = EditorGUI.PrefixLabel(Rc, Lbl);
            }

            Rect Rc_Field;
            bool DidFindTarget;

            if (TryGetTarget(Property, out Component? Target)) {
                Rect Rc_Dropdown = new(Rc) { width = _DropdownWidth };
                if (GUI.Button(Rc_Dropdown, _DropdownButton.Value, EditorStyles.iconButton)) {
                    GenericMenu Menu = new();
                    bool        Any  = false;
                    if (TryFindAnimator(Target, Attribute.Source, out Animator? Anim)) {
                        foreach (string Param in GetParameters(Anim)) {
                            Any = true;
                            void Callback() {
                                ValueEntry.SmartValue = Param;
                                if (!ValueEntry.ApplyChanges()) {
                                    Debug.LogError($"Failed to apply changes to {Property.Name}.");
                                }
                            }
                            Menu.AddItem(new(Param), false, Callback);
                        }
                    }

                    if (!Any) {
                        Menu.AddDisabledItem(new("No animator component, runtime controller, or parameters found."));
                    }
                    Menu.ShowAsContext();
                }
                Rc_Field      = new(Rc) { xMin = Rc.xMin + _DropdownWidth + _Padding };
                DidFindTarget = true;
            } else {
                Rc_Field      = Rc;
                DidFindTarget = false;
            }

            ValueEntry.SmartValue = EditorGUI.TextField(Rc_Field, ValueEntry.SmartValue);
            ValueEntry.ApplyChanges();

            if (!DidFindTarget) {
                SirenixEditorGUI.ErrorMessageBox("No GameObject found in parent hierarchy.");
            }
        }

        #endregion

    }
}
