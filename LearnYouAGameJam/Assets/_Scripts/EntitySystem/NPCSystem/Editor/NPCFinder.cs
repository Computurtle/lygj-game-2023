using System.Diagnostics;
using LYGJ.Common;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    public sealed class NPCFinder : EditorWindow {

        static readonly ResettableLazy<GUIContent>
            _Goto  = new(() => new(EditorIcons.Crosshair.Raw, "Go to the object in the scene.")),
            _Key   = new(() => new("Key", "The key used to identify the NPC.")),
            _Name  = new(() => new("Name", "The name of the NPC.")),
            _None  = new(() => new("No NPCs were found in the scene.")),
            _Title = new(() => new("NPCs", EditorIcons.MultiUser.Raw, "A list of all NPCs in the scene."));

        [ExecuteOnReload]
        static void Cleanup() {
            _Goto.Reset();
            _Key.Reset();
            _Name.Reset();
            _None.Reset();
            _Title.Reset();
        }

        const float
            _KeyWidth  = 100f,
            _GotoWidth = 20f;

        Vector2 _Scroll = Vector2.zero;

        void OnGUI() {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();
            EditorGUILayout.LabelField(_Key.Value, GUILayout.Width(_KeyWidth));
            EditorGUILayout.LabelField(_Name.Value);
            EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(_GotoWidth));
            SirenixEditorGUI.EndBoxHeader();

            _Scroll = EditorGUILayout.BeginScrollView(_Scroll);
            bool Any = false;
            foreach (NPCBase NPC in NPCs.All) {
                Any = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(NPC.Key, GUILayout.Width(_KeyWidth));
                EditorGUILayout.LabelField(NPC.name);
                if (GUILayout.Button(_Goto.Value, EditorStyles.iconButton, GUILayout.Width(_GotoWidth))) {
                    if (Event.current.shift) {
                        Selection.activeObject = NPC.gameObject;
                        Close();
                    } else {
                        EditorGUIUtility.PingObject(NPC.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (!Any) {
                EditorGUILayout.LabelField(_None.Value, EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndScrollView();

            SirenixEditorGUI.EndBox();
        }

        [MenuItem("LYGJ/NPCs")]
        static void Open() {
            NPCFinder Window = GetWindow<NPCFinder>();
            Window.titleContent = _Title.Value;
            Window.Show();
        }

    }
}
