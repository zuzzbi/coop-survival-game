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
using System.Net.Sockets;
using System.Net;

namespace CoopSurvivalGame
{
    /// <summary>
    /// Logika interakcji dla klasy Game.xaml
    /// </summary>
    /// 
    public class Player { }
    public partial class Game : Window
    {
        public Game(bool isHost, string ip = null)
        {
            InitializeComponent();
            canvas.Focus();
            this.isHost = isHost;
            if (isHost)
            {
                window.Title = "HOST";
                Canvas.SetLeft(player1, 1);
                Canvas.SetTop(player1, 1);
                Canvas.SetLeft(player2, 1);
                Canvas.SetTop(player2, 100);
                server = new TcpListener(IPAddress.Any, 5000);
                server.Start();
                sock = server.AcceptSocket();
            }
            else
            {
                window.Title = "KLIENT";
                Canvas.SetLeft(player1, 1);
                Canvas.SetTop(player1, 1);
                Canvas.SetLeft(player2, 1);
                Canvas.SetTop(player2, 100);
                client = new TcpClient(ip, 5000);
                sock = client.Client;
            }
        }
        private Socket sock;
        private TcpListener server = null;
        private TcpClient client;
        bool isHost;

        private void Listen()
        {
            
        }

        private void onkeydown(object sender, KeyEventArgs e)
        {
            if (isHost)
            {
                if (e.Key == Key.A)
                {
                    Canvas.SetLeft(player1, Canvas.GetLeft(player1) - 5);
                    try
                    {
                        byte[] buffer = new byte[1];
                        sock.Receive(buffer);
                        if (buffer[0] == 1)
                        {
                            Canvas.SetLeft(player2, Canvas.GetLeft(player2) - 5);
                        }
                        if (buffer[0] == 2)
                        {
                            Canvas.SetLeft(player2, Canvas.GetLeft(player2) + 5);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                if (e.Key == Key.D)
                {
                    Canvas.SetLeft(player1, Canvas.GetLeft(player1) + 5);
                }
                if (e.Key == Key.W)
                {
                    Canvas.SetTop(player1, Canvas.GetTop(player1) - 5);
                }
                if (e.Key == Key.S)
                {
                    Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
                }
            }
            else
            {
                if (e.Key == Key.A)
                {
                    Canvas.SetLeft(player2, Canvas.GetLeft(player2) - 5);
                    byte[] buffer = { 1 };
                    sock.Send(buffer);
                }
                if (e.Key == Key.D)
                {
                    Canvas.SetLeft(player2, Canvas.GetLeft(player2) + 5);
                    byte[] buffer = { 2 };
                    sock.Send(buffer);
                }
                if (e.Key == Key.W)
                {
                    Canvas.SetTop(player2, Canvas.GetTop(player2) - 5);
                }
                if (e.Key == Key.S)
                {
                    Canvas.SetTop(player2, Canvas.GetTop(player2) + 5);
                }
            }
        }
    }
}
