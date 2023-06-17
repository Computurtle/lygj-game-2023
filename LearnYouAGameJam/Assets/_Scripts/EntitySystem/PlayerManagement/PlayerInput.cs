using System;
using System.Diagnostics;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityPlayerInput = UnityEngine.InputSystem.PlayerInput;

namespace LYGJ.EntitySystem.PlayerManagement {
    public sealed class PlayerInput : SingletonMB<PlayerInput> {

        [SerializeField, Tooltip("The player input component."), Required, SceneObjectsOnly] UnityPlayerInput _PlayerInput = null!;

        #if UNITY_EDITOR
        void Reset() => _PlayerInput = FindObjectOfType<UnityPlayerInput>();
        #endif

        InputValue[] _Inputs = Array.Empty<InputValue>();

        /// <summary> The 'move' input. </summary>
        public static InputVector2 Move { get; private set; } = null!;

        /// <summary> The 'look' input. </summary>
        public static InputVector2 Look { get; private set; } = null!;

        /// <summary> The 'crouch' input. </summary>
        public static InputButton Crouch { get; private set; } = null!;

        /// <summary> The 'fire' input. </summary>
        public static InputButton Fire { get; private set; } = null!;

        /// <summary> The 'interact' input. </summary>
        public static InputButton Interact { get; private set; } = null!;

        /// <summary> The 'run' input. </summary>
        public static InputButton Run { get; private set; } = null!;

        /// <summary> The 'jump' input. </summary>
        public static InputButton Jump { get; private set; } = null!;

        /// <summary> The 'mouse position' input. </summary>
        public static InputVector2 MousePosition { get; private set; } = null!;

        /// <summary> The 'pause' input. </summary>
        public static InputButton Pause { get; private set; } = null!;

        /// <summary> The 'rotate item' input. </summary>
        public static InputFloat RotateItem { get; private set; } = null!;

        /// <summary> The 'dialogue continue' input. </summary>
        public static InputButton DialogueContinue { get; private set; } = null!;

        /// <summary> The 'switch camera' input. </summary>
        public static InputButton SwitchCamera { get; private set; } = null!;

        /// <summary> The 'inventory' input. </summary>
        public static InputButton Inventory { get; private set; } = null!;

        protected override void Awake() {
            base.Awake();
            InputActionAsset Asset = _PlayerInput.actions;
            _Inputs = new InputValue[] {
                Move             = new(Asset.FindAction("Move", true)),
                Look             = new(Asset.FindAction("Look", true)),
                Crouch           = new(Asset.FindAction("Crouch", true)),
                Fire             = new(Asset.FindAction("Fire", true)),
                Interact         = new(Asset.FindAction("Interact", true)),
                Run              = new(Asset.FindAction("Run", true)),
                Jump             = new(Asset.FindAction("Jump", true)),
                MousePosition    = new(Asset.FindAction("Mouse Position", true)),
                Pause            = new(Asset.FindAction("Pause", true)),
                RotateItem       = new(Asset.FindAction("Rotate Item", true)),
                DialogueContinue = new(Asset.FindAction("Dialogue Continue", true)),
                SwitchCamera     = new(Asset.FindAction("Switch Camera", true)),
                Inventory        = new(Asset.FindAction("Inventory", true)),
            };
            Asset.Enable();
        }

        void Update() {
            foreach (InputValue Input in _Inputs) {
                Input.Update();
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            foreach (InputValue Input in _Inputs) {
                Input.Dispose();
            }

            _Inputs = Array.Empty<InputValue>();
            if (_PlayerInput != null) {
                _PlayerInput.actions.Disable();
            }
        }

    }

    public abstract class InputValue : IDisposable {

        /// <summary> Performs per-frame updates. </summary>
        public virtual void Update() { }

        #region IDisposable

        /// <inheritdoc />
        public virtual void Dispose() { }

        #endregion

        #if UNITY_EDITOR
        protected const float InputIgnorance = 0.2f;
        #endif
    }

    public abstract class InputValue<T> : InputValue where T : struct {
        // ReSharper disable once InconsistentNaming
        protected T _Value;

        /// <summary> Gets whether the two given values are equivalent. </summary>
        /// <param name="Left"> The left operand. </param>
        /// <param name="Right"> The right operand. </param>
        /// <returns> <see langword="true"/> if the two given values are equivalent; otherwise, <see langword="false"/>. </returns>
        protected abstract bool Equals( T Left, T Right );

