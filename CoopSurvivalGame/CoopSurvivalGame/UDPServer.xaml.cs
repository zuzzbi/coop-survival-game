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
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Threading;

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

            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            canvas.Focus();
        }

        private Socket _socketForReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket _socketForSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        DispatcherTimer gameTimer = new DispatcherTimer();
        List<Rectangle> shotsActive = new List<Rectangle>();
        List<Rectangle> shotsToRemove = new List<Rectangle>();
        bool keyA = false;
        bool keyW = false;
        bool keyS = false;
        bool keyD = false;
        bool keyUp = false;
        bool keyDown = false;
        bool keyLeft = false;
        bool keyRight = false;
        int shotCounterServer = 0;

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

                Dispatcher.Invoke(new Action(() =>
                {
                    ChangePosition(rec);
                }));
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

        private void CreateShot(Key key, int positionTop, int positionLeft)
        {
            Rectangle shot = new Rectangle();
            shot.Width = 5;
            shot.Height = 5;
            shot.Fill = System.Windows.Media.Brushes.Cyan;
            shot.Name = "s"+shotCounterServer.ToString();
            shotCounterServer++;
            switch (key)
            {
                case Key.Up:
                    Canvas.SetTop(shot, positionTop);
                    Canvas.SetLeft(shot, positionLeft);
                    shot.Tag = "shotUp";
                    break;
                case Key.Down:
                    Canvas.SetTop(shot, positionTop - shot.Height);
                    Canvas.SetLeft(shot, positionLeft);
                    shot.Tag = "shotDown";
                    break;
                case Key.Right:
                    Canvas.SetTop(shot, positionTop);
                    Canvas.SetLeft(shot, positionLeft);
                    shot.Tag = "shotRight";
                    break;
                case Key.Left:
                    Canvas.SetTop(shot, positionTop);
                    Canvas.SetLeft(shot, positionLeft - shot.Width);
                    shot.Tag = "shotLeft";
                    break;
                default:
                    break;
            }
            canvas.Children.Add(shot);
            shotsActive.Add(shot);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            foreach (var item in canvas.Children.OfType<Rectangle>())
            {
                if (item is Rectangle && (string)item.Tag == "shotUp")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) - 7);
                    if (Canvas.GetTop(item) < -5)
                    {
                        shotsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotDown")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) + 7);
                    if (Canvas.GetTop(item) > canvas.ActualHeight + 5)
                    {
                        shotsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotRight")
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) + 7);
                    if (Canvas.GetLeft(item) > canvas.ActualWidth + 5)
                    {
                        shotsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotLeft")
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) - 7);
                    if (Canvas.GetLeft(item) < -5)
                    {
                        shotsToRemove.Add(item);
                    }
                }
            }

            foreach (Rectangle item in shotsToRemove)
            {
                canvas.Children.Remove(item);
                shotsActive.Remove(item);
            }

            if (keyA) 
            {
                Canvas.SetLeft(player1, Canvas.GetLeft(player1) - 5);
            }
            if (keyW)
            {
                Canvas.SetTop(player1, Canvas.GetTop(player1) - 5);
            }
            if (keyS)
            {
                Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
            }
            if (keyD)
            {
                Canvas.SetLeft(player1, Canvas.GetLeft(player1) + 5);
            }
            if (keyUp)
            {
                CreateShot(Key.Up, Convert.ToInt32(Canvas.GetTop(player1)), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                keyUp = false;
            }
            if (keyDown)
            {
                CreateShot(Key.Down, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                keyDown = false;
            }
            if (keyRight)
            {
                CreateShot(Key.Right, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width));
                keyRight = false;
            }
            if (keyLeft)
            {
                CreateShot(Key.Left, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1)));
                keyLeft = false;
            }
            Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            foreach (var item in shotsActive)
            {
                Send("shot," + Canvas.GetTop(item).ToString() + "," + Canvas.GetLeft(item).ToString() + "," + item.Name);
                //Send("shot," + Canvas.GetTop(item).ToString() + "," + Canvas.GetLeft(item).ToString());

            }

        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    keyA = true;
                    //Canvas.SetLeft(player1, Canvas.GetLeft(player1) - 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.W:
                    keyW=true;
                    //Canvas.SetTop(player1, Canvas.GetTop(player1) - 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.S:
                    keyS = true;
                    //Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.D:
                    keyD = true;
                    //Canvas.SetLeft(player1, Canvas.GetLeft(player1) + 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.Up:
                    if (!e.IsRepeat)
                        keyUp =true;
                    //CreateShot(Key.Up, Convert.ToInt32(Canvas.GetTop(player1)), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                    break;
                case Key.Down:
                    if (!e.IsRepeat)
                        keyDown =true;
                    //CreateShot(Key.Down, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                    break;
                case Key.Right:
                    if (!e.IsRepeat)
                        keyRight = true;
                    //CreateShot(Key.Right, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width));
                    break;
                case Key.Left:
                    if (!e.IsRepeat)
                        keyLeft = true;
                    //CreateShot(Key.Left, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1)));
                    break;
                default:

                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    keyA = false;
                    //Canvas.SetLeft(player1, Canvas.GetLeft(player1) - 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.W:
                    keyW = false;
                    //Canvas.SetTop(player1, Canvas.GetTop(player1) - 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.S:
                    keyS = false;
                    //Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.D:
                    keyD = false;
                    //Canvas.SetLeft(player1, Canvas.GetLeft(player1) + 5);
                    //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
                    break;
                case Key.Up:
                    keyUp = false;
                    //CreateShot(Key.Up, Convert.ToInt32(Canvas.GetTop(player1)), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                    break;
                case Key.Down:
                    keyDown = false;
                    //CreateShot(Key.Down, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2));
                    break;
                case Key.Right:
                    keyRight = false;
                    //CreateShot(Key.Right, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width));
                    break;
                case Key.Left:
                    keyLeft = false;
                    //CreateShot(Key.Left, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1)));
                    break;
                default:

                    break;
            }

        }
    }
}
