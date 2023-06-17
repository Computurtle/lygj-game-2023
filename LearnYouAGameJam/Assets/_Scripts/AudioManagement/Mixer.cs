using System.Diagnostics;

namespace LYGJ.AudioManagement {
    public enum Mixer {
        /// <summary> The master mixer group. </summary>
        Master,
        /// <summary> The mixer group for music. </summary>
        Music,
        /// <summary> The mixer group for sound effects. </summary>
        SFX,
        /// <summary> The mixer group for UI sound effects. </summary>
        UI,
        /// <summary> The mixer group for ambient sound effects. </summary>
        Ambience,
        /// <summary> The mixer group for voice over. </summary>
        Vox
    }
}
