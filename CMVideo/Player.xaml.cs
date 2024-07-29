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
        public Player()
        {
            InitializeComponent();

            _controls = new Controls(this);

            VideoView.Content = _controls;
        }

        public Player(string file_path)
        {
            InitializeComponent();

            _controls = new Controls(this, file_path);
            VideoView.Content = _controls;
        }

        protected override void OnClosed(EventArgs e)
        {
            VideoView.Dispose();
        }
    }
}
