using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

namespace LYGJ.VFX {
    public sealed class ViewScaler : MonoBehaviour {

        [SerializeField, Tooltip("The input camera.")]                Camera   _InputCamera;
        [SerializeField, Tooltip("The output image.")]                RawImage _OutputImage;
        [SerializeField, Tooltip("The layer for the output camera.")] int      _OutputLayer = 31;

        /// <summary> Gets the input camera. </summary>
        public Camera InputCamera {
            get => _InputCamera;
            #if UNITY_EDITOR
            set {
                if (Application.isPlaying) { throw new InvalidOperationException("Cannot set input camera in play mode."); }

                _InputCamera = value;
                UpdateView();
                UpdateCameras(Silent: true);
            }
            #endif
        }

        /// <summary> Gets the output image. </summary>
        public RawImage OutputImage {
            get => _OutputImage;
            #if UNITY_EDITOR
            set {
                if (Application.isPlaying) { throw new InvalidOperationException("Cannot set output image in play mode."); }

                _OutputImage = value;
                UpdateView();
                UpdateCameras(Silent: true);
            }
            #endif
        }

        /// <summary> Gets the output layer. </summary>
        public int OutputLayer {
            get => _OutputLayer;
            #if UNITY_EDITOR
            set {
                if (Application.isPlaying) { throw new InvalidOperationException("Cannot set output layer in play mode."); }

                _OutputLayer = value;
                UpdateView();
                UpdateCameras(Silent: true);
            }
            #endif
        }

        public enum ScaleMode {
            /// <summary> A percentage pixel density relative to the screen size. </summary>
            Relative,
            /// <summary> A fixed pixel density, set from width. </summary>
            FixedWidth,
            /// <summary> A fixed pixel density, set from height. </summary>
            FixedHeight
        }

        /// <summary> Gets or sets the scaling mode. </summary>
        public ScaleMode Mode {
            get => _Mode;
            set {
                if (_Mode == value) { return; }

                _Mode = value;
                #if UNITY_EDITOR
                if (!Application.isPlaying) { return; }
                #endif
                UpdateView();
            }
        }

        [SerializeField, Tooltip("The scaling mode.")]
        ScaleMode _Mode = ScaleMode.Relative;

        [SerializeField, Tooltip("The relative scale."), Range(0.0001f, 1f)]
        float _RelativeScale = 0.25f;

        /// <summary> Gets or sets the relative scale. </summary>
        public float RelativeScale {
            get => _RelativeScale;
            set {
                if (_RelativeScale == value) { return; }

                _RelativeScale = value;
                if (_Mode != ScaleMode.Relative) { return; }
                #if UNITY_EDITOR
                if (!Application.isPlaying) { return; }
                #endif
                UpdateView();
            }
        }

        [SerializeField, Tooltip("The fixed width scale."), Min(1f)]
        float _FixedWidthScale = 480;

        /// <summary> Gets or sets the fixed width scale. </summary>
        public float FixedWidthScale {
            get => _FixedWidthScale;
            set {
                if (_FixedWidthScale == value) { return; }

                _FixedWidthScale = value;
                if (_Mode != ScaleMode.FixedWidth) { return; }
                #if UNITY_EDITOR
                if (!Application.isPlaying) { return; }
                #endif
                UpdateView();
            }
        }

        /// <summary> Gets or sets the fixed height scale. </summary>
        [SerializeField, Tooltip("The fixed height scale."), Min(1f)]
        float _FixedHeightScale = 270;

        /// <summary> Gets or sets the fixed height scale. </summary>
        public float FixedHeightScale {
            get => _FixedHeightScale;
            set {
                if (_FixedHeightScale == value) { return; }

                _FixedHeightScale = value;
                if (_Mode != ScaleMode.FixedHeight) { return; }
                #if UNITY_EDITOR
                if (!Application.isPlaying) { return; }
                #endif
                UpdateView();
            }
        }

        RenderTexture?      _Target;
        static readonly int _ViewScaler     = Shader.PropertyToID("_ViewScaler");
        static readonly int _ViewScalerSize = Shader.PropertyToID("_ViewScalerSize");

        static void GetSize( int ScreenWidth, int ScreenHeight, ScaleMode Mode, float RelativeScale, float FixedWidthScale, float FixedHeightScale, out int Width, out int Height ) {
            const int MinWidth = 2, MinHeight = 2;
            switch (Mode) {
                case ScaleMode.Relative:
                    Width  = Mathf.Max(Mathf.RoundToInt(ScreenWidth  * RelativeScale), MinWidth);
                    Height = Mathf.Max(Mathf.RoundToInt(ScreenHeight * RelativeScale), MinHeight);
                    break;
                case ScaleMode.FixedWidth:
                    Width  = Mathf.Max(Mathf.RoundToInt(FixedWidthScale), MinWidth);
                    Height = Mathf.Max(Mathf.RoundToInt(ScreenHeight * (FixedWidthScale / ScreenWidth)), MinHeight);
                    break;
                case ScaleMode.FixedHeight:
                    Width  = Mathf.Max(Mathf.RoundToInt(ScreenWidth * (FixedHeightScale / ScreenHeight)), MinWidth);
                    Height = Mathf.Max(Mathf.RoundToInt(FixedHeightScale), MinHeight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null);
            }
        }

        void OnEnable() {
            UpdateView();
            UpdateCameras();
        }

        static void GetScreenSize( out int Width, out int Height ) {
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                Resolution Res = Screen.currentResolution;
                Width  = Res.width;
                Height = Res.height;
                return;
            }
            #endif
            Width  = Screen.width;
            Height = Screen.height;
        }

        [MemberNotNull(nameof(_Target))]
        void UpdateView() {
            GetScreenSize(out int ScreenWidth, out int ScreenHeight);
            GetSize(ScreenWidth, ScreenHeight, _Mode, _RelativeScale, _FixedWidthScale, _FixedHeightScale, out int Width, out int Height);

            if (_Target == null || _Target.width != Width || _Target.height != Height) {
                if (_Target != null) {
                    _Target.Release();
                }

                _Target = new(Width, Height, depth: 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp
                };
            }

            Shader.SetGlobalTexture(_ViewScaler, _Target);
            Shader.SetGlobalVector(_ViewScalerSize, new(Width, Height, 1f / Width, 1f / Height));
        }

        void UpdateCameras( bool Silent = false ) {
            if (_InputCamera == null) {
                if (!Silent) {
                    Debug.LogError("Input camera is null.", this);
                }

                return;
            }

            _InputCamera.targetTexture =  _Target;
            _InputCamera.cullingMask   &= ~(1 << _OutputLayer);

            if (_OutputImage == null) {
                if (!Silent) {
                    Debug.LogError("Output image is null.", this);
                }

                return;
            }
            _OutputImage.texture = _Target == null ? throw new InvalidOperationException("Target is null.") : _Target;
        }

        void OnValidate() {
            UpdateView();
            UpdateCameras();
        }

        /// <summary> Repaints the view. </summary>
        public void Repaint() {
            UpdateView();
            UpdateCameras();
        }

    }
}
