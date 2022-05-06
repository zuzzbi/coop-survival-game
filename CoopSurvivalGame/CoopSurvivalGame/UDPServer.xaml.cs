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
using System.Net;
using System.Net.Sockets;
using System.Drawing;

namespace CoopSurvivalGame
{
    /// <summary>
    /// Logika interakcji dla klasy UDPServer.xaml
    /// </summary>
    public partial class UDPServer : Window
    {
        public UDPServer()
        {
            InitializeComponent();
            canvas.Focus();
        }

        private Socket _socketForReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket _socketForSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(string address, int portForReceive, int portForSend)
        {
            _socketForReceive.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socketForReceive.Bind(new IPEndPoint(IPAddress.Parse(address), portForReceive));

            _socketForSend.Connect(IPAddress.Parse(address), portForSend);
            


            Receive();
        }

        private void Receive()
        {
            _socketForReceive.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socketForReceive.EndReceiveFrom(ar, ref epFrom);
                _socketForReceive.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                string rec = Encoding.ASCII.GetString(so.buffer, 0, bytes);
                
                Dispatcher.Invoke( new Action( () => {
                    ChangePosition(rec);
                } ) );
            }, state);

        }

        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socketForSend.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socketForSend.EndSend(ar);
            }, state);
        }

        private void ChangePosition(string position)
        {
            string playerName = position.Split(',')[0];
            int positionFromTop = Convert.ToInt32(position.Split(',')[1]);
            int positionFromLeft = Convert.ToInt32(position.Split(',')[2]);

            Canvas.SetLeft(player2, positionFromLeft);
            Canvas.SetTop(player2, positionFromTop);

            Send("player2," + Canvas.GetTop(player2).ToString() + "," + Canvas.GetLeft(player2).ToString());
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    Canvas.SetTop(player1, Canvas.GetLeft(player1) - 5);
                    Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.W:
                    Canvas.SetTop(player1, Canvas.GetTop(player1) - 5);
                    Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.S:
                    Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
                    Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.D:
                    Canvas.SetTop(player1, Canvas.GetLeft(player1) + 5);
                    Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;

                default:

                    break;
            }
        }
    }
}
