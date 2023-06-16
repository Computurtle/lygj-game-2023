using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ.VFX.Editor {
    [CustomEditor(typeof(ViewScaler))]
    public sealed class ViewScalerEditor : UnityEditor.Editor {

        static TComponent? ComponentField<TComponent>( string Label, string Tooltip, TComponent Value ) where TComponent : Component => (TComponent?)EditorGUILayout.ObjectField(EditorGUIUtility.TrTextContent(Label, Tooltip), Value, typeof(TComponent), true);

        static TEnum EnumField<TEnum>( string Label, string Tooltip, TEnum Value ) where TEnum : struct, Enum => (TEnum)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent(Label, Tooltip), Value);

        static float SliderField( string Label, string Tooltip, float Value, float Min, float Max ) => EditorGUILayout.Slider(EditorGUIUtility.TrTextContent(Label, Tooltip), Value, Min, Max);

        static float FloatField( string Label, string Tooltip, float Value, float Min = float.MinValue, float Max = float.MaxValue ) => Mathf.Clamp(EditorGUILayout.FloatField(EditorGUIUtility.TrTextContent(Label, Tooltip), Value), Min, Max);

        static int LayerField( string Label, string Tooltip, int Value ) => EditorGUILayout.LayerField(EditorGUIUtility.TrTextContent(Label, Tooltip), Value);

        #region Overrides of Editor

        /// <inheritdoc />
        public override void OnInspectorGUI() {
            ViewScaler Target = (ViewScaler)target;

            Camera   Input  = Target.InputCamera;
            RawImage Output = Target.OutputImage;
            int      Layer  = Target.OutputLayer;
            if (Application.isPlaying) {
                EditorGUI.BeginDisabledGroup(true);
            }
            Input  = ComponentField("Input Camera", "The input camera.", Input)!;
            Output = ComponentField("Output Image", "The output image.", Output)!;
            Layer  = LayerField("Output Layer", "The layer for the output.", Layer);
            if (Application.isPlaying) {
                EditorGUI.EndDisabledGroup();
            } else {
                Target.InputCamera  = Input;
                Target.OutputImage = Output;
                Target.OutputLayer  = Layer;
            }

            EditorGUILayout.Space();
            Target.Mode = EnumField("Mode", "The scaling mode.", Target.Mode);
            switch (Target.Mode) {
                case ViewScaler.ScaleMode.Relative:
                    Target.RelativeScale = SliderField("Relative Scale", "The relative scale as a percentage.", Target.RelativeScale, 0.0001f, 1f);
                    break;
                case ViewScaler.ScaleMode.FixedWidth:
                    Target.FixedWidthScale = FloatField("Width", "The fixed width scale, in pixels.", Target.FixedWidthScale, 1f, float.MaxValue);
                    break;
                case ViewScaler.ScaleMode.FixedHeight:
                    Target.FixedHeightScale = FloatField("Height", "The fixed height scale, in pixels.", Target.FixedHeightScale, 1f, float.MaxValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Repaint", "Repaint the output image."))) {
                Target.Repaint();
            }
        }

        #endregion

    }
}
