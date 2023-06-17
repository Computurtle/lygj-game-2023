using System.IO;

namespace LYGJ.Common {
    public static class FileSystemExtensions {
        /// <summary> Creates a pointer to a theoretical (or existing) file in the given directory. </summary>
        /// <remarks> This method does not create the file itself, merely a pointer to its path. </remarks>
        /// <param name="Directory"> The directory to create the pointer in. </param>
        /// <param name="FileName"> The name of the file. </param>
        /// <returns> A pointer to the file. </returns>
        public static FileInfo CreateSubfile( this DirectoryInfo Directory, string FileName ) => new(Path.Combine(Directory.FullName, FileName));
    }
}
