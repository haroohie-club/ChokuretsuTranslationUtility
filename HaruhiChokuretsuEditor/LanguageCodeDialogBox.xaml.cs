using System.Windows;

namespace HaruhiChokuretsuEditor
{
    /// <summary>
    /// Interaction logic for LanguageCodeDialogBox.xaml
    /// </summary>
    public partial class LanguageCodeDialogBox : Window
    {
        public string LanguageCode { get; set; }

        public LanguageCodeDialogBox()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            LanguageCode = languageCodeTextBox.Text;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
