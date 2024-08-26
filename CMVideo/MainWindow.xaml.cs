using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace CMVideo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string file_path = null;
        string last_path = null;
        readonly List<string> filenames = new List<string>();

        public MainWindow()
        {
            InitializeComponent ();
            ExampleButton.Click += ExampleButton_Click;
            
        }

        private void ExampleButton_Click(object sender, RoutedEventArgs e)
        {

            //var window = new Player(file_path);
          //  window.Show();
        }



        private void Multiplay_Click(object sender, RoutedEventArgs e)
        {
           var file_names = Get_filenames(sender, e);
           var window = new Player(file_names);
            window.Show();
        }

        private void File_Button_Click(object sender, RoutedEventArgs e)
        {
          Get_filenames(sender, e);
        }



        /**
         * Get the filenames of the videos you want to play
         */
        private List<string> Get_filenames(object sender, RoutedEventArgs e)
        {
            string userName = Get_Username();

            OpenFileDialog fd = new OpenFileDialog
            {
                Multiselect = true,
                DefaultExt = "*.*",
                InitialDirectory = "C:\\Users\\" + userName + "\\Videos"
            };

            
            bool? success = fd.ShowDialog();

            if (success == true)
            {
                file_path = fd.FileName;

              
                last_path = System.IO.Path.GetFullPath(file_path);
               
                foreach (string file in fd.FileNames)
                {
    
                   filenames.Add(file);
                }

                return filenames;
            }

            return filenames;
        }
        
        /**
         * Gets the path of the file to play. File browser beings in ../User/Videos
         */
        private string Get_path(object sender, RoutedEventArgs e)
        {

            if (last_path == null)
            {
                string userName = Get_Username();
                OpenFileDialog fd = new OpenFileDialog
                {
                    Multiselect = true,
                    DefaultExt = "*.*",
                    InitialDirectory = "C:\\Users\\" + userName + "\\Videos"
                };

                bool? success = fd.ShowDialog();

                if (success == true)
                {
                    file_path = fd.FileName;
               
                    Console.WriteLine("File path is : " + file_path);
                    Console.WriteLine("File extension is: " + System.IO.Path.GetExtension(fd.FileName));
                    last_path = System.IO.Path.GetFullPath(file_path);
                }
                else
                {
                    // do nothing
                }

   
            }
            else
            {
                OpenFileDialog fd = new OpenFileDialog();
                fd.DefaultExt = "*.*";
                fd.InitialDirectory = last_path;
                bool? success = fd.ShowDialog();

                if (success == true)
                {
                    file_path = fd.FileName;

                    Console.WriteLine("File path is : " + file_path);
                    Console.WriteLine("File extension is: " + System.IO.Path.GetExtension(fd.FileName));
                    last_path = System.IO.Path.GetPathRoot(file_path);
                }
            }
            return "";
        }


        private string Get_Username()
        {
            try
            {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                int index_char_length = 1;
                char index_char = '\\';
                int substring_index = userName.IndexOf(index_char) + index_char_length;
                int length = userName.Length - substring_index;//This will yield a length of 2 since the start index is at 6 and the length is 8.
                string user = userName.Substring(substring_index, length);

                return user;
            }
            catch
            {
                Console.WriteLine("Unable to get username");
            }
            return "";
        }

    }
      

}
 
