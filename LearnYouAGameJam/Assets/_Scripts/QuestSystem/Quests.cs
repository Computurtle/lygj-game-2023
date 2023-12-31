﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using LYGJ.SaveManagement;
using LYGJ.SceneManagement;
using UnityEngine;

namespace LYGJ.QuestSystem {
    public static class Quests {
        /// <summary> The path to the resources folder containing all quests. </summary>
        public const string ResourcesPath = "Quests";

        /// <summary> Gets all quests. </summary>
        public static IEnumerable<Quest> All => Resources.LoadAll<Quest>(ResourcesPath);

        /// <summary> Attempts to get the quest with the specified ID. </summary>
        /// <param name="ID"> The ID of the quest to get. </param>
        /// <param name="Quest"> [out] The quest with the specified ID, if found. </param>
        /// <returns> <see langword="true"/> if the quest was found; otherwise, <see langword="false"/>. </returns>
        public static bool TryGet( [LocalizationRequired(false)] string ID, [NotNullWhen(true)] out Quest? Quest ) {
            Quest = Resources.Load<Quest>($"{ResourcesPath}/{ID.ToLowerInvariant()}");
            return Quest != null;
        }

        /// <summary> Gets whether the quest with the specified ID exists. </summary>
        /// <param name="ID"> The ID of the quest to check for. </param>
        /// <returns> <see langword="true"/> if the quest exists; otherwise, <see langword="false"/>. </returns>
        public static bool Exists( [LocalizationRequired(false)] string ID ) => TryGet(ID, out _);

        /// <summary> Starts the quest with the specified ID. </summary>
        /// <param name="ID"> The ID of the quest to start. </param>
        /// <param name="Token"> The cancellation token which can be used to cancel the quest (such as when the scene changes). </param>
        /// <exception cref="QuestNotFoundException"> Thrown if the quest with the specified ID could not be found. </exception>
        /// <exception cref="QuestAlreadyStartedException"> Thrown if the quest with the specified ID has already been started. </exception>
        public static void Start( [LocalizationRequired(false)] string ID, CancellationToken Token ) { // TODO: Remember started quests and start when game begins.
            ID = ID.ToLowerInvariant();
            if (!TryGet(ID, out Quest? Quest)) {
                throw new QuestNotFoundException(ID);
            }
            if (GetCompletion(ID) is not Completion.NotStarted) {
                throw new QuestAlreadyStartedException(ID);
            }
            Quest.Construct();
            Quest.StartQuest(Token);
        }

        /// <inheritdoc cref="Start(string,CancellationToken)"/>
        public static void Start( [LocalizationRequired(false)] string ID ) => Start(ID, Scenes.SceneChangeToken);

        [Pure, MustUseReturnValue]
        [return: LocalizationRequired(false)]
        static string GetQuestSaveID( [LocalizationRequired(false)] params string[] Segments ) => SaveData.GetName(Segments.Prepend("q"));

        /// <summary> Gets the completion status of the quest with the specified ID. </summary>
        /// <param name="ID"> The ID of the quest to get the completion status of. </param>
        /// <returns> The completion status of the quest with the specified ID. </returns>
        public static Completion GetCompletion( [LocalizationRequired(false)] string ID ) => Saves.Current.GetOrCreate(GetQuestSaveID(ID), Completion.NotStarted);

        /// <summary> Sets the completion status of the quest with the specified ID. </summary>
        /// <param name="ID"> The ID of the quest to set the completion status of. </param>
        /// <param name="Completion"> The completion status to set. </param>
        public static void SetCompletion( [LocalizationRequired(false)] string ID, Completion Completion ) => Saves.Current.Set(GetQuestSaveID(ID), Completion);

        /// <summary> Gets the completion status of the quest stage with the specified ID. </summary>
        /// <param name="QuestID"> The ID of the quest to get the completion status of. </param>
        /// <param name="StageID"> The ID of the quest stage to get the completion status of. </param>
        /// <returns> The completion status of the quest stage with the specified ID. </returns>
        public static Completion GetCompletion( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID ) => Saves.Current.GetOrCreate(GetQuestSaveID(QuestID, StageID), Completion.NotStarted);

        /// <summary> Sets the completion status of the quest stage with the specified ID. </summary>
        /// <param name="QuestID"> The ID of the quest to set the completion status of. </param>
        /// <param name="StageID"> The ID of the quest stage to set the completion status of. </param>
        /// <param name="Completion"> The completion status to set. </param>
        public static void SetCompletion( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID, Completion Completion ) => Saves.Current.Set(GetQuestSaveID(QuestID, StageID), Completion);

        /// <summary> (Re-)starts the quest stage with the specified ID. </summary>
        /// <param name="QuestID"> The ID of the quest to (re-)start. </param>
        /// <param name="StageID"> The ID of the quest stage to (re-)start. </param>
        /// <param name="Token"> The cancellation token which can be used to cancel the quest (such as when the scene changes). </param>
        /// <exception cref="QuestNotFoundException"> Thrown if the quest with the specified ID could not be found. </exception>
        public static void StartStage( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID, CancellationToken Token = default ) {
            if (!TryGet(QuestID, out Quest? Quest)) {
                throw new QuestNotFoundException(QuestID);
            }
            Quest.StartStage(StageID.ToLowerInvariant(), Token);
        }

        /// <inheritdoc cref="SetCompletion(string,Completion)"/>
        public static void Complete( [LocalizationRequired(false)] string ID ) => SetCompletion(ID, Completion.Completed);

        /// <inheritdoc cref="SetCompletion(string,string,Completion)"/>
        public static void CompleteStage( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID ) => SetCompletion(QuestID, StageID, Completion.Completed);

        /// <inheritdoc cref="SetCompletion(string,Completion)"/>
        [Obsolete]
        public static void Fail( [LocalizationRequired(false)] string ID ) => SetCompletion(ID, Completion.Failed);

        /// <inheritdoc cref="SetCompletion(string,string,Completion)"/>
        [Obsolete]
        public static void FailStage( [LocalizationRequired(false)] string QuestID, [LocalizationRequired(false)] string StageID ) => SetCompletion(QuestID, StageID, Completion.Failed);
    }

    public enum Completion {
        NotStarted = -1,
        Started    = 0,
        Completed  = 1,
        [Obsolete]
        Failed     = 2
    }
}
