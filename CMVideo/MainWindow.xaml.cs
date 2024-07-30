using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibVLCSharp.WPF;
using Microsoft.Win32;

namespace CMVideo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string file_path = null;
        public MainWindow()
        {
            InitializeComponent();
            ExampleButton.Click += ExampleButton_Click;
        }

        private void ExampleButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Player(file_path);
            window.Show();
        }

        private void File_Button_Click(object sender, RoutedEventArgs e)
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            int index_char_length = 1;
            char index_char = '\\';
            int substring_index = userName.IndexOf(index_char) + index_char_length;
            int length = userName.Length - substring_index;//This will yield a length of 2 since the start index is at 6 and the length is 8.
            string user = userName.Substring(substring_index, length);
            OpenFileDialog fd = new OpenFileDialog();
            fd.DefaultExt = "*.*";
            fd.InitialDirectory = "C:\\Users\\" + user + "\\Videos";
            
            bool? success = fd.ShowDialog();

            if (success == true)
            {
                file_path = fd.FileName;
                Console.WriteLine("File path is : " + file_path);
                Console.WriteLine("File extension is: " + System.IO.Path.GetExtension(fd.FileName));
            }
            else
            {
                // do nothing
            }
        }
    }
}
