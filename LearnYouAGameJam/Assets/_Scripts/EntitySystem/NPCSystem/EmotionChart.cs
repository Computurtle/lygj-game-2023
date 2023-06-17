using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LYGJ.EntitySystem.NPCSystem {
    [CreateAssetMenu(menuName = "LYGJ/NPC/Emotion Definition", fileName = "New EmotionDefinition")]
    public sealed class EmotionChart : ScriptableObject {
        [Serializable] public sealed class Definition {

            /// <summary> Gets the face texture. </summary>
            [field: SerializeField, Tooltip("The face texture."), AssetsOnly, LabelText("@ToString()"), PreviewField, HorizontalGroup("A"), PropertyOrder(0)] public Texture? Texture { get; private set; } = null;

            /// <summary> Gets the animation value. </summary>
            [field: SerializeField, HideLabel, HorizontalGroup("A"), PropertyOrder(2), EnableIf(nameof(HasAnimation))]
            public Motion Animation { get; private set; } = default;

            /// <summary> Gets a value indicating whether the animation value is set. </summary>
            [field: SerializeField, Tooltip("The animation value."), LabelText("Animation"), ToggleLeft, HorizontalGroup("A"), PropertyOrder(1)]
            public bool HasAnimation = false;

            /// <summary> Gets the emotion. </summary>
            [field: SerializeField, HideInInspector]
            public Emotion Emotion { get; private set; } = default;

            public Definition() { }
            public Definition( Emotion Emotion, Texture? Texture = null, Motion Animation = default, bool HasAnimation = false ) {
                this.Emotion      = Emotion;
                this.Texture      = Texture;
                this.Animation    = Animation;
                this.HasAnimation = HasAnimation;
            }

            #region Overrides of Object

            /// <inheritdoc />
            public override string ToString() => Emotion.ToString();

            #endregion

        }

        [SerializeField, Tooltip("The emotion definitions."), ListDrawerSettings(IsReadOnly = true)] Definition[] _Definitions = Array.Empty<Definition>();

        #if UNITY_EDITOR
        void Reset() {
            int Ln = Enum<Emotion>.Count;
            _Definitions = new Definition[Ln];
            string Path = AssetDatabase.GetAssetPath(this);
            Path = Path[..Path.LastIndexOf('/')];
            // Texture[] Textures = AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Texture>().ToArray();
            Texture[] Textures = AssetDatabase.FindAssets("t:Texture", new[] { Path }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Texture>).ToArray();
            int       TexLn    = Textures.Length;
            int       I        = 0;
            foreach (Emotion Emotion in Enum<Emotion>.Values) {
                _Definitions[I] = new(Emotion, I < TexLn ? Textures[I] : null);
                I++;
            }
        }
        #endif

        /// <summary> Tries to get the definition for the given emotion. </summary>
        /// <param name="Emotion"> The emotion. </param>
        /// <param name="Definition"> The definition. </param>
        /// <returns> <see langword="true"/> if the definition was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetDefinition( Emotion Emotion, [NotNullWhen(true)] out Definition? Definition ) {
            int Idx = (int)Emotion;
            Idx--;
            if (Idx < 0 || Idx >= _Definitions.Length) {
                Definition = null;
                return false;
            }
            Definition = _Definitions[Idx];
            return true;
        }

        /// <summary> Tries to get the animation value for the given emotion. </summary>
        /// <param name="Emotion"> The emotion. </param>
        /// <param name="Value"> The animation value. </param>
        /// <returns> <see langword="true"/> if the animation value was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetAnimation( Emotion Emotion, out Motion Value ) {
            if (TryGetDefinition(Emotion, out Definition? Definition)) {
                if (Definition.HasAnimation) {
                    Value = Definition.Animation;
                    return true;
                }
            }
            Value = 0;
            return false;
        }

        /// <summary> Tries to get the face texture for the given emotion. </summary>
        /// <param name="Emotion"> The emotion. </param>
        /// <param name="Texture"> The face texture. </param>
        /// <returns> <see langword="true"/> if the face texture was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetTexture( Emotion Emotion, [NotNullWhen(true)] out Texture? Texture ) {
            if (TryGetDefinition(Emotion, out Definition? Definition)) {
                Texture = Definition.Texture;
                return Texture != null;
            }
            Texture = null;
            return false;
        }
    }
}
