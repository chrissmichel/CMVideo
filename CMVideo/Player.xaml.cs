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
        readonly MainWindow _mainWindow;
        
        public Player(string file_path)
        {
            InitializeComponent();

            _controls = new Controls(this, file_path);
            VideoView.Content = _controls;
            VideoView.Content = _mainWindow;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
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
