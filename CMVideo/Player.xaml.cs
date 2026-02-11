using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CMVideo
{
    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    public partial class Player : Window
    {
        readonly Controls _controls;

        /// <summary>
        /// Constructor for playing a single video file
        /// </summary>
        public Player(string filePath) : this(new List<string> { filePath })
        {
        }

        /// <summary>
        /// Constructor for playing a playlist of video files
        /// </summary>
        public Player(List<string> files)
        {
            InitializeComponent();

            _controls = new Controls(this, files);
            VideoView.Content = _controls;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
          var window = Window.GetWindow(this);
          window.Activate();
          window.Show();
          window.Focus();
          window.Topmost = true;
          window.KeyDown += HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Space: 
                    _controls.PauseButton_Click(sender, e);
                    break;
                case Key.Left:
                    _controls.Rewind10_Click(sender, e);
                    break;
                case Key.Right:
                    _controls.Forward10_Click(sender, e);
                    break;
                case Key.F:
                    _controls.meme(sender, e);
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            VideoView.Dispose();
            
        }
    }
}