        public delegate void ChangedEventHandler( T OldValue, T NewValue );

        /// <summary> Raised when the value of this input changes. </summary>
        public event ChangedEventHandler? Changed;

        /// <summary> Gets or sets the value. </summary>
        public virtual T Value {
            get => _Value;
            set {
                #if UNITY_EDITOR
                if (Application.isPlaying && Time.timeSinceLevelLoad < InputIgnorance) {
                    // Debug.Log("Ignoring input due to level load.");
                    return;
                }
                #endif
                if (Equals(value, _Value)) { return; }

                T OldValue = _Value;
                _Value = value;
                RaiseChanged(OldValue, value);
            }
        }

        /// <summary> Raises the <see cref="Changed"/> event. </summary>
        /// <param name="OldValue"> The old value. </param>
        /// <param name="NewValue"> The new value. </param>
        protected void RaiseChanged( T OldValue, T NewValue ) => Changed?.Invoke(OldValue, NewValue);

        protected readonly InputAction Action;

        protected InputValue( InputAction Action, T Initial = default! ) {
            this.Action      =  Action;
            _Value           =  Initial;
            Action.performed += OnPerformed;
            Action.canceled  += OnCancelled;
        }

        #region Overrides of InputValue

        /// <inheritdoc />
        public override void Dispose() {
            base.Dispose();
            Action.performed -= OnPerformed;
            Action.canceled  -= OnCancelled;
        }

        #endregion

        protected virtual void OnPerformed( InputAction.CallbackContext Context ) => Value = Context.ReadValue<T>();
        protected virtual void OnCancelled( InputAction.CallbackContext Context ) => Value = default!;
    }

    public sealed class InputVector2 : InputValue<Vector2> {

        /// <inheritdoc />
        public InputVector2( InputAction Action, Vector2 Initial = default ) : base(Action, Initial) { }

        #region Overrides of InputValue<Vector2>

        /// <inheritdoc />
        protected override bool Equals( Vector2 Left, Vector2 Right ) => Left == Right;

        #endregion

    }

    public sealed class InputButton : InputValue<bool> {

        public delegate void PressedEventHandler();

        /// <summary> Raised when the button is pressed. </summary>
        public event PressedEventHandler? Pressed;

        public delegate void ReleasedEventHandler( float Duration );

        /// <summary> Raised when the button is released. </summary>
        public event ReleasedEventHandler? Released;

        public delegate void HeldEventHandler( float Duration );

        /// <summary> Raised on Update() whilst the button is still being held. </summary>
        public event HeldEventHandler? Held;

        float _HoldTime;

        #region Overrides of InputValue<bool>

        /// <inheritdoc />
        public override bool Value {
            get => _Value;
            set {
                if (value == _Value) { return; }

                bool OldValue = _Value;
                _Value = value;
                if (value) {
                    _HoldTime = 0f;
                    Pressed?.Invoke();
                } else {
                    Released?.Invoke(_HoldTime);
                    _HoldTime = 0f;
                }

                RaiseChanged(OldValue, value);
            }
        }

        /// <inheritdoc />
        public override void Update() {
            if (Value) {
                _HoldTime += Time.deltaTime;
                Held?.Invoke(_HoldTime);
            }
        }

        #endregion

        /// <summary> Gets whether the button is currently being held. </summary>
        public bool IsHeld => Value;

        /// <inheritdoc />
        public InputButton( InputAction Action, bool Initial = false ) : base(Action, Initial) { }

        /// <inheritdoc />
        protected override void OnPerformed( InputAction.CallbackContext Context ) => Value = Context.ReadValueAsButton();

        /// <inheritdoc />
        protected override void OnCancelled( InputAction.CallbackContext Context ) => Value = false;

        #region Overrides of InputValue<bool>

        /// <inheritdoc />
        protected override bool Equals( bool Left, bool Right ) => Left == Right;

        #endregion

    }

    public sealed class InputFloat : InputValue<float> {

        /// <inheritdoc />
        public InputFloat( InputAction Action, float Initial = default ) : base(Action, Initial) { }

        #region Overrides of InputValue<float>

        /// <inheritdoc />
        protected override bool Equals( float Left, float Right ) => Left == Right;

        #endregion

    }
}
