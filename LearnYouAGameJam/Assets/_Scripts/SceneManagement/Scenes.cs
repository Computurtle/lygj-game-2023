using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LYGJ.Common;
using LYGJ.Common.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace LYGJ.SceneManagement {
    public sealed class Scenes : SingletonMB<Scenes> {

        const string _ResourcePath = "Scenes";

        /// <inheritdoc cref="SingletonMB{T}.Instance"/>
        public new static Scenes Instance {
            get {
                try {
                    return SingletonMB<Scenes>.Instance;
                } catch (SingletonNotFoundException) {
                    Scenes? Prefab = Resources.Load<Scenes>(_ResourcePath);
                    if (Prefab != null) {
                        return _Instance = Instantiate(Prefab);
                    }

                    #if UNITY_EDITOR
                    if (_Instance == null) {
                        GameObject GO = new(nameof(Scenes), typeof(Scenes));
                        _Instance = GO.GetComponent<Scenes>();

                        string Path = $"Assets/Resources/{_ResourcePath}.prefab";
                        PrefabUtility.SaveAsPrefabAsset(GO, Path);
                        Debug.Log($"Created prefab at {Path}.");
                        if (Application.isPlaying) {
                            Destroy(GO);
                        } else {
                            DestroyImmediate(GO);
                        }

                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Scenes>(Path));

                        return _Instance;
                    }
                    #endif

                    throw;
                }
            }
        }

        /// <summary> Gets the current scene name. </summary>
        public static string Current => SceneManager.GetActiveScene().name;

        [Title("Fading")]
        [SerializeField, Tooltip("The fade in time, in seconds."), SuffixLabel("s"), MinValue(0f)]
        float _FadeInTime = 0.5f;
        [SerializeField, Tooltip("The fade in delay, in seconds."), SuffixLabel("s"), MinValue(0f)]
        float _FadeInDelay = 0.5f;
        [SerializeField, Tooltip("The fade out time, in seconds."), SuffixLabel("s"), MinValue(0f)]
        float _FadeOutTime = 0.5f;
        [SerializeField, Tooltip("The fade out delay, in seconds."), SuffixLabel("s"), MinValue(0f)]
        float _FadeOutDelay = 0.5f;
        [SerializeField, Tooltip("The canvas group to spawn for fading."), Required, AssetsOnly]
        CanvasGroup _FadeCanvasGroupPrefab = null!;

        /// <summary> Fades out the current scene to black. </summary>
        /// <returns> The coroutine. </returns>
        public IEnumerator FadeOut() => FadeTo(Pool<CanvasGroup>.Get(_FadeCanvasGroupPrefab, transform), false, _FadeOutTime, _FadeOutDelay);

        /// <summary> Fades in the current scene from black. </summary>
        /// <returns> The coroutine. </returns>
        public IEnumerator FadeIn() => FadeTo(Pool<CanvasGroup>.Get(_FadeCanvasGroupPrefab, transform), true, _FadeInTime, _FadeInDelay);

        static IEnumerator FadeTo( CanvasGroup Group, bool In, float Duration, float Delay ) {
            float Elapsed = 0f;
            float Start, End;
            if (In) {
                Start = Group.alpha = 1f;
                End   = 0f;
            } else {
                Start = Group.alpha = 0f;
                End   = 1f;
            }
            Group.blocksRaycasts = true;
            yield return new WaitForSeconds(Delay);
            while (Elapsed < Duration) {
                Group.alpha =  Mathf.Lerp(Start, End, Elapsed / Duration);
                Elapsed     += Time.deltaTime;
                yield return null;
            }
            Group.alpha          = End;
            Group.blocksRaycasts = false;
            if (In) {
                Pool<CanvasGroup>.Return(Group);
            }
        }

        void Start() {
            StartCoroutine(FadeIn());
            SceneManager.sceneLoaded += FadeInOnLoad;
        }
        void FadeInOnLoad( Scene Scene, LoadSceneMode Mode ) {
            Pool<CanvasGroup>.ReturnAll(transform);
            StartCoroutine(FadeIn());
        }

        #region Overrides of SingletonMB<Scenes>

        /// <inheritdoc />
        protected override void OnDestroy() {
            base.OnDestroy();
            SceneManager.sceneLoaded -= FadeInOnLoad;
        }

        #endregion

        [Title("Loading Screen")]
        [SerializeField, Tooltip("Whether loading screens should be used by default (i.e. if no override is specified)."), LabelText("On By Default"), ToggleLeft]
        bool _UseLoadingScreenByDefault = true;

        [SerializeField, Tooltip("The loading screen scene."), Scene, LabelText("Scene")]
        string _LoadingScene = "LoadingScreen";

        [SerializeField, Tooltip("The loading screen overrides, if any."), ListDrawerSettings(ShowFoldout = false)]
        LoadingScreenOverride[] _Overrides = Array.Empty<LoadingScreenOverride>();

        #if UNITY_EDITOR
        [Button("Add Missing Overrides", ButtonSizes.Medium), ShowIf(nameof(Editor_HasMissingOverrides))]
        void Editor_AddMissingOverrides() {
            HashSet<string> Scenes = new();
            foreach (LoadingScreenOverride Override in _Overrides) {
                Scenes.Add(Override.Scene);
            }

            foreach (EditorBuildSettingsScene Scene in EditorBuildSettings.scenes) {
                string Name = Path.GetFileNameWithoutExtension(Scene.path);
                if (string.Equals(Name, _LoadingScene, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (!Scenes.Contains(Name)) {
                    ArrayUtility.Add(ref _Overrides, new(Name, Name.ConvertNamingConvention(NamingConvention.TitleCase)));
                }
            }
        }
        bool Editor_HasMissingOverrides() {
            if (_Overrides.Length != EditorBuildSettings.scenes.Length - 1) {
                return true;
            }

            HashSet<string> Scenes = new();
            foreach (LoadingScreenOverride Override in _Overrides) {
                Scenes.Add(Override.Scene);
            }

            foreach (EditorBuildSettingsScene Scene in EditorBuildSettings.scenes) {
                string Name = Path.GetFileNameWithoutExtension(Scene.path);
                if (string.Equals(Name, _LoadingScene, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                if (!Scenes.Contains(Name)) {
                    return true;
                }
            }

            return false;
        }
        #endif

        /// <summary> Gets the override for the given scene. </summary>
        /// <param name="Scene"> The scene. </param>
        /// <returns> The override for the given scene. </returns>
        public LoadingScreenOverride GetOverride( string Scene ) {
            foreach (LoadingScreenOverride Override in _Overrides) {
                if (string.Equals(Override.Scene, Scene, StringComparison.OrdinalIgnoreCase)) {
                    return Override;
                }
            }

            return new(Scene, _UseLoadingScreenByDefault);
        }

        #region Overrides of SingletonMB<Scenes>

        /// <inheritdoc />
        protected override void Awake() {
            if (_Instance != null && _Instance != this) {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(this);
            _Instance = this;
        }

        #endregion


        /// <summary> Gets whether the given scene is the loading screen scene. </summary>
        /// <param name="Scene"> The scene. </param>
        /// <returns> <see langword="true"/> if the given scene is the loading screen scene; otherwise, <see langword="false"/>. </returns>
        static bool GetIsLoadingScreen( string Scene ) => string.Equals(Scene, Instance._LoadingScene, StringComparison.OrdinalIgnoreCase);


        /// <inheritdoc cref="GetIsLoadingScreen(string)"/>
        static bool GetIsLoadingScreen( Scene Scene ) => GetIsLoadingScreen(Scene.name);

        /// <summary> Gets whether we are currently in the loading screen scene. </summary>
        /// <returns> <see langword="true"/> if we are currently in the loading screen scene; otherwise, <see langword="false"/>. </returns>
        public static bool IsLoadingScreen => GetIsLoadingScreen(Current);

        (LoadingScreenOverride Override, Action? Callback)? _Loading = null;

        /// <summary> Loads the given scene. </summary>
        /// <param name="Scene"> The scene. </param>
        /// <param name="Loaded"> The action to perform when the scene is loaded. </param>
        public void LoadScene( string Scene, Action? Loaded = null ) {
            if (_SceneChangeCTS is { } CancelCTS) { CancelCTS.Cancel(); }
            SceneUnloaded?.Invoke(Current);

            LoadingScreenOverride Override = GetOverride(Scene);

            SceneManager.sceneLoaded += OnSceneLoaded;

            _Loading = (Override, Loaded);

            IEnumerator DoThing() {
                yield return FadeOut();
                SceneManager.LoadScene(Override.Use ? _LoadingScene : Scene);
            }
            StartCoroutine(DoThing());
        }

        /// <inheritdoc cref="LoadScene(string,Action?)"/>
        public static void Load( string Scene, Action? Loaded = null ) => Instance.LoadScene(Scene, Loaded);

        Action? _FirstFrame = null; // See: http://answers.unity.com/answers/1315128/view.html / https://issuetracker.unity3d.com/issues/loadsceneasync-allowsceneactivation-flag-is-ignored-in-awake

        public delegate void SceneLoadedEventHandler( string Scene );

        /// <summary> Called when a scene is loaded. </summary>
        /// <remarks> This is called after <c>Start</c>, upon the first <see cref="UnityEngine.PlayerLoop.Update"/> after a scene change. <br/>
        /// Additionally, this is not called on the loading screen scene. Use <see cref="LoadingSceneLoaded"/> for that. <br/><br/>
        /// Calling instance classes must be sure to unsubscribe from this event when they are destroyed lest they cause unexpected behaviour / memory leaks. </remarks>
        [Obsolete] public static event SceneLoadedEventHandler? SceneLoaded;

        /// <summary> Called when the loading screen scene is loaded. </summary>
        /// <remarks> This is called after <c>Start</c>, upon the first <see cref="UnityEngine.PlayerLoop.Update"/> after a scene change. <br/><br/>
        /// Calling instance classes must be sure to unsubscribe from this event when they are destroyed lest they cause unexpected behaviour / memory leaks. </remarks>
        [Obsolete] public static event SceneLoadedEventHandler? LoadingSceneLoaded;

        /// <summary> Called when a scene is unloaded. </summary>
        /// <remarks> This is called just before <c>OnDestroy</c> on the scene's root objects. <br/><br/>
        /// Calling instance classes must be sure to unsubscribe from this event when they are destroyed lest they cause unexpected behaviour / memory leaks. </remarks>
        [Obsolete] public static event SceneLoadedEventHandler? SceneUnloaded;

        void OnSceneLoaded( Scene Scene, LoadSceneMode Mode ) {
            if (!GetIsLoadingScreen(Scene)) {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
            void FirstFrame() {
                if (_Loading is { } L) {
                    if (L.Override.Use) {
                        LoadingScreen.Show(L.Override, L.Callback);
                    } else {
                        L.Callback?.Invoke();
                    }
                    _Loading = null;

                    if (Pool<CanvasGroup>.ReturnAll(transform) == 0) {
                        Debug.LogError("Failed to return canvas group.", this);
                    }
                }

                async UniTask FirstFrameDelayed() {
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);

                    string Scene = Current;
                    if (Scene == _LoadingScene) {
                        // Debug.Log($"Invoking {nameof(LoadingSceneLoaded)} for scene {Scene}. ({LoadingSceneLoaded?.GetInvocationList().Length ?? 0} listeners)");
                        LoadingSceneLoaded?.Invoke(Scene);
                    } else {
                        // Debug.Log($"Invoking {nameof(SceneLoaded)} for scene {Scene}. ({SceneLoaded?.GetInvocationList().Length ?? 0} listeners)");
                        SceneLoaded?.Invoke(Scene);
                    }
                }
                FirstFrameDelayed().Forget(Debug.LogException);
            }

            _FirstFrame = FirstFrame;
        }

        CancellationTokenSource? _SceneChangeCTS;

        void Update() {
            if (_FirstFrame is { } Frame) {
                Frame();
                _FirstFrame = null;
            }
        }

        /// <summary> Returns a cancellation token that is cancelled when the scene changes. </summary>
        /// <returns> A cancellation token that is cancelled when the scene changes. </returns>
        public static CancellationToken SceneChangeToken => Instance.GetSceneChangeToken();

        CancellationToken GetSceneChangeToken() {
            if (_SceneChangeCTS is null) {
                _SceneChangeCTS = new();
                void Cancelled() {
                    _SceneChangeCTS?.Dispose();
                    _SceneChangeCTS = null;
                }
                _SceneChangeCTS.Token.Register(Cancelled);
            }
            return _SceneChangeCTS.Token;
        }

        /// <summary> Waits for the scene to change. </summary>
        /// <remarks> This is called just prior to the scene change to ensure objects have not yet been destroyed. </remarks>
        /// <returns> The asynchronous operation. </returns>
        [MustUseReturnValue] public static UniTask WaitForSceneChange() => WaitForSceneChange(CancellationToken.None);

        /// <inheritdoc cref="WaitForSceneChange()"/>
        /// <param name="Token"> The cancellation token. </param>
        [MustUseReturnValue] public static UniTask WaitForSceneChange( CancellationToken Token ) {
            static async UniTask Awaiter( CancellationToken Token ) {
                (UniTask Task, CancellationTokenRegistration Registration) = SceneChangeToken.ToUniTask();
                await using (Registration) {
                    await Task.AttachExternalCancellation(Token);
                }
            }
            return Awaiter(Token);
        }

        #if UNITY_EDITOR
        /// <summary> Gets whether the application is exiting. </summary>
        public static bool IsApplicationExiting => (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) || IsSceneUnloading;
        #endif

        /// <summary> Gets whether the current scene is unloading. </summary>
        /// <remarks> This can be used to determine if we are in an <c>OnDestroy()</c> scope. </remarks>
        public static bool IsSceneUnloading => !SceneManager.GetActiveScene().isLoaded;

    }

    [Serializable]
    public struct LoadingScreenOverride {
        [SerializeField, HideLabel, LabelWidth(0f), HorizontalGroup, PropertyOrder(-1), Scene]
        string _Scene;

        [SerializeField, HideLabel, LabelWidth(0f), HorizontalGroup(20f), PropertyOrder(0)]
        bool _Use;

        /// <summary> Gets the scene. </summary>
        public string Scene => _Scene;

        /// <summary> Gets whether to use the loading screen. </summary>
        public bool Use => _Use;

        public LoadingScreenOverride( string Scene, string DisplayName, bool Use = true ) {
            _Scene                      = Scene;
            _DisplayName                = DisplayName;
            _Use                        = Use;
            _BackgroundImages           = Array.Empty<Sprite>();
            _CumulativeBackgroundImages = false;
            _LoadingTips                = Array.Empty<string>();
            _CumulativeLoadingTips      = false;
        }

        public LoadingScreenOverride( string Scene, bool Use = true ) : this(Scene, Scene, Use) { }

        /// <summary> Gets whether the given scene should use the loading screen. </summary>
        /// <param name="Overrides"> The overrides. </param>
        /// <param name="Default"> The default value. </param>
        /// <param name="Scene"> The scene. </param>
        /// <returns> <see langword="true"/> if the given scene should use the loading screen; otherwise, <see langword="false"/>. </returns>
        public static bool ShouldUseLoadingScreen( IEnumerable<LoadingScreenOverride> Overrides, bool Default, string Scene ) {
            foreach (LoadingScreenOverride Override in Overrides) {
                if (string.Equals(Override.Scene, Scene, StringComparison.OrdinalIgnoreCase)) {
                    return Override.Use;
                }
            }

            return Default;
        }

        [SerializeField, Tooltip("The display name of the scene."), LabelText("Display Name"), ShowIf(nameof(_Use))]
        string _DisplayName;

        [SerializeField, Tooltip("The background image(s)."), LabelText("Background Image(s)"), ShowIf(nameof(_Use)), HideDuplicateReferenceBox]
        Sprite[] _BackgroundImages;

        [SerializeField, Tooltip("Whether the provided background images are cumulative (build on the default collection, rather than replacing it)."), LabelText("Cumulative"), ShowIf("@" + nameof(_Use) + " && " + nameof(_BackgroundImages) + ".Length > 0"), ToggleLeft]
        bool _CumulativeBackgroundImages;

        [SerializeField, Tooltip("The loading tip(s). If empty, the default loading tip(s) will be used."), LabelText("Loading Tip(s)"), ShowIf(nameof(_Use)), HideDuplicateReferenceBox]
        string[] _LoadingTips;

        [SerializeField, Tooltip("Whether the provided loading tips are cumulative (build on the default collection, rather than replacing it)."), LabelText("Cumulative"), ShowIf("@" + nameof(_Use) + " && " + nameof(_LoadingTips) + ".Length > 0"), ToggleLeft]
        bool _CumulativeLoadingTips;

        /// <summary> Gets the display name of the scene. </summary>
        public string DisplayName => _DisplayName;

        /// <summary> Gets the background image(s). </summary>
        /// <param name="Default"> The default background image(s). </param>
        /// <returns> The background image(s). </returns>
        public IEnumerable<Sprite> GetBackgroundImages( IEnumerable<Sprite> Default ) {
            if (_CumulativeBackgroundImages || _BackgroundImages.Length == 0) {
                foreach (Sprite BackgroundImage in Default) {
                    yield return BackgroundImage;
                }
            }

            foreach (Sprite BackgroundImage in _BackgroundImages) {
                yield return BackgroundImage;
            }
        }

        /// <summary> Gets the loading tip(s). </summary>
        /// <param name="Default"> The default loading tip(s). </param>
        /// <returns> The loading tip(s). </returns>
        public IEnumerable<string> GetLoadingTips( IEnumerable<string> Default ) {
            if (_CumulativeLoadingTips || _LoadingTips.Length == 0) {
                foreach (string LoadingTip in Default) {
                    yield return LoadingTip;
                }
            }

            foreach (string LoadingTip in _LoadingTips) {
                yield return LoadingTip;
            }
        }
    }
}
