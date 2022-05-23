using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Diagnostics;

namespace CoopSurvivalGame
{
    /// <summary>
    /// Logika interakcji dla klasy UDPClient.xaml
    /// </summary>
    public partial class UDPClient : Window
    {
        public UDPClient()
        {
            InitializeComponent();
            Score1.Text = "0";
            Score2.Text = "0";
            stopwatch.Start();
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
        DispatcherTimer gameTimer = new DispatcherTimer();
        private AsyncCallback recv = null;
        List<Rectangle> shotsActive = new List<Rectangle>();
        List<Rectangle> itemsToRemove = new List<Rectangle>();
        bool keyA = false;
        bool keyW = false;
        bool keyS = false;
        bool keyD = false;
        Stopwatch stopwatch = new Stopwatch();
        int shotDelay = 500;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Client(string address, int portForReceive, int portForSend)
        {
            _socketForSend.Connect(IPAddress.Parse(address), portForSend);

            _socketForReceive.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            _socketForReceive.Bind(new IPEndPoint(IPAddress.Parse(address), portForReceive));

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
                    Thread t = new Thread(() => this.ChangePosition(rec));
                    t.Start();
                        //ChangePosition(rec);
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

        public void GameLoop(object sender, EventArgs e)
        {
            foreach (Rectangle item in shotsActive)
            {
                if (item.Name.Contains("Up"))
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) - 7);
                    if (Canvas.GetTop(item) < -5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                if (item.Name.Contains("Down"))
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) + 7);
                    if (Canvas.GetTop(item) > canvas.ActualHeight + 5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                if (item.Name.Contains("Right"))
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) + 7);
                    if (Canvas.GetLeft(item) > canvas.ActualWidth + 5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                if (item.Name.Contains("Left"))
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) - 7);
                    if (Canvas.GetLeft(item) < -5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            foreach (Rectangle item in itemsToRemove)
            {
                shotsActive.Remove(item);
                canvas.Children.Remove(item);
            }
            itemsToRemove = new List<Rectangle>();

            if (keyA)
            {
                Canvas.SetLeft(player2, Canvas.GetLeft(player2) - 5);
            }
            if (keyW)
            {
                Canvas.SetTop(player2, Canvas.GetTop(player2) - 5);
            }
            if (keyS)
            {
                Canvas.SetTop(player2, Canvas.GetTop(player2) + 5);
            }
            if (keyD)
            {
                Canvas.SetLeft(player2, Canvas.GetLeft(player2) + 5);
            }
            Send("player2," + Canvas.GetTop(player2).ToString() + "," + Canvas.GetLeft(player2).ToString());
        }

