using System;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive.Event
{
    public interface IEventSection<T>
    {
        public string Name { get; set; }
        public List<byte> Data { get; set; }
        public int NumObjects { get; set; }
        public int ObjectLength { get; set; }
        public List<T> Objects { get; set; }
        public Type SectionType { get; set; }
        public Type ObjectType { get; set; }

        public void Initialize(IEnumerable<byte> data, int numObjects, string name, int offset);
        public string GetAsm(int indentation, ref int currentPointer);
        public IEventSection<object> GetGeneric();
    }
}
