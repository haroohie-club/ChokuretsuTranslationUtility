using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using SkiaSharp;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HaruhiChokuretsuEditor
{
    public class EventTextBox : TextBox
    {
        public EventFile EventFile { get; set; }
        public int DialogueIndex { get; set; }
    }

    public class ShtxWidthBox : TextBox
    {
        public GraphicsFile Shtxds { get; set; }
    }

    public class BitmapButton : Button
    {
        public SKBitmap Bitmap { get; set; }
    }

    public class GraphicsLayoutCreationButton : Button
    {
        public TextBox StartTextBox { get; set; }
        public TextBox LengthTextBox { get; set; }
        public bool CreatedLayout { get; set; }
    }

    public class LayoutEntryStackPanel : StackPanel
    {
        public int EntryIndex { get; set; }
        public LayoutEntry LayoutEntry { get; set; }
        public TextBox RelativeShtxIndex { get; set; } = new() { Width = 35 };
        public TextBox TextureX { get; set; } = new() { Width = 35 };
        public TextBox TextureY { get; set; } = new() { Width = 35 };
        public TextBox TextureW { get; set; } = new() { Width = 35 };
        public TextBox TextureH { get; set; } = new() { Width = 35 };
        public TextBox ScreenX { get; set; } = new() { Width = 35 };
        public TextBox ScreenY { get; set; } = new() { Width = 35 };
        public TextBox ScreenW { get; set; } = new() { Width = 35 };
        public TextBox ScreenH { get; set; } = new() { Width = 35 };
        public TextBox Tint { get; set; } = new() { Width = 100 };
        public TextBox UnknownShort1 { get; set; } = new() { Width = 35 };
        public TextBox UnknownShort2 { get; set; } = new() { Width = 35 };
        public TextBox UnknownShort3 { get; set; } = new() { Width = 35 };

        public LayoutEntryStackPanel(LayoutEntry layoutEntry, int index)
        {
            Orientation = Orientation.Horizontal;

            EntryIndex = index;
            LayoutEntry = layoutEntry;

            RelativeShtxIndex.Text = $"{layoutEntry.RelativeShtxIndex}";
            TextureX.Text = $"{layoutEntry.TextureX}";
            TextureY.Text = $"{layoutEntry.TextureY}";
            TextureW.Text = $"{layoutEntry.TextureW}";
            TextureH.Text = $"{layoutEntry.TextureH}";
            ScreenX.Text = $"{layoutEntry.ScreenX}";
            ScreenY.Text = $"{layoutEntry.ScreenY}";
            ScreenW.Text = $"{layoutEntry.ScreenW}";
            ScreenH.Text = $"{layoutEntry.ScreenH}";
            Tint.Text = $"{(uint)layoutEntry.Tint:X8}";
            UnknownShort1.Text = $"{layoutEntry.UnknownShort1}";
            UnknownShort2.Text = $"{layoutEntry.UnknownShort2}";
            UnknownShort3.Text = $"{layoutEntry.UnknownShort3}";

            RelativeShtxIndex.TextChanged += RelativeShtxIndex_TextChanged;
            TextureX.TextChanged += TextureX_TextChanged;
            TextureY.TextChanged += TextureY_TextChanged;
            TextureW.TextChanged += TextureW_TextChanged;
            TextureH.TextChanged += TextureH_TextChanged;
            ScreenX.TextChanged += ScreenX_TextChanged;
            ScreenY.TextChanged += ScreenY_TextChanged;
            ScreenW.TextChanged += ScreenW_TextChanged;
            ScreenH.TextChanged += ScreenH_TextChanged;
            Tint.TextChanged += Tint_TextChanged;
            UnknownShort1.TextChanged += UnknownShort1_TextChanged;
            UnknownShort2.TextChanged += UnknownShort2_TextChanged;
            UnknownShort3.TextChanged += UnknownShort3_TextChanged;

            Children.Add(new TextBlock { Text = $"{EntryIndex}" });
            Children.Add(RelativeShtxIndex);
            Children.Add(TextureX);
            Children.Add(TextureY);
            Children.Add(TextureW);
            Children.Add(TextureH);
            Children.Add(ScreenX);
            Children.Add(ScreenY);
            Children.Add(ScreenW);
            Children.Add(ScreenH);
            Children.Add(Tint);
            Children.Add(UnknownShort1);
            Children.Add(UnknownShort2);
            Children.Add(UnknownShort3);
        }

        private void UnknownShort3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(UnknownShort3.Text, out short relativeShtxIndex))
            {
                LayoutEntry.UnknownShort3 = relativeShtxIndex;
            }
        }

        private void UnknownShort2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(UnknownShort2.Text, out short relativeShtxIndex))
            {
                LayoutEntry.UnknownShort2 = relativeShtxIndex;
            }
        }

        private void UnknownShort1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(UnknownShort1.Text, out short relativeShtxIndex))
            {
                LayoutEntry.UnknownShort1 = relativeShtxIndex;
            }
        }

        private void RelativeShtxIndex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(RelativeShtxIndex.Text, out short relativeShtxIndex))
            {
                LayoutEntry.RelativeShtxIndex = relativeShtxIndex;
            }
        }

        private void Tint_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (uint.TryParse(Tint.Text, System.Globalization.NumberStyles.HexNumber, new System.Globalization.CultureInfo("en-US"), out uint tint))
            {
                LayoutEntry.Tint = new(tint);
            }
        }

        private void ScreenH_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(ScreenH.Text, out short screenH))
            {
                LayoutEntry.ScreenH = screenH;
            }
        }

        private void ScreenW_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(ScreenW.Text, out short screenW))
            {
                LayoutEntry.ScreenW = screenW;
            }
        }

        private void ScreenY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(ScreenY.Text, out short screenY))
            {
                LayoutEntry.ScreenY = screenY;
            }
        }

        private void ScreenX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(ScreenX.Text, out short screenX))
            {
                LayoutEntry.ScreenX = screenX;
            }
        }

        private void TextureH_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(TextureH.Text, out short textureH))
            {
                LayoutEntry.TextureH = textureH;
            }
        }

        private void TextureW_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(TextureW.Text, out short textureW))
            {
                LayoutEntry.TextureW = textureW;
            }
        }

        private void TextureY_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(TextureY.Text, out short textureY))
            {
                LayoutEntry.TextureY = textureY;
            }
        }

        private void TextureX_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (short.TryParse(TextureX.Text, out short textureX))
            {
                LayoutEntry.TextureX = textureX;
            }
        }
    }

    public class GraphicsLayoutRegenerateButton : Button
    {
        public List<LayoutEntry> LayoutEntries { get; set; }
    }
}
