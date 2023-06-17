using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cysharp.Threading.Tasks;
using LYGJ.Common;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace LYGJ.SceneManagement {
    public sealed class LoadingScreen : SingletonMB<LoadingScreen> {
        [SerializeField, Tooltip("The prefab for graphics in the loading screen."), AssetsOnly, Required]
        LoadingScreenGraphic _GraphicPrefab = null!;
        [SerializeField, Tooltip("The parent for graphics in the loading screen."), Required, SceneObjectsOnly]
        RectTransform _GraphicParent = null!;

        [SerializeField, Tooltip("The minimum duration of the loading screen, in seconds."), SuffixLabel("s"), MinValue(0)]
        float _MinDuration = 3f;

        [Title("Display Name")]
        [SerializeField, Tooltip("The text component for the loading scene's name."), Required, SceneObjectsOnly]
        TMP_Text _NameText = null!;
        [SerializeField, Tooltip("The format for the loading scene's name.")]
        string _NameFormat = "{0}";

        [Title("Progress")]
        [SerializeField, Tooltip("The progress bar for the loading screen."), Required, SceneObjectsOnly]
        Image _ProgressBar = null!;
        [SerializeField, Tooltip("The progress text for the loading screen."), Required, SceneObjectsOnly]
        TMP_Text _ProgressText = null!;
        [SerializeField, Tooltip("The progress text format for the loading screen.")]
        string _ProgressFormat = "{0:P0}";

        [Title("Graphics")]
        [SerializeField, Tooltip("The duration of graphics in the loading screen, in seconds."), SuffixLabel("s"), MinValue(0)]
        float _Duration = 5f;
        [SerializeField, Tooltip("The delay between graphics in the loading screen, in seconds."), SuffixLabel("s"), MinValue(0)]
        float _Delay = 0.5f;
        [SerializeField, Tooltip("The default loading screen backgrounds.")]
        Sprite[] _Backgrounds = Array.Empty<Sprite>();
        [SerializeField, Tooltip("The default loading screen tips.")]
        string[] _Tips = Array.Empty<string>();

        IReadOnlyList<Sprite>? _CurrentBGs         = null;
        readonly List<Sprite>  _CurrentBGsGrabBag  = new();
        IReadOnlyList<string>? _CurrentTips        = null;
        readonly List<string>  _CurrentTipsGrabBag = new();

        [MemberNotNullWhen(true, nameof(_CurrentBGs), nameof(_CurrentTips), nameof(_Op))]
        bool Running { get; set; } = false;

        float Progress {
            set {
                _ProgressBar.fillAmount = value;
                _ProgressText.text      = string.Format(_ProgressFormat, value);
            }
        }

        (AsyncOperation Op, Action? Callback)? _Op = null;

        /// <summary> Starts the loading screen. </summary>
        /// <param name="Info"> The loading screen info. </param>
        /// <param name="Callback"> The callback to invoke when the loading screen is finished. </param>
        public void ShowLoadingScreen( LoadingScreenOverride Info, Action? Callback = null ) {
            if (Running) {
                throw new InvalidOperationException("The loading screen is already running.");
            }

            _NameText.text = string.Format(_NameFormat, Info.DisplayName);

            _CurrentBGs = Info.GetBackgroundImages(_Backgrounds).ToArray();
            _CurrentBGsGrabBag.AddRange(_CurrentBGs);

            _CurrentTips = Info.GetLoadingTips(_Tips).ToArray();
            _CurrentTipsGrabBag.AddRange(_CurrentTips);

            AsyncOperation Op = SceneManager.LoadSceneAsync(Info.Scene, LoadSceneMode.Single);

            Op.allowSceneActivation = false;
            _Op                     = (Op, Callback);

            Running = true;

            Progress = 0f;
            ShowGraphic();
        }

        /// <inheritdoc cref="ShowLoadingScreen"/>
        public static void Show( LoadingScreenOverride Info, Action? Callback ) => Instance.ShowLoadingScreen(Info, Callback);

        void ShowGraphic() {
            if (!Running) {
                throw new InvalidOperationException("The loading screen is not running.");
            }

            LoadingScreenGraphic Graphic = Pool<LoadingScreenGraphic>.Get(_GraphicPrefab, _GraphicParent);

            Sprite? BG;
            if (_CurrentBGs.Count > 0) {
                if (_CurrentBGsGrabBag.Count == 0) { _CurrentBGsGrabBag.AddRange(_CurrentBGs); }
                BG = _CurrentBGsGrabBag.GetRandom();
                _CurrentBGsGrabBag.Remove(BG);
            } else {
                Debug.LogWarning("No backgrounds for loading screen.", this);
                BG = null;
            }

            string? Tip;
            if (_CurrentTips.Count > 0) {
                if (_CurrentTipsGrabBag.Count == 0) { _CurrentTipsGrabBag.AddRange(_CurrentTips); }
                Tip = _CurrentTipsGrabBag.GetRandom();
                _CurrentTipsGrabBag.Remove(Tip);
            } else {
                Debug.LogWarning("No tips for loading screen.", this);
                Tip = null;
            }

            IEnumerator ShowGraphicDelayed() {
                yield return new WaitForSeconds(_Delay);
                ShowGraphic();
            }
            void Callback() => StartCoroutine(ShowGraphicDelayed());
            Graphic.Set(BG, Tip, _Duration, Callback);
        }

        float _RunTime = 0f;

        // Action
        // void OnSceneLoaded( Scene Scene, LoadSceneMode Mode ) {
        //     if (!Running) { return; }
        //
        //     if (Scene != _Op.Value.Op.scene) { return; }
        //
        //     _Op = null;
        //     Running = false;
        // }

        void Update() {
            if (!Running) { return; }

            AsyncOperation Op = _Op.Value.Op;

            Progress =  Op.progress;

            _RunTime += Time.deltaTime;
            if (_RunTime < _MinDuration) { return; }

            if (Op.progress >= 0.9f) {
                Action? Callback = _Op.Value.Callback;
                IEnumerator Finalise() {
                    yield return Scenes.Instance.FadeOut();
                    Op.allowSceneActivation = true;
                    if (Callback is not null) {
                        // Callback.Invoke();
                        async UniTask InvokeCallback() {
                            await UniTask.Yield();
                            Callback.Invoke();
                        }
                        InvokeCallback().Forget(Debug.LogException);
                    }
                }

                _Op     = null;
                Running = false;
                StartCoroutine(Finalise());
            }
        }

        #region Overrides of SingletonMB<LoadingScreen>

        /// <inheritdoc />
        protected override void Awake() {
            base.Awake();
            Pool.ReturnAll<LoadingScreenGraphic>(_GraphicParent);
        }

        #endregion

    }
}
