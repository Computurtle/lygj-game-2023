using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LYGJ.DialogueSystem {
    public sealed class DialogueChain : ScriptableObject, IReadOnlyList<DialogueObject> {
        [SerializeField, SerializeReference] DialogueObject[] _Objects = Array.Empty<DialogueObject>();

        /// <summary> The objects in the dialogue chain. </summary>
        #if UNITY_EDITOR
        public DialogueObject[] Objects {
            get => _Objects;
            set => _Objects = value;
        }
        #else
        public IReadOnlyList<DialogueObject> Objects => _Objects;
        #endif

        /// <inheritdoc cref="List{T}.IndexOf(T)"/>
        public int IndexOf( DialogueObject Object ) => Array.IndexOf(_Objects, Object);

        /// <inheritdoc cref="List{T}.IndexOf(T)"/>
        public int IndexOf( Func<DialogueObject, bool> Predicate ) {
            int Ln = _Objects.Length;
            for (int I = 0; I < Ln; I++) {
                if (Predicate(_Objects[I])) {
                    return I;
                }
            }
            return -1;
        }

        /// <summary> Gets the index of the first object with the specified label. </summary>
        /// <param name="Label"> The label to search for. </param>
        /// <param name="Index"> The index of the label, or <c>-1</c> if the label was not found. </param>
        /// <param name="Comparison"> The string comparison to use. </param>
        /// <returns> <see langword="true"/> if the label was found, <see langword="false"/> otherwise. </returns>
        public bool TryGetLabel( string Label, out int Index, StringComparison Comparison = StringComparison.OrdinalIgnoreCase ) {
            int Ln = _Objects.Length;
            for (int I = 0; I < Ln; I++) {
                if (_Objects[I] is DialogueLabel Lbl && string.Equals(Lbl.Name, Label, Comparison)) {
                    Index = I;
                    return true;
                }
            }
            Index = -1;
            return false;
        }

        /// <inheritdoc />
        DialogueObject IReadOnlyList<DialogueObject>.this[ int Index ] => _Objects[Index];

        /// <inheritdoc cref="IReadOnlyList{T}.this"/>
        public DialogueObject? this[ int Index ] => Index >= 0 && Index < _Objects.Length ? _Objects[Index] : null;

        /// <inheritdoc />
        public int Count => _Objects.Length;

        /// <summary> Gets the first object in the chain. </summary>
        /// <returns> The first object in the chain, or <see langword="null"/> if the chain is empty. </returns>
        public DialogueObject? First => _Objects.Length > 0 ? _Objects[0] : null;

        /// <summary> Gets the index of the first object in the chain. </summary>
        /// <returns> The index of the first object in the chain, or <c>-1</c> if the chain is empty. </returns>
        public int FirstIndex => _Objects.Length > 0 ? 0 : -1;

        #region Implementation of IEnumerable

        /// <inheritdoc />
        public IEnumerator<DialogueObject> GetEnumerator() => _Objects.GetStrongEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _Objects.GetWeakEnumerator();

        #endregion

        /// <inheritdoc cref="Dialogue.Display(DialogueChain)"/>
        public UniTask<int> Play() => Dialogue.Display(this);

        #if UNITY_EDITOR
        [Button("Play"), HideInEditorMode, ContextMenu("Play")]
        void Editor_Play() {
            async UniTask Perform() {
                int Exit = await Dialogue.Display(this);
                Debug.Log($"Dialogue ended with exit code {Exit}.", this);
            }
            Perform().Forget(Debug.LogException);
        }
        [ContextMenu("Play", true)]
        bool Editor_Play_Validate() => Application.isPlaying;

        [MenuItem("Assets/Create/LYGJ/Dialogue/Dialogue Chain", priority = 100)] // Uses ProjectWindowUtil.CreateAsset to let the user choose the name, then deletes the proxy file, creates a new .diag file, and opens it in the user's default editor.
        static void Editor_Create() {
            // 1. Determine directory in Project browser.
            string Path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(Path)) {
                Path = "Assets";
            } else {
                int Period = Path.LastIndexOf('.');
                int Slash  = Path.LastIndexOf('/');
                if (Period > Slash) {
                    Path = Path[..Slash];
                }
            }

            // 2. Create proxy file.
            DialogueChain Proxy = CreateInstance<DialogueChain>();
            ProjectWindowUtil.CreateAsset(Proxy, $"{Path}/New Dialogue Chain.asset");

            async UniTask Perform() {
                // 3. Wait until finished renaming
                string ProxyPath = string.Empty;
                bool Predicate() => string.IsNullOrEmpty(ProxyPath = AssetDatabase.GetAssetPath(Proxy));
                await UniTask.WaitWhile(Predicate);

                // 5. Delete proxy file.
                System.IO.File.Delete(ProxyPath);
                System.IO.File.Delete($"{ProxyPath}.meta");

                // 6. Create .diag file.
                string DiagPath = $"{ProxyPath[..^6]}.diag";
                await System.IO.File.WriteAllTextAsync(DiagPath, $"npc: \"Hello, world!\"{Environment.NewLine}exit{Environment.NewLine}");

                // 7. Import .diag file.
                AssetDatabase.Refresh();
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<DialogueChain>(DiagPath));
            }
            Perform().Forget(Debug.LogException);
        }
        #endif

    }

    [Serializable]
    public abstract class DialogueObject { // TODO: Way to determine next object (i.e. if jump is used; default to sequential?)

        /// <summary> Displays the dialogue object. </summary>
        /// <param name="Chain"> The chain that is being displayed. </param>
        /// <returns> The instruction to follow after displaying the object. </returns>
        public abstract UniTask<DialogueInstruction> Display( DialogueChain Chain );
    }

    [Serializable]
    public sealed class SpokenDialogue : DialogueObject {
        [SerializeField]
        public string Name = string.Empty;

        [SerializeField, ToggleLeft, LabelText("Known")]
        public bool SpeakerKnown = true;

        [SerializeField, ListDrawerSettings(DefaultExpandedState = true)]
        public string[] Text = Array.Empty<string>(); // TODO: Dialogue script uses TextExcerpt.Split() on each string when displaying.

        // TODO: Speed function (calls Dialogue.Speed = x)
        //       ${speed 5}, ${speed * 5}, {speed clear}, etc.

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override async UniTask<DialogueInstruction> Display( DialogueChain Chain ) {
            int Ln = Text.Length;
            for (int I = 0; I < Ln; I++) {
                string Line = Text[I];

                await Dialogue.ClearText(this, Name);
                Dialogue.CurrentSpeaker = Name;
                await Dialogue.DisplayText(this, Name, SpeakerKnown, Line);

                if (I < Ln - 1) {
                    await Dialogue.WaitForInput();
                }
                Dialogue.CurrentSpeaker = null;
            }

            return DialogueInstruction.Continue(PauseForInput: true);
        }

        #endregion

        public SpokenDialogue() { }
        public SpokenDialogue( string Name, bool SpeakerKnown, params string[] Text ) {
            this.Name      = Name;
            this.SpeakerKnown = SpeakerKnown;
            this.Text      = Text;
        }
    }

    [Serializable]
    public sealed class DialogueExit : DialogueObject {
        [SerializeField] public int ExitCode = 0;

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override UniTask<DialogueInstruction> Display( DialogueChain Chain ) => UniTask.FromResult(DialogueInstruction.Exit(ExitCode));

        #endregion

        public DialogueExit() { }
        public DialogueExit( int ExitCode ) => this.ExitCode = ExitCode;

    }

    [Serializable]
    public sealed class DialogueJump : DialogueObject {
        [SerializeField] public string Label = string.Empty;

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override UniTask<DialogueInstruction> Display( DialogueChain Chain ) {
            if (Chain.TryGetLabel(Label, out int Index)) {
                return UniTask.FromResult(DialogueInstruction.Goto(Index));
            }

            Debug.LogWarning($"Dialogue jump returned a label '{Label}', but it was not found in the chain.", Chain);
            return UniTask.FromResult(DialogueInstruction.Continue());
        }

        #endregion

        public DialogueJump() { }
        public DialogueJump( string Label ) => this.Label = Label;
    }

    [Serializable]
    public sealed class DialogueLabel : DialogueObject {
        [SerializeField] public string Name = string.Empty;

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override UniTask<DialogueInstruction> Display( DialogueChain Chain ) => UniTask.FromResult(DialogueInstruction.Continue());

        #endregion

        public DialogueLabel() { }
        public DialogueLabel( string Name ) => this.Name = Name;
    }

    [Serializable]
    public class DialogueMethodInvocation : DialogueObject {
        [SerializeField]                                                  public string   MethodName = string.Empty;
        [SerializeField, ListDrawerSettings(DefaultExpandedState = true)] public string[] Args       = Array.Empty<string>();

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override UniTask<DialogueInstruction> Display( DialogueChain Chain ) {
            string Result = Dialogue.Methods.Invoke(MethodName, Args);
            if (Chain.TryGetLabel(Result, out int Index)) {
                return UniTask.FromResult(DialogueInstruction.Goto(Index));
            }

            if (!string.IsNullOrEmpty(Result)) {
                Debug.LogWarning($"Dialogue method '{MethodName}' returned a value ('{Result}'), but it was not used.", Chain);
            }
            return UniTask.FromResult(DialogueInstruction.Continue());
        }

        #endregion

        public DialogueMethodInvocation() { }
        public DialogueMethodInvocation( string MethodName, params string[] Args ) {
            this.MethodName = MethodName;
            this.Args       = Args;
        }
    }

    [Serializable]
    public sealed class DialogueJumpMethodInvocation : DialogueMethodInvocation {

        #region Overrides of DialogueMethodInvocation

        /// <inheritdoc />
        public override UniTask<DialogueInstruction> Display( DialogueChain Chain ) {
            string Result = Dialogue.Methods.Invoke(MethodName, Args);
            if (Chain.TryGetLabel(Result, out int Index)) {
                return UniTask.FromResult(DialogueInstruction.Goto(Index));
            }

            Debug.LogWarning($"Dialogue method '{MethodName}' returned a label '{Result}', but it was not found in the chain.", Chain);
            return UniTask.FromResult(DialogueInstruction.Continue());
        }

        #endregion

        public DialogueJumpMethodInvocation() { }
        public DialogueJumpMethodInvocation( string MethodName, params string[] Args ) : base(MethodName, Args) { }

    }

    [Serializable]
    public sealed class DialogueChoice : DialogueObject {
        [SerializeField, ListDrawerSettings(DefaultExpandedState = true)] public DialogueChoiceOption[] Options = Array.Empty<DialogueChoiceOption>();

        #region Overrides of DialogueObject

        /// <inheritdoc />
        public override async UniTask<DialogueInstruction> Display( DialogueChain Chain ) {
            DialogueChoiceOption Chosen = await Dialogue.DisplayChoices(Options);
            if (Chain.TryGetLabel(Chosen.Label, out int Index)) {
                return DialogueInstruction.Goto(Index);
            }

            Debug.LogWarning($"Dialogue choice returned a label '{Chosen.Label}', but it was not found in the chain.", Chain);
            return DialogueInstruction.Continue();
        }

        #endregion

        public DialogueChoice() { }
        public DialogueChoice( params DialogueChoiceOption[] Options ) => this.Options = Options;
    }

    [Serializable]
    public sealed class DialogueChoiceOption {
        [SerializeField] public string Text  = string.Empty;
        [SerializeField] public string Label = string.Empty;

        public DialogueChoiceOption() { }
        public DialogueChoiceOption( string Text, string Label ) {
            this.Text  = Text;
            this.Label = Label;
        }

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString() => Text;

        #endregion

    }

    public enum DialogueInstructionType {
        Continue,
        Goto,
        Exit
    }

    public readonly struct DialogueInstruction {

        /// <summary> The type of instruction. </summary>
        public readonly DialogueInstructionType Type;

        /// <summary> The index to go to. </summary>
        public readonly int Index;

        /// <summary> The end code to exit with. </summary>
        public int ExitCode => Index;

        /// <summary> Whether to pause for user input before performing the instruction. </summary>
        public readonly bool PauseForInput;

        DialogueInstruction( DialogueInstructionType Type, int Index = -1, bool PauseForInput = false ) {
            this.Type  = Type;
            this.Index = Index;
            this.PauseForInput = PauseForInput;
        }

        /// <summary> Creates a new <see cref="DialogueInstruction"/> to continue onto the next node. </summary>
        /// <param name="PauseForInput"> Whether to pause for user input before performing the instruction. </param>
        /// <returns> A new <see cref="DialogueInstruction"/> to continue onto the next node. </returns>
        public static DialogueInstruction Continue( bool PauseForInput = false ) => new(DialogueInstructionType.Continue, -1, PauseForInput);

        /// <summary> Creates a new <see cref="DialogueInstruction"/> to jump to the given index. </summary>
        /// <param name="Index"> The index to jump to. </param>
        /// <param name="PauseForInput"> Whether to pause for user input before performing the instruction. </param>
        /// <returns> A new <see cref="DialogueInstruction"/> to jump to the given index. </returns>
        public static DialogueInstruction Goto( int Index, bool PauseForInput = false ) => new(DialogueInstructionType.Goto, Index, PauseForInput);

        /// <summary> Creates a new <see cref="DialogueInstruction"/> to exit with the given end code. </summary>
        /// <param name="ExitCode"> The end code to exit with. </param>
        /// <returns> A new <see cref="DialogueInstruction"/> to exit with the given end code. </returns>
        public static DialogueInstruction Exit( int ExitCode ) => new(DialogueInstructionType.Exit, ExitCode, false);
    }

    public readonly struct MethodCallback {
        /// <summary> The name of the method to invoke. </summary>
        public readonly string MethodName;

        /// <summary> The arguments to pass to the method. </summary>
        public readonly string[] Args;

        /// <summary> Invokes the method. </summary>
        /// <returns> The result of the method. </returns>
        public string Invoke() {
            if (string.IsNullOrEmpty(MethodName)) {
                return string.Empty;
            }

            return Dialogue.Methods.Invoke(MethodName, Args);
        }

        MethodCallback( string MethodName, string[] Args ) {
            this.MethodName = MethodName;
            this.Args       = Args;
        }

        MethodCallback( string MethodName ) : this(MethodName, Array.Empty<string>()) { }

        /// <summary> Gets a <see cref="MethodCallback"/> with no method name or arguments. </summary>
        public static MethodCallback None => new(string.Empty);

        /// <summary> Gets a <see cref="MethodCallback"/> from the given text. </summary>
        /// <param name="Text"> The text to get the callback from. </param>
        /// <returns> The <see cref="MethodCallback"/> from the given text. </returns>
        public static MethodCallback GetCallback( in ReadOnlySpan<char> Text ) {
            Debug.Log($"Getting callback from '{Text.ToString()}'");
            // Text will be as such: "methodName arg1 arg2 arg3"
            int Ln = Text.Length;
            if (Ln == 0) {
                Debug.LogWarning("Dialogue method callback is empty.");
                return None;
            }

            int           Segment    = 0; // 0 = MethodName, 1 = Args
            string        MethodName = string.Empty;
            StringBuilder SB         = new();
            List<string>  Args       = new();
            bool          Verbatim   = false, InQuotes = false;

            // Smart arg parsing: Split on spaces, unless in double quotes. Verbatim always takes precedence regardless of scope.
            for (int I = 0; I < Ln; I++) {
                char C = Text[I];
                if (Verbatim) {
                    SB.Append(C);
                    Verbatim = false;
                    continue;
                }

                switch (C) {
                    case '\\':
                        Verbatim = true;
                        break;
                    case '"':
                        InQuotes = !InQuotes;
                        break;
                    case ' ' when !InQuotes:
                        if (Segment == 0) {
                            MethodName = SB.ToString();
                            SB.Clear();
                            Segment++;
                        } else {
                            Args.Add(SB.ToString());
                            SB.Clear();
                        }

                        break;
                    default:
                        SB.Append(C);
                        break;
                }
            }

            if (SB.Length > 0) {
                if (Segment == 0) {
                    MethodName = SB.ToString();
                } else {
                    Args.Add(SB.ToString());
                }
            }

            return new(MethodName, Args.ToArray(Args.Count));
        }
    }

}
