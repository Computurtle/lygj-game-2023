using System;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace LYGJ.Common.Datatypes {
    internal static class TimeSpanDrawerHelpers {
        static readonly ResettableLazy<GUIContent>
            _H        = new(() => new("H", "The number of hours in the time interval.")),
            _M        = new(() => new("M", "The number of minutes in the time interval.")),
            _S        = new(() => new("S", "The number of seconds in the time interval.")),
            _Ms       = new(() => new("MS", "The number of milliseconds in the time interval.")),
            _Ticks    = new(() => new("Ticks", "The number of ticks in the time interval.")),
            _TicksBtn = new(() => new(EditorIcons.Timer.Raw, "Toggle between ticks and time interval.")); //,
            // _EditBtn  = new(() => new(EditorIcons.Pen.Raw, "Edit the time interval."));

        static readonly ResettableLazy<GUIStyle>
            // _ToggleLft = new(() => new(EditorStyles.miniButtonLeft) {
            //     fixedWidth = 20f
            // }),
            _ToggleRgt = new(() => new(EditorStyles.miniButtonRight) {
                fixedWidth = 20f
            });

        [ExecuteOnReload]
        static void Cleanup() {
            _H.Reset();
            _M.Reset();
            _S.Reset();
            _Ms.Reset();
            _Ticks.Reset();

            _TicksBtn.Reset();
            // _EditBtn.Reset();

            // _ToggleLft.Reset();
            _ToggleRgt.Reset();
        }

        const float _BtnWidth      = 20f;
        const float _UnitWidth     = 20f;
        const float _TicksLblWidth = 40f;

        const float _Padding = 2f;

        public static string GetTooltip( TimeSpan TimeSpan ) {
            // Tooltip will be:
            // 1h 2m 3s 4ms
            // [123,456,789 ticks]

            StringBuilder SB = new();
            void AppendUnit( string Unit, int Value, ref bool Chain ) {
                if (!Chain && Value == 0) {
                    return;
                }

                if (SB.Length != 0) {
                    SB.Append(' ');
                }

                SB.Append(Value);
                SB.Append(Unit);
                Chain = true;
            }

            bool Chain = false;
            AppendUnit("h", TimeSpan.Hours, ref Chain);
            AppendUnit("m", TimeSpan.Minutes, ref Chain);
            AppendUnit("s", TimeSpan.Seconds, ref Chain);
            AppendUnit("ms", TimeSpan.Milliseconds, ref Chain);
            if (!Chain) {
                SB.Append("0ms");
            }

            SB.AppendLine()
                .Append('[')
                .Append(TimeSpan.Ticks)
                .Append(" ticks]");

            return SB.ToString();
        }

        public static GUIContent DecorateLabel( GUIContent Label, TimeSpan Value ) => string.IsNullOrEmpty(Label.tooltip) ? new(Label.text, Label.image, GetTooltip(Value)) : Label;

        public static TimeSpan DrawUnits( Rect Fld, TimeSpan Value, out bool Changed ) {
            const float
                HPercent  = 0.2f,
                MPercent  = 0.2f,
                SPercent  = 0.2f,
                MsPercent = 0.4f;
            float       AvailWidth = Fld.width - _BtnWidth - _Padding;

            Rect HRect  = new(Fld.x, Fld.y, AvailWidth                * HPercent, Fld.height);
            Rect MRect  = new(HRect.xMax + _Padding, Fld.y, AvailWidth * MPercent, Fld.height);
            Rect SRect  = new(MRect.xMax + _Padding, Fld.y, AvailWidth * SPercent, Fld.height);
            Rect MsRect = new(SRect.xMax + _Padding, Fld.y, AvailWidth * MsPercent, Fld.height);

            float OldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = _UnitWidth;

            int OldHours, OldMinutes, OldSeconds, OldMilliseconds;
            int NewHours        = EditorGUI.IntField(HRect, _H.Value, OldHours = Value.Hours);
            int NewMinutes      = EditorGUI.IntField(MRect, _M.Value, OldMinutes = Value.Minutes);
            int NewSeconds      = EditorGUI.IntField(SRect, _S.Value, OldSeconds = Value.Seconds);
            int NewMilliseconds = EditorGUI.IntField(MsRect, _Ms.Value, OldMilliseconds = Value.Milliseconds);

            EditorGUIUtility.labelWidth = OldLabelWidth;

            if (NewHours != OldHours || NewMinutes != OldMinutes || NewSeconds != OldSeconds || NewMilliseconds != OldMilliseconds) {
                Changed = true;
                return new(hours: NewHours, minutes: NewMinutes, seconds: NewSeconds, milliseconds: NewMilliseconds, days: 0);
            }

            Changed = false;
            return Value;
        }

        public static TimeSpan DrawTicks( Rect Fld, TimeSpan Value, out bool Changed ) {
            float OldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = _TicksLblWidth;

            long OldTicks = Value.Ticks;
            long NewTicks = EditorGUI.LongField(new(Fld.x, Fld.y, Fld.width - _BtnWidth, Fld.height), _Ticks.Value, OldTicks);

            EditorGUIUtility.labelWidth = OldLabelWidth;

            if (OldTicks != NewTicks) {
                Changed = true;
                return new(ticks: NewTicks);
            }

            Changed = false;
            return Value;
        }

        public static TimeSpan DrawTimeSpan( Rect Fld, TimeSpan Value, ref bool TickMode/*, ref bool Editing*/, out bool Changed ) {
            // Label [ ticks       ] [⏰][✏️]
            // or
            // Label H [ hours ] M [ minutes ] S [ seconds ] MS [ milliseconds ] [⏰][✏️]

            // Rect EditBtnRect = new(Fld.xMax - _BtnWidth, Fld.y, _BtnWidth, Fld.height);
            // Rect TicksBtnRect = new(EditBtnRect.x - _BtnWidth, Fld.y, _BtnWidth, Fld.height);
            Rect TicksBtnRect = new(Fld.xMax - _BtnWidth, Fld.y, _BtnWidth, Fld.height);
            Rect ValueRect = new(Fld.x, Fld.y, TicksBtnRect.x - Fld.x, Fld.height);

            Value = TickMode
                ? DrawTicks(ValueRect, Value, out Changed)
                : DrawUnits(ValueRect, Value, out Changed);

            // if (GUI.Button(EditBtnRect, _EditBtn.Value, _ToggleLft.Value)) {
            //     Editing = true;
            // }

            if (GUI.Button(TicksBtnRect, _TicksBtn.Value, _ToggleRgt.Value)) {
                TickMode = !TickMode;
            }

            return Value;
        }

        public static void ForceApplyChanges( InspectorProperty Prop ) {
            InspectorProperty? Parent = Prop.ParentValueProperty;
            Object?            Target = null;

            while (Parent is not null) {
                if (Parent.ValueEntry != null) {
                    if (Parent.ValueEntry.WeakSmartValue is Object Obj) {
                        Target = Obj;
                    }
                    break;
                }

                Parent = Parent.ParentValueProperty;
            }

            if (Target is not null) {
                EditorUtility.SetDirty(Target);
                Debug.Log($"Forcefully applied changes to {Prop.Path}.");
            } else {
                Debug.LogWarning($"Failed to apply changes to {Prop.Path}.");
            }
        }
    }

    [UsedImplicitly]
    public sealed class UniTimeSpanDrawer : OdinValueDrawer<UniTimeSpan> {

        bool /*_Editing = false, */_TickMode = false;

        #region Overrides of OdinValueDrawer<UniTimeSpan>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            Rect Fld = EditorGUILayout.GetControlRect();
            if (Lbl is not null) {
                Fld = EditorGUI.PrefixLabel(Fld, Lbl);
            }

            TimeSpan Old = ValueEntry.SmartValue.TimeSpan;
            TimeSpan New = TimeSpanDrawerHelpers.DrawTimeSpan(Fld, Old, ref _TickMode, /*ref _Editing,*/ out bool Changed);

            if (Changed) {
                ValueEntry.SmartValue = new(New);
                if (!ValueEntry.ApplyChanges()) {
                    Debug.LogWarning($"Failed to apply changes to {ValueEntry.Property.Path}. Attempting forceful save.");
                    TimeSpanDrawerHelpers.ForceApplyChanges(ValueEntry.Property);
                }
            }
        }

        #endregion

    }

    [UsedImplicitly]
    public sealed class TimeSpanDrawer : OdinValueDrawer<TimeSpan> {

        bool /*_Editing = false,*/ _TickMode = false;

        #region Overrides of OdinValueDrawer<TimeSpan>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            TimeSpan Old = ValueEntry.SmartValue;

            Rect     Fld = EditorGUILayout.GetControlRect();
            if (Lbl is not null) {
                Fld = EditorGUI.PrefixLabel(Fld, TimeSpanDrawerHelpers.DecorateLabel(Lbl, Old));
            }

            TimeSpan New = TimeSpanDrawerHelpers.DrawTimeSpan(Fld, Old, ref _TickMode, /*ref _Editing,*/ out bool Changed);

            if (Changed) {
                ValueEntry.SmartValue = New;
                ValueEntry.ApplyChanges();
            }
        }

        #endregion

    }
}
