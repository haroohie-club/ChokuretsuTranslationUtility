using FolderBrowserEx;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using Microsoft.Win32;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace HaruhiChokuretsuEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ArchiveFile<EventFile> _evtFile;
        private ArchiveFile<GraphicsFile> _grpFile;
        private ArchiveFile<DataFile> _datFile;

        private int _currentImageWidth = 256;
        private int _currentSearchIndex = 0;

        private bool _layoutDarkMode = false;

        private readonly ConsoleLogger _log = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "EVT file|evt*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _evtFile = ArchiveFile<EventFile>.FromFile(openFileDialog.FileName, _log);
                _evtFile.GetFileByIndex(580).InitializeScenarioFile();
                _evtFile.GetFileByIndex(581).InitializeTopicFile();
                _evtFile.Files.Where(f => f.Index is >= 359 and <= 531).ToList().ForEach(f => f.IdentifyEventFileTopics(_evtFile.GetFileByIndex(581).Topics));

                EventFile voiceMapFile = _evtFile.GetFileByIndex(589);
                if (voiceMapFile is not null)
                {
                    _evtFile.Files[_evtFile.Files.IndexOf(voiceMapFile)] = voiceMapFile.CastTo<VoiceMapFile>();
                }

                FontReplacementDictionary fontReplacementDictionary = new();
                fontReplacementDictionary.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText("Font/font_replacement.json")));
                _evtFile.Files.ForEach(e => e.FontReplacementMap = fontReplacementDictionary);

                eventsListBox.ItemsSource = _evtFile.Files;
                eventsListBox.Items.Refresh();
            }
        }
        private void OpenEventsDatFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "DAT File|dat*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _evtFile = ArchiveFile<EventFile>.FromFile(openFileDialog.FileName, _log);
                _evtFile.Files.ForEach(f => f.InitializeDialogueForSpecialFiles());
                FontReplacementDictionary fontReplacementDictionary = new();
                fontReplacementDictionary.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(File.ReadAllText("Font/font_replacement.json")));
                _evtFile.Files.ForEach(e => e.FontReplacementMap = fontReplacementDictionary);

                eventsListBox.ItemsSource = _evtFile.Files;
                eventsListBox.Items.Refresh();
            }
        }

        private void SaveEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            string filter = _evtFile.FileName.StartsWith("dat") ? "DAT file|dat*.bin" : "EVT file|evt*.bin";
            SaveFileDialog saveFileDialog = new()
            {
                Filter = filter
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _evtFile.GetBytes());
                MessageBox.Show("Save completed!");
            }
        }

        private void ExportEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, ((EventFile)eventsListBox.SelectedItem).GetBytes());
            }
        }

        private void ImportEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                EventFile newEventFile = new();
                newEventFile.Initialize(File.ReadAllBytes(openFileDialog.FileName), _evtFile.Files[eventsListBox.SelectedIndex].Offset, _log);
                newEventFile.Index = _evtFile.Files[eventsListBox.SelectedIndex].Index;
                newEventFile.Name = _evtFile.Files[eventsListBox.SelectedIndex].Name;
                newEventFile.Offset = _evtFile.Files[eventsListBox.SelectedIndex].Offset;
                _evtFile.Files[eventsListBox.SelectedIndex] = newEventFile;
                _evtFile.Files[eventsListBox.SelectedIndex].Edited = true;
                eventsListBox.Items.Refresh();
            }
        }

        private void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editStackPanel.Children.Clear();
            eventsTopicsStackPanel.Children.Clear();
            eventsScenariosStackPanel.Children.Clear();
            eventSettingsStackPanel.Children.Clear();
            frontPointersStackPanel.Children.Clear();
            endPointersStackPanel.Children.Clear();
            if (eventsListBox.SelectedIndex >= 0)
            {
                EventFile selectedFile = (EventFile)eventsListBox.SelectedItem;
                mainWindow.Title = $"Suzumiya Haruhi no Chokuretsu Editor - Event 0x{selectedFile.Index:X3}";
                editStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.DialogueLines.Count} lines of dialogue" });
                frontPointersStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.Data?.Count ?? 0} bytes" });
                frontPointersStackPanel.Children.Add(new TextBlock { Text = $"Actual compressed length: {selectedFile.CompressedData?.Length ?? 0:X}; Calculated length: {selectedFile.Length:X}" });
                for (int i = 0; i < selectedFile.DialogueLines.Count; i++)
                {
                    StackPanel dialogueStackPanel = new() { Orientation = Orientation.Horizontal };
                    dialogueStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.DialogueLines[i].Speaker} ({selectedFile.DialogueLines[i].SpeakerName}):\t" });
                    EventTextBox textBox = new() { EventFile = selectedFile, DialogueIndex = i, Text = selectedFile.DialogueLines[i].Text, AcceptsReturn = true };
                    textBox.TextChanged += TextBox_TextChanged;
                    dialogueStackPanel.Children.Add(textBox);
                    editStackPanel.Children.Add(dialogueStackPanel);
                }
                foreach (Topic topic in selectedFile.Topics)
                {
                    StackPanel topicStackPanel = new() { Orientation = Orientation.Horizontal };
                    topicStackPanel.Children.Add(new TextBlock { Text = $"{topic.EventIndex} (0x{topic.EventIndex:X3})" });
                    eventsTopicsStackPanel.Children.Add(topicStackPanel);
                }
                foreach (ScenarioCommand command in selectedFile.Scenario?.Commands ?? new())
                {
                    eventsScenariosStackPanel.Children.Add(new TextBlock { Text = command.GetParameterString(_evtFile, _datFile) });
                }
                //foreach (ScenarioRouteSelectionStruct scenario in selectedFile.ScenarioStructs)
                //{
                //    StackPanel scenarioStackPanel = new();
                //    scenarioStackPanel.Children.Add(new TextBlock { Text = $"{scenario.Title}" });
                //    scenarioStackPanel.Children.Add(new TextBlock { Text = $"{scenario.UnknownInt1}\t{scenario.UnknownInt2}\t" +
                //        $"{scenario.UnknownInt3}\t{scenario.UnknownInt4}\t{scenario.UnknownInt5}\t{scenario.UnknownInt6}" });
                //    scenarioStackPanel.Children.Add(new TextBlock { Text = $"Required Brigade Member: {scenario.RequiredBrigadeMember}" });
                //    scenarioStackPanel.Children.Add(new TextBlock { Text = $"Haruhi Present: {scenario.HaruhiPresent}" });
                //    foreach (ScenarioRouteStruct route in scenario.Routes)
                //    {
                //        StackPanel routeStackPanel = new() { Orientation = Orientation.Horizontal };
                //        routeStackPanel.Children.Add(new TextBlock { Text = $"{route.Title} ({route.UnknownShort}) -> {route.ScriptIndex}\t" });
                //        routeStackPanel.Children.Add(new TextBlock { Text = $"{string.Join(", ", route.CharactersInvolved)}" });
                //        scenarioStackPanel.Children.Add(routeStackPanel);
                //    }
                //    eventsScenariosStackPanel.Children.Add(scenarioStackPanel);
                //    eventsScenariosStackPanel.Children.Add(new Separator());
                //}
                if (selectedFile.Settings is not null)
                {
                    eventSettingsStackPanel.Children.Add(new TextBlock { Text = $"{nameof(selectedFile.Settings.NumUnknown01)}: {selectedFile.Settings.NumUnknown01}" });
                    eventSettingsStackPanel.Children.Add(new TextBlock { Text = $"{nameof(selectedFile.Settings.NumUnknown01)}: {selectedFile.Settings.NumUnknown01}" });
                    eventSettingsStackPanel.Children.Add(new TextBlock { Text = $"{nameof(selectedFile.Settings.NumChoices)}: {selectedFile.Settings.NumChoices}" });
                    
                    foreach (EventFileSection section in selectedFile.EventFileSections)
                    {
                        if (section.Section is not null)
                        {
                            eventSettingsStackPanel.Children.Add(new TextBlock { Text = $"{section.Section.Name}: " +
                                $"{string.Join(", ", section.Section.Objects.Where(o => o is not null).Select(o => ((dynamic)Convert.ChangeType(o, section.Section.ObjectType)).ToString()))}" });
                        }
                    }
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EventTextBox textBox = (EventTextBox)sender;
            textBox.EventFile.EditDialogueLine(textBox.DialogueIndex, textBox.Text);
        }

        private void ExportStringsEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (eventsListBox.SelectedIndex >= 0)
            {
                EventFile selectedFile = (EventFile)eventsListBox.SelectedItem;
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "RESX files|*.resx"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    selectedFile.WriteResxFile(saveFileDialog.FileName);
                }
            }
        }

        private void ImportStringsEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (eventsListBox.SelectedIndex >= 0)
            {
                EventFile selectedFile = (EventFile)eventsListBox.SelectedItem;
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "RESX files|*.resx"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    selectedFile.ImportResxFile(openFileDialog.FileName);
                }
            }
        }

        private void ExportAllStringsEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new()
            {
                AllowMultiSelect = false
            };
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (EventFile eventFile in _evtFile.Files)
                {
                    eventFile.WriteResxFile(Path.Combine(folderBrowser.SelectedFolder, $"{eventFile.Index:D3}.ja.resx"));
                }
            }
        }

        private void ImportAllStringsEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            LanguageCodeDialogBox languageCodeDialogBox = new();
            if (languageCodeDialogBox.ShowDialog() == true)
            {
                FolderBrowserDialog folderBrowser = new()
                {
                    AllowMultiSelect = false
                };
                if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(folderBrowser.SelectedFolder)
                        .Where(f => f.EndsWith($".{languageCodeDialogBox.LanguageCode}.resx", StringComparison.OrdinalIgnoreCase)).ToArray();
                    foreach (string file in files)
                    {
                        if (int.TryParse(Regex.Match(file, @"(\d{3})\.[\w-]+\.resx").Groups[1].Value, out int fileIndex))
                        {
                            _evtFile.GetFileByIndex(fileIndex).ImportResxFile(file);
                        }
                    }
                }
            }
        }

        private void ExportTopicsEventsFileButton_Click(object sender, RoutedEventArgs e)
        {
            TopicDialogBox topicDialogBox = new();
            if (topicDialogBox.ShowDialog() == true && topicDialogBox.FinalFileIndex is not null)
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "Comma-separated values|*.csv"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    List<string> topics = _evtFile.Files.Where(f => f.Index >= ((EventFile)eventsListBox.SelectedItem).Index && f.Index <= topicDialogBox.FinalFileIndex)
                        .SelectMany(e => e.Topics)
                        .Distinct()
                        .Select(t => t.ToCsvLine())
                        .ToList();

                    topics.Insert(0, "Topic Index,Topic,UID,Associated Event");
                    File.WriteAllLines(saveFileDialog.FileName, topics);
                }
            }
        }

        private void DialogueSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_evtFile is not null && !string.IsNullOrWhiteSpace(dialogueSearchBox.Text))
            {
                List<EventFile> filteredEventFiles = _evtFile.Files
                    .Where(f => f.DialogueLines.FirstOrDefault(d => d.Text.Contains(dialogueSearchBox.Text.ToLowerInvariant())) is not null).ToList();
                SearchDialogue(filteredEventFiles);
            }
        }

        private void DialogueNextSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_evtFile is not null && !string.IsNullOrWhiteSpace(dialogueSearchBox.Text))
            {
                List<EventFile> filteredEventFiles = _evtFile.Files
                    .Where(f => f.DialogueLines.FirstOrDefault(d => d.Text.Contains(dialogueSearchBox.Text.ToLowerInvariant())) is not null).ToList();
                if (++_currentSearchIndex >= filteredEventFiles.Count)
                {
                    _currentSearchIndex = 0;
                }
                SearchDialogue(filteredEventFiles);
            }
        }

        private void SearchDialogue(List<EventFile> filteredEventFiles)
        {
            if (filteredEventFiles.Count > 0)
            {
                EventFile eventFile = filteredEventFiles[_currentSearchIndex];
                eventsListBox.SelectedIndex = _evtFile.Files.IndexOf(eventFile);
                DialogueLine dialogueLine = eventFile.DialogueLines.FirstOrDefault(d => d.Text.Contains(dialogueSearchBox.Text.ToLowerInvariant()));
                foreach (var child in editStackPanel.Children)
                {
                    if (child.GetType() == typeof(StackPanel))
                    {
                        var childStackPanel = (StackPanel)child;
                        foreach (var grandchild in childStackPanel.Children)
                        {
                            if (grandchild.GetType() == typeof(EventTextBox))
                            {
                                var textBox = (EventTextBox)grandchild;
                                if (textBox.DialogueIndex == eventFile.DialogueLines.IndexOf(dialogueLine))
                                {
                                    textBox.BringIntoView();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CompressFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "All files|*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] decompressedBytes = File.ReadAllBytes(openFileDialog.FileName);
                byte[] compressedBytes = Helpers.CompressData(decompressedBytes);

                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "All files|*"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, compressedBytes);
                }
            }
        }

        private void DecompressFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "All files|*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] compressedBytes = File.ReadAllBytes(openFileDialog.FileName);
                byte[] decompressedBytes = Helpers.DecompressData(compressedBytes);

                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "All files|*"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, decompressedBytes);
                }
            }
        }

        private void OpenGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "GRP file|grp*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _grpFile = ArchiveFile<GraphicsFile>.FromFile(openFileDialog.FileName, _log);
                _grpFile.GetFileByIndex(0xE50).InitializeFontFile(); // initialize the font file
                graphicsStatsStackPanel.Children.Clear();
                graphicsListBox.ItemsSource = _grpFile.Files;
                graphicsListBox.Items.Refresh();

                // Gather stats on the header values for research purposes
                Dictionary<int, Dictionary<ushort, int>> statsDictionaries = new();
                for (int i = 0x04; i < 0x14; i += 2)
                {
                    statsDictionaries.Add(i, new Dictionary<ushort, int>());
                }

                foreach (GraphicsFile file in _grpFile.Files)
                {
                    if (file.Data is null || Encoding.ASCII.GetString(file.Data.Take(4).ToArray()) != "SHTX")
                    {
                        continue;
                    }

                    for (int i = 0x04; i < 0x14; i += 2)
                    {
                        ushort value = BitConverter.ToUInt16(file.Data.Skip(i).Take(2).ToArray());
                        if (statsDictionaries[i].ContainsKey(value))
                        {
                            statsDictionaries[i][value]++;
                        }
                        else
                        {
                            statsDictionaries[i].Add(value, 1);
                        }
                    }
                }

                foreach (int offset in statsDictionaries.Keys)
                {
                    graphicsStatsStackPanel.Children.Add(new TextBlock { Text = $"0x{offset:X2}", FontSize = 16 });

                    if (offset == 4)
                    {
                        foreach (ushort value in statsDictionaries[offset].Keys)
                        {
                            StackPanel statStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                            statStackPanel.Children.Add(new TextBlock { Text = Encoding.ASCII.GetString(BitConverter.GetBytes(value)) });
                            statStackPanel.Children.Add(new TextBlock { Text = $" {statsDictionaries[offset][value]}" });
                            graphicsStatsStackPanel.Children.Add(statStackPanel);
                        }
                    }
                    else
                    {
                        foreach (ushort value in statsDictionaries[offset].Keys)
                        {
                            StackPanel statStackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                            statStackPanel.Children.Add(new TextBlock { Text = string.Concat(BitConverter.GetBytes(value).Select(b => $"{b:X2} ")) });
                            statStackPanel.Children.Add(new TextBlock { Text = $"{statsDictionaries[offset][value]}" });
                            graphicsStatsStackPanel.Children.Add(statStackPanel);
                        }
                    }

                    graphicsStatsStackPanel.Children.Add(new Separator());
                }
            }
        }

        private void SaveGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "GRP file|grp*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _grpFile.GetBytes());
                MessageBox.Show("Save completed!");
            }
        }

        private void ExportGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, ((GraphicsFile)graphicsListBox.SelectedItem).GetBytes());
            }
        }

        private void ImportGraphicsFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                GraphicsFile newGraphicsFile = new();
                byte[] compressedData = File.ReadAllBytes(openFileDialog.FileName);
                newGraphicsFile.CompressedData = compressedData;
                newGraphicsFile.Initialize(compressedData, _grpFile.Files[graphicsListBox.SelectedIndex].Offset, _log);
                newGraphicsFile.Index = _grpFile.Files[graphicsListBox.SelectedIndex].Index;
                newGraphicsFile.Name = _grpFile.Files[graphicsListBox.SelectedIndex].Name;
                _grpFile.Files[graphicsListBox.SelectedIndex] = newGraphicsFile;
                _grpFile.Files[graphicsListBox.SelectedIndex].Edited = true;
                graphicsListBox.Items.Refresh();
            }
        }

        private void ExportGraphicsImageFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphicsListBox.SelectedIndex >= 0)
            {
                GraphicsFile selectedFile = (GraphicsFile)graphicsListBox.SelectedItem;
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "PNG file|*.png",
                    FileName = $"grp_{selectedFile.Index:D4}.png"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    if (selectedFile.FileFunction != GraphicsFile.Function.SCREEN)
                    {
                        SKBitmap bitmap = selectedFile.GetImage(_currentImageWidth);
                        using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                        bitmap.Encode(fileStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                    else
                    {
                        SKBitmap bitmap = selectedFile.GetScreenImage(selectedFile.GetAssociatedScreenTiles(_grpFile));
                        using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                        bitmap.Encode(fileStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
                    }
                }
            }
        }

        private void ImportGraphicsImageFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphicsListBox.SelectedIndex >= 0 && ((((GraphicsFile)graphicsListBox.SelectedItem).PixelData?.Count ?? 0) > 0 || ((GraphicsFile)graphicsListBox.SelectedItem).FileFunction == GraphicsFile.Function.SCREEN))
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PNG file|*.png"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    GraphicsFile selectedFile = (GraphicsFile)graphicsListBox.SelectedItem;
                    GraphicsFile tilesGrp = null;
                    if (selectedFile.FileFunction == GraphicsFile.Function.SCREEN)
                    {
                        tilesGrp = selectedFile.GetAssociatedScreenTiles(_grpFile);
                    }
                    int width = selectedFile.SetImage(openFileDialog.FileName, associatedTiles: tilesGrp);
                    tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
                    tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetImage(width, 0, tilesGrp: tilesGrp)), MaxWidth = 256 });
                    _currentImageWidth = width;
                }
            }
        }

        private void ImportGraphicsImageWithPaletteFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (graphicsListBox.SelectedIndex >= 0 && (((GraphicsFile)graphicsListBox.SelectedItem).PixelData?.Count ?? 0) > 0)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PNG file|*.png"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    GraphicsFile selectedFile = (GraphicsFile)graphicsListBox.SelectedItem;
                    int width = selectedFile.SetImage(openFileDialog.FileName, setPalette: true, transparentIndex: 0);
                    tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
                    tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetImage(width, 0)), MaxWidth = 256 });
                    _currentImageWidth = width;
                }
            }
        }

        private void AddGraphicsImaageFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "PNG file|*.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _grpFile.AddFile(openFileDialog.FileName);
                graphicsListBox.Items.Refresh();
            }
        }

        private void GraphicsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tilesEditStackPanel.Children.Clear();
            paletteEditStackPanel.Children.Clear();
            if (graphicsListBox.SelectedIndex >= 0)
            {
                GraphicsFile selectedFile = (GraphicsFile)graphicsListBox.SelectedItem;
                tilesEditStackPanel.Children.Add(new TextBlock { Text = selectedFile.Name });
                tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.Determinant ?? ""} {selectedFile.Data?.Count ?? 0} bytes" });
                tilesEditStackPanel.Children.Add(new TextBlock { Text = $"Actual compressed length: {selectedFile.CompressedData.Length:X}; Calculated length: {selectedFile.Length:X}" });
                tilesEditStackPanel.Children.Add(new TextBlock { Text = $"Unknown08: {selectedFile.Unknown08}, Tile Width: {selectedFile.RenderWidth}, Tile Height: {selectedFile.RenderHeight}" });
                if (selectedFile.PixelData is not null)
                {
                    ShtxWidthBox graphicsWidthBox = new() { Shtxds = selectedFile, Text = $"{selectedFile.Width}" };
                    graphicsWidthBox.TextChanged += GraphicsWidthBox_TextChanged;
                    tilesEditStackPanel.Children.Add(graphicsWidthBox);
                    tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetImage()), MaxWidth = selectedFile.Width });
                    _currentImageWidth = 256;

                    paletteEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetPalette()), MaxWidth = 256 });
                }
                else if (selectedFile.FileFunction == GraphicsFile.Function.SCREEN)
                {
                    tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(selectedFile.GetScreenImage(selectedFile.GetAssociatedScreenTiles(_grpFile))), MaxWidth = selectedFile.Width });
                }
                else if (selectedFile.FileFunction == GraphicsFile.Function.LAYOUT)
                {
                    TextBox startTextBox = new() { Width = 40 };
                    TextBox lengthTextBox = new() { Width = 40 };
                    GraphicsLayoutCreationButton graphicsLayoutCreationButton = new() { StartTextBox = startTextBox, LengthTextBox = lengthTextBox, Content = "Preview Layout" };
                    graphicsLayoutCreationButton.Click += GraphicsLayoutCreationButton_Click;
                    CheckBox darkModeCheckBox = new() { IsChecked = _layoutDarkMode };
                    darkModeCheckBox.Checked += DarkModeCheckBox_Checked;
                    darkModeCheckBox.Unchecked += DarkModeCheckBox_Unchecked;
                    StackPanel layoutControlsPanel = new() { Orientation = Orientation.Horizontal };
                    layoutControlsPanel.Children.Add(new TextBlock { Text = "Start: " });
                    layoutControlsPanel.Children.Add(startTextBox);
                    layoutControlsPanel.Children.Add(new TextBlock { Text = "Length: " });
                    layoutControlsPanel.Children.Add(lengthTextBox);
                    layoutControlsPanel.Children.Add(graphicsLayoutCreationButton);
                    layoutControlsPanel.Children.Add(darkModeCheckBox);
                    layoutControlsPanel.Children.Add(new TextBlock() { Text = "Dark Mode " });
                    layoutControlsPanel.Children.Add(new TextBlock { Text = $" (Total Entries: {selectedFile.LayoutEntries.Count})" });
                    tilesEditStackPanel.Children.Add(layoutControlsPanel);
                }
                else if (selectedFile.FileFunction == GraphicsFile.Function.ANIMATION)
                {
                    if (selectedFile.AnimationEntries.Count > 0)
                    {
                        if (selectedFile.AnimationEntries[0].GetType() == typeof(PaletteRotateAnimationEntry))
                        {
                            PaletteRotateAnimationEntry firstAnimEntry = (PaletteRotateAnimationEntry)selectedFile.AnimationEntries[0];
                            tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(firstAnimEntry.PaletteOffset)}: {firstAnimEntry.PaletteOffset}" });
                            tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(firstAnimEntry.SwapSize)}: {firstAnimEntry.SwapSize}" });
                            tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(firstAnimEntry.SwapAreaSize)}: {firstAnimEntry.SwapAreaSize}" });
                            tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(firstAnimEntry.FramesPerTick)}: {firstAnimEntry.FramesPerTick}" });
                            tilesEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(firstAnimEntry.AnimationType)}: {firstAnimEntry.AnimationType}" });
                        }
                        else if (selectedFile.AnimationEntries[0].GetType() == typeof(FrameAnimationEntry))
                        {
                            List<GraphicsFile> animationFrames = selectedFile.GetAnimationFrames(_grpFile.GetFileByIndex(selectedFile.Index + 1));
                            foreach (GraphicsFile animationFrame in animationFrames)
                            {
                                tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(animationFrame.GetImage()), MaxWidth = animationFrame.Width });
                            }
                        }
                    }
                }
            }
        }

        private void GraphicsLayoutCreationButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsLayoutCreationButton graphicsLayoutCreationButton = (GraphicsLayoutCreationButton)sender;
            if (graphicsLayoutCreationButton.CreatedLayout)
            {
                tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
                tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
                tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
                tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
            }
            else
            {
                graphicsLayoutCreationButton.CreatedLayout = true;
            }
            int startIndex = int.Parse(graphicsLayoutCreationButton.StartTextBox.Text);
            int length = int.Parse(graphicsLayoutCreationButton.LengthTextBox.Text);
            (SKBitmap bitmap, List<LayoutEntry> entries) = ((GraphicsFile)graphicsListBox.SelectedItem).GetLayout(_grpFile.Files, startIndex, length, _layoutDarkMode);
            tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(bitmap), MaxWidth = 640 });
            StackPanel layoutEntriesStackPanel = new();
            int i = startIndex;
            foreach (LayoutEntry entry in entries)
            {
                LayoutEntryStackPanel layoutEntryStackPanel = new(entry, i);
                layoutEntriesStackPanel.Children.Add(layoutEntryStackPanel);
                i++;
            }
            tilesEditStackPanel.Children.Add(layoutEntriesStackPanel);
            GraphicsLayoutRegenerateButton regenerateButton = new() { Content = "Regenerate Layout", LayoutEntries = entries };
            regenerateButton.Click += RegenerateButton_Click;
            tilesEditStackPanel.Children.Add(regenerateButton);
            tilesEditStackPanel.Children.Add(new StackPanel() { Height = 50 });
        }

        private void RegenerateButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsLayoutRegenerateButton button = (GraphicsLayoutRegenerateButton)sender;
            tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 4);
            (SKBitmap bitmap, List<LayoutEntry> _) = ((GraphicsFile)graphicsListBox.SelectedItem).GetLayout(_grpFile.Files, button.LayoutEntries, _layoutDarkMode);
            tilesEditStackPanel.Children.Insert(tilesEditStackPanel.Children.Count - 3, new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(bitmap), MaxWidth = 640 });
        }

        private void DarkModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _layoutDarkMode = true;
        }

        private void DarkModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _layoutDarkMode = false;
        }

        private void GraphicsWidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            tilesEditStackPanel.Children.RemoveAt(tilesEditStackPanel.Children.Count - 1);
            ShtxWidthBox widthBox = (ShtxWidthBox)sender;
            bool successfulParse = int.TryParse(widthBox.Text, out int width);
            if (!successfulParse)
            {
                width = 256;
            }
            tilesEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(widthBox.Shtxds.GetImage(width)), MaxWidth = 256 });
            _currentImageWidth = width;
        }

        private void OpenDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "DAT file|dat*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _datFile = ArchiveFile<DataFile>.FromFile(openFileDialog.FileName, _log);
                if (openFileDialog.FileName.Contains("dat.bin", StringComparison.OrdinalIgnoreCase))
                {
                    List<byte> qmapData = _datFile.GetFileByName("QMAPS").Data;
                    List<string> mapFileNames = new();
                    for (int i = 0; i < BitConverter.ToInt32(qmapData.Skip(0x10).Take(4).ToArray()); i++)
                    {
                        mapFileNames.Add(Encoding.ASCII.GetString(qmapData.Skip(BitConverter.ToInt32(qmapData.Skip(0x14 + i * 8).Take(4).ToArray())).TakeWhile(b => b != 0).ToArray()).Replace(".", ""));
                    }

                    for (int i = 1; i < _datFile.Files.Count + 1; i++)
                    {
                        if (mapFileNames.Contains(_datFile.Files[i - 1].Name))
                        {
                            DataFile oldMapFile = _datFile.GetFileByIndex(i);
                            MapFile mapFile = _datFile.GetFileByIndex(i).CastTo<MapFile>();
                            _datFile.Files[_datFile.Files.IndexOf(oldMapFile)] = mapFile;
                        }
                    }
                    for (int i = 0x8E; i <= 0x97; i++)
                    {
                        DataFile oldPuzzleFile = _datFile.GetFileByIndex(i);
                        PuzzleFile puzzleFile = oldPuzzleFile.CastTo<PuzzleFile>();
                        _datFile.Files[_datFile.Files.IndexOf(oldPuzzleFile)] = puzzleFile;
                    }
                    DataFile sysTexFile = _datFile.GetFileByIndex(0x9B);
                    _datFile.Files[_datFile.Files.IndexOf(sysTexFile)] = sysTexFile.CastTo<SystemTextureFile>();
                }
                dataListBox.ItemsSource = _datFile.Files;
                dataListBox.Items.Refresh();
            }
        }

        private void SaveDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "DAT file|dat*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, _datFile.GetBytes());
                MessageBox.Show("Save completed!");
            }
        }

        private void ExportDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, ((DataFile)dataListBox.SelectedItem).GetBytes());
            }
        }

        private void ImportDataFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "BIN file|*.bin"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                DataFile newDataFile = new();
                newDataFile.Initialize(File.ReadAllBytes(openFileDialog.FileName), _datFile.Files[dataListBox.SelectedIndex].Offset, _log);
                newDataFile.Index = _datFile.Files[dataListBox.SelectedIndex].Index;
                newDataFile.Name = _datFile.Files[dataListBox.SelectedIndex].Name;
                newDataFile.Offset = _datFile.Files[dataListBox.SelectedIndex].Offset;
                _datFile.Files[dataListBox.SelectedIndex] = newDataFile;
                _datFile.Files[dataListBox.SelectedIndex].Edited = true;
                graphicsListBox.Items.Refresh();
            }
        }

        private void DataListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataEditStackPanel.Children.Clear();
            if (dataListBox.SelectedIndex >= 0)
            {
                DataFile selectedFile = (DataFile)dataListBox.SelectedItem;
                dataEditStackPanel.Children.Add(new TextBlock { Text = $"{selectedFile.Data.Count} bytes" });
                dataEditStackPanel.Children.Add(new TextBlock { Text = $"Actual compressed length: {selectedFile.CompressedData.Length:X}; Calculated length: {selectedFile.Length:X}" });

                if (selectedFile.GetType() == typeof(PuzzleFile))
                {
                    PuzzleFile puzzle = (PuzzleFile)selectedFile;
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"Map: " +
                        $"{puzzle.Settings.GetMapName(_datFile.GetFileByName("QMAPS").Data)} " +
                        $"({puzzle.Settings.MapId})" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.BaseTime)}: {puzzle.Settings.BaseTime}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.NumSingularities)}: {puzzle.Settings.NumSingularities}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.Unknown04)}: {puzzle.Settings.Unknown04}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.TargetNumber)}: {puzzle.Settings.TargetNumber}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.ContinueOnFailure)}: {puzzle.Settings.ContinueOnFailure}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.AccompanyingCharacter)}: {puzzle.Settings.AccompanyingCharacter}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.PowerCharacter1)}: {puzzle.Settings.PowerCharacter1}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.PowerCharacter2)}: {puzzle.Settings.PowerCharacter2}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.SingularityTexture)}: {puzzle.Settings.SingularityTexture}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.SingularityLayout)}: {puzzle.Settings.SingularityLayout}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.SingularityAnim1)}: {puzzle.Settings.SingularityAnim1}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.SingularityAnim2)}: {puzzle.Settings.SingularityAnim2}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.TopicSet)}: {puzzle.Settings.TopicSet}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.Unknown15)}: {puzzle.Settings.Unknown15}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.Unknown16)}: {puzzle.Settings.Unknown16}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(puzzle.Settings.Unknown17)}: {puzzle.Settings.Unknown17}" });
                    foreach (PuzzleHaruhiRoute haruhiRoute in puzzle.HaruhiRoutes)
                    {
                        dataEditStackPanel.Children.Add(new TextBlock { Text = haruhiRoute.ToString() });
                    }
                    GraphicsFile singularityLayout = _grpFile.GetFileByIndex(puzzle.Settings.SingularityLayout);
                    GraphicsFile singularityTexture = _grpFile.GetFileByIndex(puzzle.Settings.SingularityTexture);
                    SKBitmap singularityImage = singularityLayout.GetLayout(
                            new List<GraphicsFile>() { singularityTexture },
                            0,
                            singularityLayout.LayoutEntries.Count,
                            false,
                            true).bitmap;
                    dataEditStackPanel.Children.Add(new Image
                    {
                        Source = GuiHelpers.GetBitmapImageFromBitmap(singularityImage),
                        Width = singularityImage.Width,
                        Height = singularityImage.Height
                    });
                }
                else if (selectedFile.GetType() == typeof(MapFile))
                {
                    MapFile map = (MapFile)selectedFile;

                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.SlgMode)}: {map.Settings.SlgMode}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.MapWidth)}: {map.Settings.MapWidth}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.MapHeight)}: {map.Settings.MapHeight}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.TextureFileIndices)}: {string.Join(", ", map.Settings.TextureFileIndices.Select(i => $"0x{i:X3}"))}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutFileIndex)}: 0x{map.Settings.LayoutFileIndex:X3} ({_grpFile.GetFileByIndex(map.Settings.LayoutFileIndex).LayoutEntries.Count} entries)" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutBgLayerStartIndex)}: {map.Settings.LayoutBgLayerStartIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.NumBgLayerDefinitions)}: {map.Settings.NumBgLayerDefinitions}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutBgLayerEndIndex)}: {map.Settings.LayoutBgLayerEndIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutOcclusionLayerStartIndex)}: {map.Settings.LayoutOcclusionLayerStartIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutOcclusionLayerEndIndex)}: {map.Settings.LayoutOcclusionLayerEndIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.LayoutBoundsIndex)}: {map.Settings.LayoutBoundsIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.ScrollingBgLayoutStartIndex)}: {map.Settings.ScrollingBgLayoutStartIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.ScrollingBgLayoutEndIndex)}: {map.Settings.ScrollingBgLayoutEndIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.TransformMode)}: {map.Settings.TransformMode}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.TopGradient)}: {map.Settings.TopGradient}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.BottomGradient)}: {map.Settings.BottomGradient}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.ScrollingBgDefinitionLayoutIndex)}: {map.Settings.ScrollingBgDefinitionLayoutIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.IntroCameraTruckingDefsStartIndex)}: {map.Settings.IntroCameraTruckingDefsStartIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.IntroCameraTruckingDefsEndIndex)}: {map.Settings.IntroCameraTruckingDefsEndIndex}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.StartingPosition)}: {map.Settings.StartingPosition}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.ColorAnimationFileIndex)}: 0x{map.Settings.ColorAnimationFileIndex:X3}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.PaletteAnimationFileIndex)}: 0x{map.Settings.PaletteAnimationFileIndex:X3}" });
                    dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(map.Settings.Unknown5C)}: {map.Settings.Unknown5C}" });

                    SKBitmap bgGradient = map.GetBackgroundGradient();
                    dataEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(bgGradient), Width = bgGradient.Width });

                    SKBitmap pathingImage = map.GetPathingImage();
                    dataEditStackPanel.Children.Add(new Image { Source = GuiHelpers.GetBitmapImageFromBitmap(pathingImage), Width = pathingImage.Width });

                    StackPanel displayLayoutStackPanel = new() { Orientation = Orientation.Horizontal };
                    TextBox startBox = new() { Width = 135 };
                    TextBox lengthBox = new() { Width = 135 };
                    GraphicsLayoutCreationButton displayLayoutButton = new() { StartTextBox = startBox, LengthTextBox = lengthBox, Content = "Display Layout" };
                    displayLayoutButton.Click += DisplayLayoutButton_Click;
                    displayLayoutStackPanel.Children.Add(startBox);
                    displayLayoutStackPanel.Children.Add(lengthBox);
                    displayLayoutStackPanel.Children.Add(displayLayoutButton);
                    dataEditStackPanel.Children.Add(displayLayoutStackPanel);
                }
                else if (selectedFile.GetType() == typeof(SystemTextureFile))
                {
                    SystemTextureFile sysTexFile = (SystemTextureFile)selectedFile;

                    foreach (SystemTexture sysTex in sysTexFile.SystemTextures)
                    {
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Screen)}: {sysTex.Screen}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.GrpIndex)}: {_grpFile?.GetFileByIndex(sysTex.GrpIndex)?.Name ?? $"{sysTex.GrpIndex}"} (0x{sysTex.GrpIndex:X3})" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Tpage)}: {sysTex.Tpage}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.PaletteNumber)}: {sysTex.PaletteNumber}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.ValidateTex)}: {sysTex.ValidateTex}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.LoadMethod)}: {sysTex.LoadMethod}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown0E)}: {sysTex.Unknown0E}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.MaxVram)}: {sysTex.MaxVram}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown12)}: {sysTex.Unknown12}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown14)}: {sysTex.Unknown14}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown16)}: {sysTex.Unknown16}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.AnimationIndex)}: {_grpFile.GetFileByIndex(sysTex.AnimationIndex)?.Name ?? $"{sysTex.AnimationIndex}"}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.TileWidth)}: {sysTex.TileWidth}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.TileHeight)}: {sysTex.TileHeight}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown1E)}: {sysTex.Unknown1E}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown20)}: {sysTex.Unknown20}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown22)}: {sysTex.Unknown22}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown24)}: {sysTex.Unknown24}" });
                        dataEditStackPanel.Children.Add(new TextBlock { Text = $"{nameof(sysTex.Unknown28)}: {sysTex.Unknown28}" });
                        dataEditStackPanel.Children.Add(new Separator());
                    }
                }
            }
        }

        private void DisplayLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            GraphicsLayoutCreationButton button = (GraphicsLayoutCreationButton)sender;
            MapFile map = (MapFile)dataListBox.SelectedItem;
            GraphicsFile layout = _grpFile.GetFileByIndex(map.Settings.LayoutFileIndex);

            if (string.IsNullOrWhiteSpace(button.StartTextBox.Text) || string.IsNullOrWhiteSpace(button.LengthTextBox.Text))
            {
                (SKBitmap mapBitmap, SKBitmap bgBitmap) = map.GetMapImages(_grpFile);
                BitmapButton mapBitmapButton = new() { Bitmap = mapBitmap, Content = "Save Image" };
                BitmapButton bgBitmapButton = new() { Bitmap = bgBitmap, Content = "Save Image" };
                mapBitmapButton.Click += BitmapButton_Click;
                bgBitmapButton.Click += BitmapButton_Click;
                dataEditStackPanel.Children.Add(mapBitmapButton);
                dataEditStackPanel.Children.Add(new Image
                {
                    Source = GuiHelpers.GetBitmapImageFromBitmap(mapBitmap),
                    Width = layout.Width,
                    Height = layout.Height,
                });
                if (bgBitmap is not null)
                {
                    dataEditStackPanel.Children.Add(bgBitmapButton);
                    dataEditStackPanel.Children.Add(new Image
                    {
                        Source = GuiHelpers.GetBitmapImageFromBitmap(bgBitmap),
                        Width = layout.Width,
                        Height = layout.Height,
                    });
                }
            }
            else
            {
                SKBitmap mapBitmap = map.GetMapImages(_grpFile, int.Parse(button.StartTextBox.Text), int.Parse(button.LengthTextBox.Text));
                dataEditStackPanel.Children.Add(new Image
                {
                    Source = GuiHelpers.GetBitmapImageFromBitmap(mapBitmap),
                    Width = layout.Width,
                    Height = layout.Height
                });
            }
        }

        private void BitmapButton_Click(object sender, RoutedEventArgs e)
        {
            BitmapButton button = (BitmapButton)sender;
            SaveFileDialog saveFileDialog = new() { Filter = "PNG image|*.png" };
            if (saveFileDialog.ShowDialog() == true)
            {
                using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                button.Bitmap.Encode(fileStream, SKEncodedImageFormat.Png, GraphicsFile.PNG_QUALITY);
            }
        }
    }
}
