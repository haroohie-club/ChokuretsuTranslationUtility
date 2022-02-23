using HaruhiChokuretsuLib.Archive;
using System.Windows.Controls;

namespace HaruhiChokuretsuEditor
{
    public class EventTextBox : TextBox
    {
        public EventFile EventFile { get; set; }
        public int DialogueIndex { get; set; }
    }

    public class ShtxdsWidthBox : TextBox
    {
        public GraphicsFile Shtxds { get; set; }
    }
}
