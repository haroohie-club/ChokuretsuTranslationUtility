namespace HaruhiChokuretsuEditor
{
    /// <summary>
    /// Interaction logic for TopicDialogueBox.xaml
    /// </summary>
    public partial class TopicDialogBox : Window
    {
        public int? FinalFileIndex { get; set; }

        public TopicDialogBox()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if (int.TryParse(finalFileIndexTextBox.Text, out int finalFileIndex))
            {
                FinalFileIndex = finalFileIndex;
            }
            else
            {
                FinalFileIndex = null;
                MessageBox.Show($"Failed to parse '{finalFileIndexTextBox.Text}' as an integer!");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}