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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;

namespace CoopSurvivalGame
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UDPServer server;

        public MainWindow()
        {
            InitializeComponent();

            
            
        }

        private void joinHost(object sender, RoutedEventArgs e)
        {
            string name = playerName.Text;
            this.server = new UDPServer();
            this.server.Show();
            this.server.Server("127.0.0.1", 27000, 50000);
            this.server.addPlayer(name);
            buttonHost.IsEnabled = false;
            buttonJoin.IsEnabled = true;
        }

        private void joinClient(object sender, RoutedEventArgs e)
        {
            string name = playerName.Text;
            if (this.server.getPlayer(name) == null) 
            {
                UDPClient client = new UDPClient(name);
                client.Show();
                client.Client("127.0.0.1", 50000, 27000);
                client.addPlayer(name);
                buttonJoin.IsEnabled = false;
            }
        }
    }
}
