using System;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.Common.Datatypes {
    
    internal static class DateTimeDrawerHelpers {
        static readonly ResettableLazy<GUIContent>
            _Y        = new(() => new("Year", "The year component of the date represented by this instance.")),
            _M        = new(() => new("Month", "The month component of the date represented by this instance.")),
            _D        = new(() => new("Day", "The day component of the date represented by this instance.")),
            _Ticks    = new(() => new("Ticks", "The number of ticks in the date time interval.")),
            _TicksBtn = new(() => new(EditorIcons.Timer.Raw, "Toggle between ticks and date time interval.")); //,
            // _EditBtn  = new(() => new(EditorIcons.Pen.Raw, "Edit the date time interval."));

        static readonly ResettableLazy<GUIStyle>
            // _ToggleLft = new(() => new(EditorStyles.miniButtonLeft) {
            //     fixedWidth = 20f
            // }),
            _ToggleRgt = new(() => new(EditorStyles.miniButtonRight) {
                fixedWidth = 20f
            });

        [ExecuteOnReload]
        static void Cleanup() {
            _Y.Reset();
            _M.Reset();
            _D.Reset();
            _Ticks.Reset();

            _TicksBtn.Reset();
            // _EditBtn.Reset();

            // _ToggleLft.Reset();
            _ToggleRgt.Reset();
        }

        const float _BtnWidth      = 20f;
        const float _UnitWidth     = 40f;
        const float _TicksLblWidth = 40f;

        const float _Padding = 2f;

        public static string GetTooltip( DateTime DateTime ) {
            // Tooltip will be:
            // 1y 2m 3d
            // 2021/12/23 (Monday)
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
            AppendUnit("y", DateTime.Year, ref Chain);
            AppendUnit("m", DateTime.Month, ref Chain);
            AppendUnit("d", DateTime.Day, ref Chain);
            if (!Chain) {
                SB.Append("0d");
            }
            
            SB.AppendLine()
                .Append(DateTime.ToString("yyyy/MM/dd (dddd)"))
                .AppendLine()
                .Append('[')
                .Append(DateTime.Ticks)
                .Append(" ticks]");

            return SB.ToString();
        }

        public static GUIContent DecorateLabel( GUIContent Label, DateTime Value ) => string.IsNullOrEmpty(Label.tooltip) ? new(Label.text, Label.image, GetTooltip(Value)) : Label;

        public static DateTime DrawUnits( Rect Fld, DateTime Value, out bool Changed ) {
            const float
                YPercent = 0.5f,
                MPercent = 0.25f,
                DPercent = 0.25f;
            float AvailWidth = Fld.width - _BtnWidth - _Padding;

            Rect YRect = new(Fld.x, Fld.y, AvailWidth                 * YPercent, Fld.height);
            Rect MRect = new(YRect.xMax + _Padding, Fld.y, AvailWidth * MPercent, Fld.height);
            Rect DRect = new(MRect.xMax + _Padding, Fld.y, AvailWidth * DPercent, Fld.height);

            float OldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = _UnitWidth;

            int OldYear, OldMonth, OldDay;
            int NewYear  = EditorGUI.IntField(YRect, _Y.Value, OldYear = Value.Year);
            int NewMonth = EditorGUI.IntField(MRect, _M.Value, OldMonth = Value.Month);
            int NewDay   = EditorGUI.IntField(DRect, _D.Value, OldDay = Value.Day);

            EditorGUIUtility.labelWidth = OldLabelWidth;

            if (NewYear != OldYear || NewMonth != OldMonth || NewDay != OldDay) {
                Changed = true;
                return new(year: NewYear, month: NewMonth, day: NewDay, hour: Value.Hour, minute: Value.Minute, second: Value.Second, millisecond: Value.Millisecond);
            }

            Changed = false;
            return Value;
        }

        public static DateTime DrawTicks( Rect Fld, DateTime Value, out bool Changed ) {
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

        public static DateTime DrawDateTime( Rect Fld, DateTime Value, ref bool TickMode/*, ref bool Editing*/, out bool Changed ) {
            // Label [ ticks       ] [⏰][✏️]
            // or
            // Label [ year ] / [ month ] / [ day ] [⏰][✏️]

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

        public static void ForceApplyChanges( InspectorProperty Prop ) => TimeSpanDrawerHelpers.ForceApplyChanges(Prop);
    }

    [UsedImplicitly]
    public sealed class UniDateTimeDrawer : OdinValueDrawer<UniDateTime> {

        bool /*_Editing = false, */ _TickMode = false;

        #region Overrides of OdinValueDrawer<UniDateTime>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            Rect Fld = EditorGUILayout.GetControlRect();
            if (Lbl is not null) {
                Fld = EditorGUI.PrefixLabel(Fld, Lbl);
            }

            DateTime Old = ValueEntry.SmartValue.DateTime;
            DateTime New = DateTimeDrawerHelpers.DrawDateTime(Fld, Old, ref _TickMode, /*ref _Editing,*/ out bool Changed);

            if (Changed) {
                ValueEntry.SmartValue = new(New);
                if (!ValueEntry.ApplyChanges()) {
                    Debug.LogWarning($"Failed to apply changes to {ValueEntry.Property.Path}. Attempting forceful save.");
                    DateTimeDrawerHelpers.ForceApplyChanges(ValueEntry.Property);
                }
            }
        }

        #endregion

    }

    [UsedImplicitly]
    public sealed class DateTimeDrawer : OdinValueDrawer<DateTime> {

        bool /*_Editing = false,*/ _TickMode = false;

        #region Overrides of OdinValueDrawer<DateTime>

        /// <inheritdoc />
        protected override void DrawPropertyLayout( GUIContent? Lbl ) {
            DateTime Old = ValueEntry.SmartValue;

            Rect Fld = EditorGUILayout.GetControlRect();
            if (Lbl is not null) {
                Fld = EditorGUI.PrefixLabel(Fld, DateTimeDrawerHelpers.DecorateLabel(Lbl, Old));
            }

            DateTime New = DateTimeDrawerHelpers.DrawDateTime(Fld, Old, ref _TickMode, /*ref _Editing,*/ out bool Changed);

            if (Changed) {
                ValueEntry.SmartValue = New;
                ValueEntry.ApplyChanges();
            }
        }

        #endregion

    }
}
