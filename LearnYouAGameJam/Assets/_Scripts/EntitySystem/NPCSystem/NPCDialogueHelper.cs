using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using LYGJ.Common;
using LYGJ.DialogueSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.EntitySystem.NPCSystem {
    public static class NPCDialogueHelper {
        static bool TryGetSpeaker( ref string[] Args, [NotNullWhen(true)] out NPCBase? NPC ) {
            // First arg may or may not be the speaker (i.e. not needed if inline method).
            if (Args.Length > 0) {
                if (NPCs.TryGet(Args[0], out NPC)) {
                    Args = Args[1..];
                    return true;
                }
            }

            if (Dialogue.CurrentSpeaker is not { } Speaker) {
                Debug.LogWarning("[Dialogue/Emote]: No current speaker!");
                NPC = null;
                return false;
            }

            if (!NPCs.TryGet(Speaker, out NPC)) {
                Debug.LogWarning($"[Dialogue/Emote]: The NPC with the name \"{Speaker}\" could not be found!");
                NPC = null;
                return false;
            }

            return true;
        }

        [DialogueFunction, UsedImplicitly]
        public static void Emote( string[] Args ) {
            if (!TryGetSpeaker(ref Args, out NPCBase? NPC)) {
                return;
            }

            switch (Args.Length) {
                case 0: {
                    // Debug.Log("[Dialogue/Emote]: Resetting emotion.");
                    NPC.ResetEmotion();
                    break;
                }
                case 1: {
                    switch (Args[0].ToLowerInvariant()) {
                        case "reset":
                        case "clear":
                            // Debug.Log("[Dialogue/Emote]: Resetting emotion.");
                            NPC.ResetEmotion();
                            return;
                        default:
                            if (!Enum<Emotion>.TryParse(Args[0], out Emotion Emotion)) {
                                Debug.LogWarning($"[Dialogue/Emote]: The emotion \"{Args[0]}\" could not be found!");
                                return;
                            }

                            // Debug.Log($"[Dialogue/Emote]: Setting emotion to {Emotion}.");
                            NPC.Emotion = Emotion;
                            return;
                    }
                }
                default: {
                    Debug.LogWarning($"[Dialogue/Emote]: Too many arguments! Expected 0 or 1, got {Args.Length}!\nUsage: [Emote <Emotion>], or [Emote reset]/[Emote clear]/[Emote] to reset the emotion.");
                    break;
                }
            }
        }


        [DialogueFunction, UsedImplicitly]
        public static void Motion( string[] Args ) {
            if (!TryGetSpeaker(ref Args, out NPCBase? NPC)) {
                return;
            }

            switch (Args.Length) {
                case 0: {
                    Debug.LogWarning("[Dialogue/Motion]: Too few arguments! Expected 1, got 0!\nUsage: [Motion <Motion> (<Force>)]");
                    break;
                }
                case 1: {
                    if (!Enum<Motion>.TryParse(Args[0], out Motion Motion)) {
                        Debug.LogWarning($"[Dialogue/Motion]: The motion \"{Args[0]}\" could not be found!");
                        return;
                    }

                    Motion P = NPC.Motion;
                    // Debug.Log($"[Dialogue/Motion]: Pushing motion {Motion} (then {P})");
                    NPC.PushMotion(Motion, Force: true);
                    NPC.PushMotion(P, Force: false);
                    break;
                }
                default: {
                    Debug.LogWarning($"[Dialogue/Motion]: Too many arguments! Expected 1, got {Args.Length}!\nUsage: [Motion <Motion>]");
                    break;
                }
            }
        }
    }
}
