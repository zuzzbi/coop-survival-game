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
        public MainWindow()
        {
            InitializeComponent();
            
            
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UDPServer server = new UDPServer();
            server.Show();
            server.Server("127.0.0.1", 27000);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UDPClient client= new UDPClient();
            client.Show();
            client.Client("127.0.0.1", 27000);
        }
    }
}