        private void ChangePosition(string position)
        {
            try
            {
                string elementType = position.Split(',')[0];
                int positionFromTop = Convert.ToInt32(position.Split(',')[1]);
                int positionFromLeft = Convert.ToInt32(position.Split(',')[2]);
                if (elementType == "player1")
                {
                    lock (canvas)
                    {
                        lock (player1)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                if (positionFromTop > 0 && positionFromTop < canvas.ActualHeight - player1.Height && positionFromLeft > 0 && positionFromLeft < canvas.ActualWidth - player1.Width)
                                {
                                    Canvas.SetLeft(player1, positionFromLeft);
                                    Canvas.SetTop(player1, positionFromTop);
                                }
                            }));
                        }
                    }
                }
                else if (elementType == "player2")
                {
                    lock (canvas)
                    {
                        lock (player2)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                if (positionFromTop > 0 && positionFromTop < canvas.ActualHeight - player2.Height && positionFromLeft > 0 && positionFromLeft < canvas.ActualWidth - player2.Width)
                                {
                                    Canvas.SetLeft(player2, positionFromLeft);
                                    Canvas.SetTop(player2, positionFromTop);
                                }
                            }));
                        }
                    }
                }
                else if (positionFromLeft == -1 && positionFromTop == -1)
                {
                    lock (canvas)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                var itemToRemove = (from Rectangle item in canvas.Children where elementType.Equals(item.Name) select item).First();
                                canvas.Children.Remove(itemToRemove);
                            }
                            catch (Exception) { }
                        }));
                    }
                }
                else if (elementType.Contains("shot"))
                {
                    lock (canvas)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                Rectangle shot = new Rectangle();
                                shot.Name = elementType;
                                shot.Width = 5;
                                shot.Height = 5;
                                shot.Fill = System.Windows.Media.Brushes.OrangeRed;
                                Canvas.SetTop(shot, positionFromTop);
                                Canvas.SetLeft(shot, positionFromLeft);
                                shotsActive.Add(shot);
                                canvas.Children.Add(shot);
                            }
                            catch (Exception) { }
                        }));
                    }
                }
                else if (elementType.Contains("enemy"))
                {
                    lock (canvas)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                if (positionFromTop >= 0 && positionFromLeft >= 0)
                                {
                                    Rectangle enemy = new Rectangle();
                                    enemy.Name = elementType;
                                    enemy.Width = 30;
                                    enemy.Height = 30;
                                    enemy.Tag = "enemy";
                                    enemy.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/enemy.png")));
                                    Canvas.SetTop(enemy, positionFromTop);
                                    Canvas.SetLeft(enemy, positionFromLeft);
                                    canvas.Children.Add(enemy);
                                }
                            }
                            catch (Exception) { }
                        }));

                    }
                }
                else if (elementType.Contains("bonus"))
                {
                    lock (canvas)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                Rectangle bonus = new Rectangle();
                                bonus.Name = elementType;
                                bonus.Width = 15;
                                bonus.Height = 15;
                                bonus.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/bonus.png")));
                                Canvas.SetTop(bonus, positionFromTop);
                                Canvas.SetLeft(bonus, positionFromLeft);
                                canvas.Children.Add(bonus);
                            }
                            catch (Exception) { }
                        }));
                    }
                }
                else if (elementType == "score")
                {
                    lock (Score1)
                    {
                        lock (Score2)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                Score1.Text = Convert.ToString(positionFromTop);
                                Score2.Text = Convert.ToString(positionFromLeft);
                            }));
                        }
                    }
                }
                else if (elementType.Contains("dir"))
                {
                   lock (canvas)
                   {
                        lock (player1)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                if (elementType.Contains("W"))
                                {
                                    player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_up.png")));
                                }
                                else if (elementType.Contains("S"))
                                {
                                    player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_down.png")));
                                }
                                else if (elementType.Contains("A"))
                                {
                                    player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_left.png")));
                                }
                                else if (elementType.Contains("D"))
                                {
                                    player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_right.png")));
                                }
                            }));
                        }
                   }
                }
            }
            catch (Exception) { }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    keyA = true;
                    if (!e.IsRepeat)
                    { 
                        player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_left.png")));
                        Send("dir" + e.Key.ToString() + ",0,0");
                    }
                    break;
                case Key.W:
                    keyW = true;
                    if (!e.IsRepeat)
                    { 
                        player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_up.png")));
                        Send("dir" + e.Key.ToString() + ",0,0");
                    }
                    break;
                case Key.S:
                    keyS = true;
                    if (!e.IsRepeat)
                    {
                        player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_down.png")));
                        Send("dir" + e.Key.ToString() + ",0,0");
                    }
                    break;
                case Key.D:
                    keyD = true;
                    if (!e.IsRepeat)
                    { 
                        player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_right.png")));
                        Send("dir" + e.Key.ToString() + ",0,0");
                    }
                    break;
                case Key.Up:
                    stopwatch.Stop();
                    if (!e.IsRepeat && stopwatch.ElapsedMilliseconds >= shotDelay)
                    {
                        Send("shotUp," + Convert.ToInt32(Canvas.GetTop(player2)).ToString() + "," + Convert.ToInt32(Canvas.GetLeft(player2) + player2.Width / 2).ToString());
                        stopwatch.Restart();
                    }
                    else
                    {
                        stopwatch.Start();
                    }
                    break;
                case Key.Down:
                    stopwatch.Stop();
                    if (!e.IsRepeat && stopwatch.ElapsedMilliseconds >= shotDelay)
                    {
                        Send("shotDown," + Convert.ToInt32(Canvas.GetTop(player2) + player2.Height).ToString() + "," + Convert.ToInt32(Canvas.GetLeft(player2) + player2.Width / 2).ToString());
                        stopwatch.Restart();
                    }
                    else
                    {
                        stopwatch.Start();
                    }
                    break;
                case Key.Right:
                    stopwatch.Stop();
                    if (!e.IsRepeat && stopwatch.ElapsedMilliseconds >= shotDelay)
                    {
                        Send("shotRight," + Convert.ToInt32(Canvas.GetTop(player2) + player2.Height / 2).ToString() + "," + Convert.ToInt32(Canvas.GetLeft(player2) + player2.Width).ToString());
                        stopwatch.Restart();
                    }
                    else
                    {
                        stopwatch.Start();
                    }
                    break;
                case Key.Left:
                    stopwatch.Stop();
                    if (!e.IsRepeat && stopwatch.ElapsedMilliseconds >= shotDelay)
                    {
                        Send("shotLeft," + Convert.ToInt32(Canvas.GetTop(player2) + player2.Height / 2).ToString() + "," + Convert.ToInt32(Canvas.GetLeft(player2)).ToString());
                        stopwatch.Restart();
                    }
                    else
                    {
                        stopwatch.Start();
                    }
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
                    break;
                case Key.W:
                    keyW = false;
                    break;
                case Key.S:
                    keyS = false;
                    break;
                case Key.D:
                    keyD = false;
                    break;
                default:

                    break;
            }
        }
    }
}
