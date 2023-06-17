using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LYGJ.QuestSystem {
    public sealed class QuestNotFoundException : KeyNotFoundException {
        /// <summary> Creates a new quest not found exception. </summary>
        /// <param name="Name"> The name of the quest that was not found. </param>
        public QuestNotFoundException( string Name ) : base($"Quest \"{Name}\" not found.") { }
    }
    public sealed class QuestStageNotFoundException : KeyNotFoundException {
        /// <summary> Creates a new stage not found exception. </summary>
        /// <param name="ID"> The quest that was unable to find the stage. </param>
        /// <param name="Stage"> The stage that was not found. </param>
        public QuestStageNotFoundException( string ID, string Stage ) : base($"Stage \"{Stage}\" not found in quest \"{ID}\".") { }
    }
    public sealed class QuestAlreadyStartedException : Exception {
        /// <summary> Creates a new quest already started exception. </summary>
        /// <param name="ID"> The quest that was already started. </param>
        public QuestAlreadyStartedException( string ID ) : base($"Quest \"{ID}\" has already been started.") { }
    }
    public sealed class QuestAlreadyCompletedException : Exception {
        /// <summary> Creates a new quest already completed exception. </summary>
        /// <param name="ID"> The quest that was already completed. </param>
        public QuestAlreadyCompletedException( string ID ) : base($"Quest \"{ID}\" has already been completed.") { }
    }
    public sealed class QuestNotStartedException : Exception {
        /// <summary> Creates a new quest not started exception. </summary>
        /// <param name="ID"> The quest that was not started. </param>
        public QuestNotStartedException( string ID ) : base($"Quest \"{ID}\" has not been started.") { }
    }
    public sealed class QuestAlreadyExistsException : Exception {
        /// <summary> Creates a new quest already exists exception. </summary>
        /// <param name="ID"> The quest that already exists. </param>
        public QuestAlreadyExistsException( string ID ) : base($"Quest \"{ID}\" already exists.") { }
    }
    public sealed class QuestStageAlreadyExistsException : Exception {
        /// <summary> Creates a new quest stage already exists exception. </summary>
        /// <param name="ID"> The quest that owns the stage. </param>
        /// <param name="Stage"> The stage that already exists. </param>
        public QuestStageAlreadyExistsException( string ID, string Stage ) : base($"Stage \"{Stage}\" already exists in quest \"{ID}\".") { }
    }
    public sealed class QuestStageAlreadyCompletedException : Exception {
        /// <summary> Creates a new quest stage already completed exception. </summary>
        /// <param name="ID"> The quest that owns the stage. </param>
        /// <param name="Stage"> The stage that is already completed. </param>
        public QuestStageAlreadyCompletedException( string ID, string Stage ) : base($"Stage \"{Stage}\" already completed in quest \"{ID}\".") { }
    }
}
