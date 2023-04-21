using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive
{
    public interface ISourceFile
    {
        public string GetSource(Dictionary<string, IncludeEntry[]> includes);
    }

    public class IncludeEntry
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public IncludeEntry(string include)
        {
            Name = include[5..include.IndexOf(',')];
            Value = int.Parse(include[(include.IndexOf(',') + 1)..]);
        }
    }
}
