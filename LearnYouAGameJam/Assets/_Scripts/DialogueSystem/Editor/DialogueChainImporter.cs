using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    [ScriptedImporter(1, "diag")]
    public sealed class DialogueChainImporter : ScriptedImporter {

        public enum ImportScope {
            SOL, // Start of line (whitespace until first character)
            Keyword, // "exit", etc. and also speaker if we reach a ':'
            Command_Name,
            Command_Args,
            Label,
            Dialogue,
            Dialogue_Quote,
            Exit_Code,
            Jump_Label,
            Jump_Callback_Args,
            Choice_Leading,
            Choice_Text,
            Choice_PreLabel,
            Choice_Label
        }
        
        #region Overrides of ScriptedImporter

        static string Sanitise( string Line ) {
            // Trim line, remove comments, make platform-agnostic (i.e. remove \r), add \0 to end.
            // When removing comments, be careful of context (for example, don't remove '#' if it's prefaced by '\').

            // Examples:
            // "   Content! # Comment\r\n" -> "Content!\0"
            // "   # Comment\n" -> string.Empty

            StringBuilder SB  = new(Line.Trim());
            int           I   = 0;
            int           LdS = 0; // Leading spaces for comments
            while (I < SB.Length) {
                char C = SB[I];
                if (C == '\\') { // Only handle '#' escaping. Other escapes are handled by existing code.
                    if (I + 1 < SB.Length && SB[I + 1] == '#') {
                        SB.Remove(I, 1);
                    }
                }
                if (C == '#') {
                    // SB.Remove(I, SB.Length - I); // Include any leading spaces
                    SB.Remove(I - LdS, SB.Length - I + LdS);
                    break;
                }
                if (char.IsWhiteSpace(C)) {
                    LdS++;
                } else {
                    LdS = 0;
                }
                I++;
            }
            SB.Append('\0');
            string Sanitised = SB.ToString();
            // Debug.Log($"Sanitised: \"{Line}\" to {(Sanitised == "\0" ? "<null>" : $"\"{Sanitised.Replace(' ', '·')}\"")}");
            return Sanitised;
        }

        static bool IsNullOrEmpty( string Line ) => string.IsNullOrEmpty(Line) || Line == "\0";

        /// <inheritdoc />
        public override void OnImportAsset( AssetImportContext Ctx ) {
            // npc: "Stuff to say." "More stuff to say."
            //      "Even more stuff to say."
            // player: "Stuff to ${callback 7} say."
            // player: "Stuff to \$\{fake-callback\} say."
            // $command arg arg
            // @jump label2
            // @jump $callback # Method 'callback' returns string for label to jump to.
            //
            // > "First option" label1
            // > "Second option" label2
            //
            // :label
            // player: "hacker"
            // exit
            // #exit 0 <-- 0 is implied

            // For the most part, whitespace is ignored. Arguments must be separated by at least one, but can be separated by any amount.
            // In most scenarios, scope resets on newline. However, if dialogue is already being read, and the next line starts with '"' (ignoring whitespace obv.), continue reading dialogue until the next line doesn't start with '"'.

            List<DialogueObject> Objects = new();

            string[]      Lines = File.ReadAllLines(Ctx.assetPath);
            StringBuilder SB    = new();

            string                     SpeakerName  = string.Empty, CommandName = string.Empty;
            bool                       SpeakerKnown = true;
            List<string>               CommandArgs  = new(),        Dialogue    = new(); // JumpCallback uses CommandName and CommandArgs.
            string                     ChoiceText   = string.Empty, ChoiceLabel = string.Empty;
            List<DialogueChoiceOption> Choices      = new();

            int LnIdx = -1;
            foreach (string Line in Lines) {
                LnIdx++;
                ImportScope Scope = ImportScope.SOL;
                string      Ln;
                foreach (char C in Ln = Sanitise(Line)) {
                    if (IsNullOrEmpty(Ln)) { continue; }

                    if (C == '\0') {
                        switch (Scope) {
                            case ImportScope.Keyword:
                                // If it's keyword, see if it's a known parameterless keyword (i.e. "exit")
                                string Keyword = SB.ToString();
                                SB.Clear();
                                switch (Keyword.ToLowerInvariant()) {
                                    case "exit":
                                        Objects.Add(new DialogueExit(0));
                                        continue;
                                    default:
                                        throw new($"Unknown keyword '{Keyword}' (Line: {LnIdx})");
                                }
                            case ImportScope.Command_Name:
                                // If in command callback (no args)
                                CommandName = SB.ToString();
                                SB.Clear();
                                Objects.Add(new DialogueMethodInvocation(CommandName));
                                continue;
                            case ImportScope.Command_Args:
                                // If in command args, add command to list.
                                CommandArgs.Add(SB.ToString());
                                SB.Clear();
                                Objects.Add(new DialogueMethodInvocation(CommandName, CommandArgs.ToArray()));
                                CommandArgs.Clear();
                                continue;
                            case ImportScope.Label:
                                // If in label, add label to list.
                                Objects.Add(new DialogueLabel(SB.ToString()));
                                SB.Clear();
                                continue;
                            case ImportScope.Dialogue:
                                // If in dialogue, add dialogue to list.
                                if (Dialogue.Count == 0 || Dialogue.Count == 1 && string.IsNullOrEmpty(Dialogue[0])) {
                                    Debug.LogWarning($"Dialogue line {LnIdx} is empty. Did you mean a label (:label)?");
                                }
                                Objects.Add(new SpokenDialogue(SpeakerName, SpeakerKnown, Dialogue.ToArray()));
                                Dialogue.Clear();
                                SpeakerKnown = true;
                                continue;
                            case ImportScope.Exit_Code:
                                // If in exit code, add exit code to list.
                                Objects.Add(new DialogueExit(int.Parse(SB.ToString())));
                                SB.Clear();
                                continue;
                            case ImportScope.Jump_Label:
                                // If in jump callback (no args)
                                bool IsMethod = SB[0] == '$';
                                if (IsMethod) {
                                    SB.Remove(0, 1);
                                }
                                CommandName = SB.ToString();
                                SB.Clear();
                                Objects.Add(IsMethod ? new DialogueJumpMethodInvocation(CommandName) : new DialogueJump(CommandName));
                                continue;
                            case ImportScope.Jump_Callback_Args:
                                // If in jump callback args, add callback to list.
                                CommandArgs.Add(SB.ToString());
                                SB.Clear();
                                Objects.Add(new DialogueJumpMethodInvocation(CommandName, CommandArgs.ToArray()));
                                CommandArgs.Clear();
                                continue;
                            case ImportScope.Choice_Label:
                                // If in choice label, add choice to list.
                                ChoiceLabel = SB.ToString();
                                SB.Clear();
                                // Debug.Log($"Adding choice '{ChoiceText}' -> '{ChoiceLabel}'");
                                Choices.Add(new(ChoiceText, ChoiceLabel));
                                continue;
                            default:
                                throw new($"Unexpected end of line in scope '{Scope}' (\"{SB}\") (Line: {LnIdx})");
                        }
                    }

                    switch (Scope) {
                        case ImportScope.SOL:
                            // Debug.Log($"New SOL ('{C}'); current choices: {Choices.Count}");
                            if (Choices.Count > 0 && C != '>') {
                                // Debug.Log($"Adding choice with {Choices.Count} options");
                                Objects.Add(new DialogueChoice(Choices.ToArray()));
                                Choices.Clear();
                            }

                            switch (C) {
                                case '$':
                                    Scope = ImportScope.Command_Name;
                                    break;
                                case '@':
                                    Debug.LogError("Obsolete: '@jump' is no longer supported. Use 'jump' instead.");
                                    Scope = ImportScope.Jump_Label;
                                    break;
                                case ':':
                                    Scope = ImportScope.Label;
                                    break;
                                case '>':
                                    // Debug.Log("Starting new choice");
                                    Scope = ImportScope.Choice_Leading;
                                    break;
                                // case '#': // Comment
                                //     continue;
                                default:
                                    Scope = ImportScope.Keyword;
                                    SB.Append(C);
                                    break;
                            }
                            break;
                        case ImportScope.Keyword:
                            switch (C) {
                                // Go until the first ' '. If we hit a ':', then we're actually a speaker name.
                                case '?' when !SpeakerKnown:
                                    throw new($"'?' is an invalid character in a speaker name (Line: {LnIdx})");
                                case '?':
                                    SpeakerKnown = false;
                                    break;
                                case ':':
                                    SpeakerName = SB.ToString();
                                    SB.Clear();
                                    Scope = ImportScope.Dialogue;
                                    break;
                                default: {
                                    if (char.IsWhiteSpace(C)) {
                                        string Keyword = SB.ToString().ToLowerInvariant();
                                        SB.Clear();
                                        Scope = Keyword switch {
                                            "exit" => ImportScope.Exit_Code,
                                            "jump" => ImportScope.Jump_Label,
                                            "goto" => ImportScope.Jump_Label, // Alias for jump
                                            _      => throw new($"Unknown keyword '{Keyword}' (Line: {LnIdx})")
                                        };
                                    } else {
                                        SB.Append(C);
                                    }

                                    break;
                                }
                            }
                            break;
                        case ImportScope.Command_Name:
                            if (char.IsWhiteSpace(C)) {
                                CommandName = SB.ToString();
                                SB.Clear();
                                Scope = ImportScope.Command_Args;
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Command_Args:
                            if (char.IsWhiteSpace(C)) {
                                CommandArgs.Add(SB.ToString());
                                SB.Clear();
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Label:
                            SB.Append(C);
                            break;
                        case ImportScope.Dialogue:
                            if (C == '"') {
                                Scope = ImportScope.Dialogue_Quote;
                            } else if (!char.IsWhiteSpace(C)) {
                                throw new($"Unexpected character '{C}' in dialogue (Line: {LnIdx})");
                            }
                            break;
                        case ImportScope.Dialogue_Quote:
                            if (C == '"') {
                                Dialogue.Add(SB.ToString());
                                SB.Clear();
                                Scope = ImportScope.Dialogue;
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Exit_Code:
                            SB.Append(C);
                            break;
                        case ImportScope.Jump_Label:
                            if (char.IsWhiteSpace(C)) {
                                CommandName = SB.ToString();
                                SB.Clear();
                                Scope = ImportScope.Jump_Callback_Args;
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Jump_Callback_Args:
                            if (char.IsWhiteSpace(C)) {
                                CommandArgs.Add(SB.ToString());
                                SB.Clear();
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Choice_Leading:
                            if (C == '"') {
                                Scope = ImportScope.Choice_Text;
                            }
                            break;
                        case ImportScope.Choice_Text:
                            if (C == '"') {
                                ChoiceText = SB.ToString();
                                SB.Clear();
                                // Debug.Log($"Choice text: '{ChoiceText}'");
                                Scope = ImportScope.Choice_PreLabel;
                            } else {
                                SB.Append(C);
                            }
                            break;
                        case ImportScope.Choice_PreLabel:
                            if (char.IsWhiteSpace(C)) { continue; }
                            Scope = ImportScope.Choice_Label;
                            goto case ImportScope.Choice_Label;
                        case ImportScope.Choice_Label:
                            SB.Append(C);
                            break;
                        default:
                            throw new($"Unexpected character '{C}' in scope '{Scope}' (Line: {LnIdx})");
                    }
                }
            }

            if (Choices.Count > 0) {
                Objects.Add(new DialogueChoice(Choices.ToArray()));
            }

            int ObjLn = Objects.Count;
            if (ObjLn > 0 && Objects[ObjLn - 1] is not DialogueExit) {
                // Debug.Log("Adding exit 0 (implied)");
                Objects.Add(new DialogueExit());
            }

            DialogueChain Chain = ScriptableObject.CreateInstance<DialogueChain>();
            Chain.Objects = Objects.ToArray();

            Ctx.AddObjectToAsset("DialogueChain", Chain);
            Ctx.SetMainObject(Chain);
        }

        #endregion

    }

    [CustomEditor(typeof(DialogueChainImporter))]
    public class DialogueChainEditor : ScriptedImporterEditor {

        async UniTask Perform() {
            DialogueChainImporter Importer = (DialogueChainImporter)target;
            DialogueChain         Chain    = AssetDatabase.LoadAssetAtPath<DialogueChain>(Importer.assetPath);

            int Exit = await Dialogue.Display(Chain);
            Debug.Log($"Dialogue ended with exit code {Exit}.", Chain);
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (Application.isPlaying) {
                using (new EditorGUI.DisabledScope(Dialogue.IsRunning)) {
                    if (GUILayout.Button("Play")) {
                        Perform().Forget(Debug.LogException);
                    }
                }
            }
        }

        #region Overrides of AssetImporterEditor

        /// <inheritdoc />
        protected override bool needsApplyRevert => false;

        #endregion

    }
}
