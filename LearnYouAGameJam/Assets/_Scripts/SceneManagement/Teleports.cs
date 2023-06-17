using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using LYGJ.Common;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace LYGJ.SceneManagement {
    public sealed class Teleports : SingletonMB<Teleports> {
        /// <summary> Attempts to find a marker with the given name. </summary>
        /// <param name="Name"> The name of the marker to find. </param>
        /// <param name="Marker"> The marker with the given name, if found. </param>
        /// <returns> <see langword="true"/> if a marker with the given name was found; otherwise, <see langword="false"/>. </returns>
        public bool TryGetMarker( [LocalizationRequired(false)] string Name, [NotNullWhen(true)] out Transform? Marker ) {
            foreach (Transform Child in transform) {
                if (string.Equals(Child.name, Name, StringComparison.OrdinalIgnoreCase)) {
                    Marker = Child;
                    return true;
                }
            }
            Marker = null;
            return false;
        }

        /// <inheritdoc cref="TryGetMarker(string,out Transform?)"/>
        public static bool TryGet( [LocalizationRequired(false)] string Name, [NotNullWhen(true)] out Transform? Marker ) => Instance.TryGetMarker(Name, out Marker);

        /// <summary> Gets the marker with the given name. </summary>
        /// <param name="Name"> The name of the marker to get. </param>
        /// <returns> The marker with the given name. </returns>
        /// <exception cref="MarkerNotFoundException"> Thrown if a marker with the given name was not found. </exception>
        public Transform GetMarker( [LocalizationRequired(false)] string Name ) {
            if (TryGetMarker(Name, out Transform? Marker)) { return Marker; }
            throw new MarkerNotFoundException(Name);
        }

        /// <inheritdoc cref="GetMarker(string)"/>
        public static Transform Get( [LocalizationRequired(false)] string Name ) => Instance.GetMarker(Name);

        /// <inheritdoc cref="GetMarker(string)"/>
        public Transform this[ [LocalizationRequired(false)] string Name ] => GetMarker(Name);

        #if UNITY_EDITOR
        static Transform MarkerParent {
            get {
                if (Application.isPlaying) {
                    return Instance.transform;
                }
                Teleports? Inst = FindObjectOfType<Teleports>();
                if (Inst != null) {
                    return Inst.transform;
                }

                GameObject? Parent = GameObject.Find("Teleports");
                if (Parent != null) {
                    _Instance = Parent.AddComponent<Teleports>();
                } else {
                    Parent = new("Teleports", typeof(Teleports));
                    _Instance = Parent.GetComponent<Teleports>();
                }
                return Parent.transform;
            }
        }
        [MenuItem("CONTEXT/Transform/Create Teleport Marker")]
        static void CreateMarker( MenuCommand Command ) {
            Transform Source = (Transform)Command.context;
            GameObject Marker = new(Source.name.ConvertNamingConvention(NamingConvention.KebabCase));
            Marker.transform.SetParent(MarkerParent);
            Marker.transform.SetPositionAndRotation(Source.position, Source.rotation);
            Undo.RegisterCreatedObjectUndo(Marker, "Create Teleport Marker");
            EditorGUIUtility.PingObject(Marker);
        }
        #endif

        /// <summary> Gets all markers. </summary>
        /// <returns> All markers. </returns>
        [ShowInInspector, Tooltip("All markers."), LabelText("Markers"), ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
        public IReadOnlyList<Transform> AllMarkers => transform.GetChildren();

        /// <inheritdoc cref="AllMarkers"/>
        public static IEnumerable<Transform> All => Instance.AllMarkers;
    }

    /// <summary> Thrown if a marker with the given name was not found. </summary>
    [Serializable]
    public sealed class MarkerNotFoundException : Exception {
        /// <inheritdoc />
        public MarkerNotFoundException() { }

        /// <inheritdoc />
        public MarkerNotFoundException( string Name ) : base($"A marker with the name \"{Name}\" was not found.") { }

        /// <inheritdoc />
        public MarkerNotFoundException( string Name, Exception? InnerException ) : base($"A marker with the name \"{Name}\" was not found.", InnerException) { }
    }
}
