using System;

namespace HaruhiChokuretsuLib.Util.Exceptions
{
    public class ArchiveLoadException : Exception
    {
        public int Index { get; set; }
        public string Filename { get; set; }
        public Exception UnderlyingException { get; set; }

        public ArchiveLoadException(int index, string filename, Exception underlyingException)
        {
            Index = index;
            Filename = filename;
            UnderlyingException = underlyingException;
        }
    }
}
