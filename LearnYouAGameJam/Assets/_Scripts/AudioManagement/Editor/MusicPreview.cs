using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using LYGJ.Common;
using UnityEditor;
using UnityEngine;

namespace LYGJ.AudioManagement {
    [CustomPreview(typeof(Music))]
    public sealed class MusicPreview : ObjectPreview {

        static readonly ResettableLazy<(MethodInfo Play, MethodInfo Pause, MethodInfo Resume, MethodInfo StopAll, MethodInfo GetPosition, MethodInfo SetSamplePosition, MethodInfo GetSampleCount)> _PlayStop = new(
            () => {
                // public static extern void PlayPreviewClip([NotNull("NullExceptionObject")] AudioClip clip, int startSample = 0, bool loop = false)
                // public static extern void PausePreviewClip()
                // public static extern void ResumePreviewClip()
                // public static extern void StopAllPreviewClips()
                // public static extern float GetPreviewClipPosition()
                // public static extern void SetPreviewClipSamplePosition([NotNull("NullExceptionObject")] AudioClip clip, int iSamplePosition);
                // public static extern int GetSampleCount(AudioClip clip);

                Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
                return (
                    AudioUtil.GetMethod("PlayPreviewClip", BindingFlags.Public        | BindingFlags.Static),
                    AudioUtil.GetMethod("PausePreviewClip", BindingFlags.Public       | BindingFlags.Static),
                    AudioUtil.GetMethod("ResumePreviewClip", BindingFlags.Public      | BindingFlags.Static),
                    AudioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Public    | BindingFlags.Static),
                    AudioUtil.GetMethod("GetPreviewClipPosition", BindingFlags.Public | BindingFlags.Static),
                    AudioUtil.GetMethod("SetPreviewClipSamplePosition", BindingFlags.Public | BindingFlags.Static),
                    AudioUtil.GetMethod("GetSampleCount", BindingFlags.Public | BindingFlags.Static)
                );
            }
        );

        static void  PlayPreviewClip( AudioClip Clip, int StartSample = 0, bool Loop = false ) => _PlayStop.Value.Play.Invoke(null, new object[] { Clip, StartSample, Loop });
        static void  PausePreviewClip()       => _PlayStop.Value.Pause.Invoke(null, null);
        static void  ResumePreviewClip()      => _PlayStop.Value.Resume.Invoke(null, null);
        static void  StopAllPreviewClips()    => _PlayStop.Value.StopAll.Invoke(null, null);
        static float GetPreviewClipPosition() => (float)_PlayStop.Value.GetPosition.Invoke(null, null);
        static void SetPreviewClipPosition( AudioClip Clip, float Position ) {
            int SampleCount = (int)_PlayStop.Value.GetSampleCount.Invoke(null, new object[] { Clip });
            int SamplePosition = (int)(Position * SampleCount);
            _PlayStop.Value.SetSamplePosition.Invoke(null, new object[] { Clip, SamplePosition });
        }

        static bool _IsPlaying, _IsPaused;

        static readonly ResettableLazy<(GUIContent PlayButton, GUIContent PauseButton, GUIContent ResumeButton, GUIContent PreviewField, GUIContent SaveButton)> _Icons = new(
            () => (
                PlayButton: new(EditorGUIUtility.IconContent("PlayButton").image, "Play"),
                PauseButton: new(EditorGUIUtility.IconContent("PauseButton").image, "Pause"),
                ResumeButton: new(EditorGUIUtility.IconContent("PlayButton").image, "Resume"),
                PreviewField: new("Preview", "The stems to preview"),
                SaveButton: new(EditorGUIUtility.IconContent("SaveAs").image, "Save the generated audio to disk")
            )
        );

        [ExecuteOnReload]
        static void ResetState() {
            _PlayStop.Reset();
            _Icons.Reset();

            if (_IsPlaying) {
                StopAllPreviewClips();
                _IsPlaying = false;
            }
        }

        Stems _Stems = Stems.FullMix;

        (AudioClip? Clip, string Name, Texture? Waveform)? _Clip;

        static string GetName( Stems Stem ) {
            StringBuilder SB = new();
            // Flags enum. Display as 'Flag 1, Flag 2, Flag 3'
            foreach (Stems S in Enum.GetValues(typeof(Stems))) {
                switch (S) {
                    case Stems.None:
                    case Stems.FullMix:
                        continue;
                }

                if ((Stem & S) == S) {
                    if (SB.Length > 0) {
                        SB.Append(", ");
                    }

                    SB.Append(S);
                }
            }

            return SB.ToString();
        }

        static Texture2D CreateWaveformSpectrum( float[] Samples, int SampleCount, int Width, int Height, Color FG, Color BG ) {
            Texture2D Tex      = new(Width, Height, TextureFormat.RGBA32, false);
            float[]   Waveform = new float[Width];
            int       PackSize = (SampleCount / Width) + 1;
            int       S        = 0;
            for (int I = 0; I < SampleCount; I += PackSize) {
                Waveform[S] = Mathf.Abs(Samples[I]);
                S++;
            }

            for (int X = 0; X < Width; X++) {
                for (int Y = 0; Y < Height; Y++) {
                    Tex.SetPixel(X, Y, BG);
                }
            }

            for (int X = 0; X < Waveform.Length; X++) {
                for (int Y = 0; Y <= Waveform[X] * (Height * .75f); Y++) {
                    Tex.SetPixel(X, (Height / 2) + Y, FG);
                    Tex.SetPixel(X, (Height / 2) - Y, FG);
                }
            }

            Tex.Apply();
            return Tex;
        }

