using System;

namespace HaruhiChokuretsuLib.Util.Exceptions
{
    /// <summary>
    /// Custom exception thrown when archive loading fails
    /// </summary>
    public class ArchiveLoadException(int index, string filename, Exception underlyingException) : Exception
    {
        /// <summary>
        /// Index of file that failed to load
        /// </summary>
        public int Index { get; set; } = index;
        /// <summary>
        /// Name of archive file that was being loaded
        /// </summary>
        public string Filename { get; set; } = filename;
        /// <summary>
        /// Underlying exception that occurred
        /// </summary>
        public Exception UnderlyingException { get; set; } = underlyingException;
    }
}
