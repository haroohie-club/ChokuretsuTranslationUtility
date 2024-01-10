using HaruhiChokuretsuLib.Util;
using System;
using System.Collections.Generic;

namespace HaruhiChokuretsuLib.Archive.Event
{
    /// <summary>
    /// An interface representing event sections
    /// </summary>
    /// <typeparam name="T">The type of object that section contains</typeparam>
    public interface IEventSection<T>
    {
        /// <summary>
        /// The name of the section (user-defined)
        /// </summary>
        public string Name { get; set; }
        internal List<byte> Data { get; set; }
        /// <summary>
        /// Number of objects contained in the section
        /// </summary>
        public int NumObjects { get; set; }
        /// <summary>
        /// The length of each object in bytes
        /// </summary>
        public int ObjectLength { get; set; }
        /// <summary>
        /// The list of objects in the section
        /// </summary>
        public List<T> Objects { get; set; }
        /// <summary>
        /// The type of the section
        /// </summary>
        public Type SectionType { get; set; }
        /// <summary>
        /// The type of the objects of the section
        /// </summary>
        public Type ObjectType { get; set; }

        internal void Initialize(IEnumerable<byte> data, int numObjects, string name, ILogger log, int offset);
        internal string GetAsm(int indentation, ref int currentPointer, EventFile evt);
        internal IEventSection<object> GetGeneric();
    }
}