        int         _LastDisplayWidth;
        const float _WaveformQuality = 1f;

        const int   _WaveformHeight = 128;
        const float _Padding        = 2, _ButtonSize = 20;

        static readonly Color
            _WaveformFG = new(1f, 0.76f, 0f, 1f),
            _WaveformBG = Color.clear;

        #region Overrides of ObjectPreview

        /// <inheritdoc />
        public override bool HasPreviewGUI() => true;

        /// <inheritdoc />
        public override void OnInteractivePreviewGUI( Rect R, GUIStyle Background ) {
            Stems Old = _Stems;
            _Stems = (Stems)EditorGUI.EnumFlagsField(new(R.xMin, R.yMin, R.width, EditorGUIUtility.singleLineHeight), _Icons.Value.PreviewField, _Stems);

            int DisplayWidth = (int)R.width;
            if (_Clip is null || Old != _Stems || (DisplayWidth is < 0 or > 1 && DisplayWidth != _LastDisplayWidth)) {
                if (_IsPlaying) {
                    StopAllPreviewClips();
                }

                _LastDisplayWidth = DisplayWidth;
                Music                    Music = (Music)target;
                IReadOnlyList<AudioClip> Clips = Music.GetAudioClips(_Stems);
                Music.GetJoinedDataInfo(Clips, out int Length, out int Channels, out int Frequency);
                AudioClip? Joint = Music.JoinStreamed(Clips, string.Join(" + ", Clips.Select(C => C.name)), Length, Channels, Frequency);
                _Clip = (
                    Joint,
                    GetName(_Stems),
                    Joint == null
                        ? null
                        : CreateWaveformSpectrum(
                            Music.GetJoinedData(Clips, Length, Channels),
                            Length,
                            (int)(DisplayWidth    * _WaveformQuality),
                            (int)(_WaveformHeight * _WaveformQuality), _WaveformFG, _WaveformBG
                        )
                );

                if (_IsPlaying) {
                    PlayPreviewClip(_Clip.Value.Clip!);
                }
            }

            if (_Clip is not null) {
                bool HasClip = _Clip.Value.Clip != null;
                GUI.enabled = HasClip;
                Rect WaveformRect = new(R.xMin, R.yMin + EditorGUIUtility.singleLineHeight + _Padding, R.width, _WaveformHeight);
                if (_Clip.Value.Waveform != null) {
                    GUI.DrawTexture(WaveformRect, _Clip.Value.Waveform);
                }

                float Pos = GetPreviewClipPosition();
                if (HasClip && Pos > 0) {
                    float X = WaveformRect.xMin + (Pos / _Clip.Value.Clip!.length) * WaveformRect.width;
                    EditorGUI.DrawRect(new(X, WaveformRect.yMin, 1, WaveformRect.height), Color.white);
                }

                if (HasClip && Event.current.type == EventType.MouseDown && WaveformRect.Contains(Event.current.mousePosition)) {
                    float X = Event.current.mousePosition.x - WaveformRect.xMin;
                    float P = X / WaveformRect.width;
                    SetPreviewClipPosition(_Clip.Value.Clip!, P);
                }
            }
        }

        /// <inheritdoc />
        public override void OnPreviewSettings() {
            base.OnPreviewSettings();
            bool WasEnabled = GUI.enabled;

            if (_Clip is not null) {
                GUI.enabled = _Clip.Value.Clip != null;
                if (_IsPlaying) {
                    if (GUILayout.Button(_IsPaused ? _Icons.Value.ResumeButton : _Icons.Value.PauseButton, EditorStyles.toolbarButton, GUILayout.Width(_ButtonSize), GUILayout.Height(_ButtonSize))) {
                        if (_IsPaused) {
                            ResumePreviewClip();
                        } else {
                            PausePreviewClip();
                        }

                        _IsPaused = !_IsPaused;
                    }
                } else {
                    if (GUILayout.Button(_Icons.Value.PlayButton, EditorStyles.toolbarButton, GUILayout.Width(_ButtonSize), GUILayout.Height(_ButtonSize))) {
                        _IsPlaying = true;
                        _IsPaused  = false;
                        PlayPreviewClip(_Clip.Value.Clip!);
                    }
                }

                if (GUILayout.Button(_Icons.Value.SaveButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.MinWidth(_ButtonSize), GUILayout.Height(_ButtonSize))) {
                    string Path = EditorUtility.SaveFilePanel("Save Audio", "", _Clip.Value.Name, "wav");
                    if (!string.IsNullOrEmpty(Path)) {
                        SavWav.Save(Path, _Clip.Value.Clip!);
                    }
                }
                GUI.enabled = WasEnabled;
            }
        }

        /// <inheritdoc />
        public override void Cleanup() {
            base.Cleanup();
            StopAllPreviewClips();
            _Clip  = null;
            _Stems = Stems.FullMix;
        }

        #endregion

    }
}
