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
    /// Logika interakcji dla klasy UDPServer.xaml
    /// </summary>
    public partial class UDPServer : Window
    {
        public UDPServer()
        {
            InitializeComponent();
            Score1.Text = Convert.ToString(playerScore1);
            Score2.Text = Convert.ToString(playerScore2);
            //stopwatchShot.Start();
            //stopwatchEnemy.Start();
            //stopwatchBonus.Start();
            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            gameTimer.Tick += GameLoop;
            //gameTimer.Start();
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
        List<Rectangle> itemsToRemove = new List<Rectangle>();
        List<Rectangle> enemyActive = new List<Rectangle>();
        List<Rectangle> bonusActive = new List<Rectangle>();
        bool keyA = false;
        bool keyW = false;
        bool keyS = false;
        bool keyD = false;
        int shotCounter = 0;
        int enemyCounter = 0;
        int bonusCounter = 0;
        Stopwatch stopwatchShot = new Stopwatch();
        Stopwatch stopwatchEnemy = new Stopwatch();
        Stopwatch stopwatchBonus = new Stopwatch();
        int bonusShots1 = 0;
        int bonusShots2 = 0;
        int shotDelay = 500;
        int playerScore1 = 0;
        int playerScore2 = 0;

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

        private void ChangePosition(string position)
        {
            string elementType = position.Split(',')[0];
            int positionFromTop = Convert.ToInt32(position.Split(',')[1]);
            int positionFromLeft = Convert.ToInt32(position.Split(',')[2]);

            if (elementType == "Hej")
            {
                lock (canvas)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        gameTimer.Start();
                        stopwatchShot.Start();
                        stopwatchEnemy.Start();
                        stopwatchBonus.Start();
                        //foreach (Rectangle item in enemyActive)
                        //{
                        //    Send(item.Name + "," + Canvas.GetTop(item).ToString() + "," + Canvas.GetLeft(item).ToString());
                        //    Send("score," + playerScore1 + ',' + playerScore2);
                        //}
                    }));

                }
            }
            else if (elementType == "player2")
            {
                //lock (canvas)
                //{
                    lock (player2)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {

                            if (positionFromTop > 0 && positionFromTop < canvas.ActualHeight - player1.Height && positionFromLeft > 0 && positionFromLeft < canvas.ActualWidth - player1.Width)
                            {
                                Canvas.SetLeft(player2, positionFromLeft);
                                Canvas.SetTop(player2, positionFromTop);
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    Send("player2," + Canvas.GetTop(player2).ToString() + "," + Canvas.GetLeft(player2).ToString());
                                }));
                            }
                        }));
                    }
                //}
            }
            else if (elementType == "shotUp")
            {
                CreateShot(Key.Up, positionFromTop, positionFromLeft, 'c');
            }
            else if (elementType == "shotDown")
            {
                CreateShot(Key.Down, positionFromTop, positionFromLeft, 'c');
            }
            else if (elementType == "shotLeft")
            {
                CreateShot(Key.Left, positionFromTop, positionFromLeft, 'c');
            }
            else if (elementType == "shotRight")
            {
                CreateShot(Key.Right, positionFromTop, positionFromLeft, 'c');
            }
            else if (elementType.Contains("dir"))
            {
                //lock (canvas)
                //{
                    lock (player2)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            if (elementType.Contains("W"))
                            {
                                player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_up.png")));
                            }
                            else if (elementType.Contains("S"))
                            {
                                player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_down.png")));
                            }
                            else if (elementType.Contains("A"))
                            {
                                player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_left.png")));
                            }
                            else if (elementType.Contains("D"))
                            {
                                player2.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p2_right.png")));
                            }
                        }));
                    }
                //}
            }
        }

        private void CreateShot(Key key, int positionTop, int positionLeft, char player)
        {
            lock (canvas)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Rectangle shot = new Rectangle();
                    shot.Width = 5;
                    shot.Height = 5;
                    shot.Fill = System.Windows.Media.Brushes.OrangeRed;
                    shot.Name = "shot" + player + key.ToString() + shotCounter.ToString();
                    shotCounter++;
                    switch (key)
                    {
                        case Key.Up:
                            Canvas.SetTop(shot, positionTop);
                            Canvas.SetLeft(shot, positionLeft);
                            break;
                        case Key.Down:
                            Canvas.SetTop(shot, positionTop - shot.Height);
                            Canvas.SetLeft(shot, positionLeft);
                            break;
                        case Key.Right:
                            Canvas.SetTop(shot, positionTop);
                            Canvas.SetLeft(shot, positionLeft);
                            break;
                        case Key.Left:
                            Canvas.SetTop(shot, positionTop);
                            Canvas.SetLeft(shot, positionLeft - shot.Width);
                            break;
                        default:
                            break;
                    }
                    canvas.Children.Add(shot);
                    shotsActive.Add(shot);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Send(shot.Name + "," + Canvas.GetTop(shot).ToString() + "," + Canvas.GetLeft(shot).ToString());
                    }));
                }));
            }
        }

        private void CreateEnemy()
        {
            Rectangle enemy = new Rectangle();
            enemy.Width = 30;
            enemy.Height = 30;
            enemy.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/enemy.png")));
            enemy.Name = "enemy" + enemyCounter.ToString();
            enemy.Tag = "2";
            enemyCounter++;
            Random random = new Random();
            Canvas.SetTop(enemy, random.Next(0, Convert.ToInt32(canvas.ActualHeight) - Convert.ToInt32(enemy.Height)));
            Canvas.SetLeft(enemy, random.Next(0, Convert.ToInt32(canvas.ActualWidth) - Convert.ToInt32(enemy.Width)));
            canvas.Children.Add(enemy);
            enemyActive.Add(enemy);
            Dispatcher.Invoke(new Action(() =>
            {
                Send(enemy.Name + "," + Canvas.GetTop(enemy).ToString() + "," + Canvas.GetLeft(enemy).ToString());
            }));
        }

        private void CreateBonus() 
        {
            Rectangle bonus = new Rectangle();
            Random random = new Random();
            bonus.Width = 15;
            bonus.Height = 15;
            bonus.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/bonus.png")));
            bonus.Name = "bonus" + bonusCounter.ToString();
            bonusCounter++;
            Canvas.SetTop(bonus, random.Next(0, Convert.ToInt32(canvas.ActualHeight) - Convert.ToInt32(bonus.Height)));
            Canvas.SetLeft(bonus, random.Next(0, Convert.ToInt32(canvas.ActualWidth) - Convert.ToInt32(bonus.Width)));
            canvas.Children.Add(bonus);
            bonusActive.Add(bonus);
            Dispatcher.Invoke(new Action(() =>
            {
                Send(bonus.Name + "," + Canvas.GetTop(bonus).ToString() + "," + Canvas.GetLeft(bonus).ToString());
            }));
        }

        private void GameLoop(object sender, EventArgs e)
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

            foreach (Rectangle bonus in bonusActive)
            {
                if (Overlap(player1, bonus))
                {
                    bonusShots1 = 3;
                    itemsToRemove.Add(bonus);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Send(bonus.Name + ",-1,-1");
                    }));
                }
                else if (Overlap(player2, bonus))
                {
                    bonusShots2 = 3;
                    itemsToRemove.Add(bonus);
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Send(bonus.Name + ",-1,-1");
                    }));
                }
            }

            foreach (Rectangle shot in shotsActive)
            {
                foreach (var enemy in enemyActive)
                {
                    if (Overlap(enemy, shot))
                    {
                        itemsToRemove.Add(shot);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Send(shot.Name + ",-1,-1");
                        }));
                        int lifeLeft = Convert.ToInt32(enemy.Tag) - 1;
                        if (shot.Name.Contains("shots") && bonusShots1 > 0)
                        {
                            lifeLeft = Convert.ToInt32(enemy.Tag) - 2;
                            bonusShots1--;
                        }
                        else if (shot.Name.Contains("shotc") && bonusShots2 > 0)
                        {
                            lifeLeft = Convert.ToInt32(enemy.Tag) - 2;
                            bonusShots2--;
                        }

                        if (lifeLeft <= 0)
                        {
                            itemsToRemove.Add(enemy);
                            Dispatcher.Invoke(new Action(() =>
                            {
                                Send(enemy.Name + ",-1,-1");
                            }));
                            if (shot.Name.Contains("shotc"))
                            {
                                playerScore2++;
                                Score2.Text = Convert.ToString(playerScore2);
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    Send("score," + playerScore1 + "," + playerScore2);
                                }));
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    Send(enemy.Name + ",-1,-1");
                                }));
                            }
                            else
                            {
                                playerScore1++;
                                Score1.Text = Convert.ToString(playerScore1);
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    Send("score," + playerScore1 + "," + playerScore2);
                                }));
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    Send(enemy.Name + ",-1,-1");
                                }));
                            }
                            Dispatcher.Invoke(new Action(() =>
                            {
                                Send("score," + playerScore1 + "," + playerScore2);
                            }));
                        }
                        else
                        {
                            enemy.Tag = Convert.ToString(lifeLeft);
                        }
                        break;
                    }
                }
            }          

            foreach (Rectangle item in itemsToRemove)
            {
                if (item.Name.Contains("enemy"))
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Send(item.Name + ",-1,-1");
                    }));
                }
                shotsActive.Remove(item);
                enemyActive.Remove(item);
                bonusActive.Remove(item);
                canvas.Children.Remove(item);
            }
            
            itemsToRemove = new List<Rectangle>();                

            if (keyA)
            {
                if (Canvas.GetLeft(player1) - 5 > 0 && Canvas.GetLeft(player1) - 5 < canvas.ActualWidth - player1.Height)
                {
                    Canvas.SetLeft(player1, Canvas.GetLeft(player1) - 5);
                }
            }
            if (keyW)
            {
                if (Canvas.GetTop(player1) - 5 > 0 && Canvas.GetTop(player1) - 5 < canvas.ActualHeight - player1.Height)
                { 
                    Canvas.SetTop(player1, Canvas.GetTop(player1) - 5); 
                }
            }
            if (keyS)
            {
                if (Canvas.GetTop(player1) + 5 > 0 && Canvas.GetTop(player1) + 5 < canvas.ActualHeight - player1.Height)
                {
                    Canvas.SetTop(player1, Canvas.GetTop(player1) + 5);
                }
            }
            if (keyD)
            {
                if (Canvas.GetLeft(player1) + 5 > 0 && Canvas.GetLeft(player1) + 5 < canvas.ActualWidth - player1.Height)
                {
                    Canvas.SetLeft(player1, Canvas.GetLeft(player1) + 5);
                }
            }

            Dispatcher.Invoke(new Action(() =>
            {
                Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            }));

            stopwatchEnemy.Stop();
            if (stopwatchEnemy.ElapsedMilliseconds > 3000) 
            {
                if (enemyActive.Count <= 2)
                {
                    CreateEnemy();
                }
                stopwatchEnemy.Restart();
            }
            else
            {
                stopwatchEnemy.Start();
            }

            stopwatchBonus.Stop();
            if (stopwatchBonus.ElapsedMilliseconds > 5000)
            {
                if (bonusActive.Count <= 0)
                {
                    CreateBonus();                  
                }
                stopwatchBonus.Restart();
            }
            else
            {
                stopwatchBonus.Start();
            }
        }
    
        private bool Overlap(Rectangle r1, Rectangle r2)
        {
            if (Canvas.GetLeft(r1) < Canvas.GetLeft(r2)+r2.ActualWidth && Canvas.GetLeft(r1)+r1.ActualWidth > Canvas.GetLeft(r2) && Canvas.GetTop(r1) < Canvas.GetTop(r2)+r2.ActualHeight && Canvas.GetTop(r1)+r1.ActualHeight > Canvas.GetTop(r2))
            {
                return true;
            }
            return false;
        }
        private void EnemyLoop(object sender, EventArgs e)
        {
            CreateEnemy();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    keyA = true;
                    if (!e.IsRepeat)
                    {
                        player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_left.png")));
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Send("dir" + e.Key.ToString() + ",0,0");
                        }));
                    }
                    break;
                case Key.W:
                    keyW = true;
                    if (!e.IsRepeat) 
                    { 
                        player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_up.png")));
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Send("dir" + e.Key.ToString() + ",0,0");
                        }));
                    }
                    break;
                case Key.S:
                    keyS = true;
                    if (!e.IsRepeat)
                    { 
                        player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_down.png")));
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Send("dir" + e.Key.ToString() + ",0,0");
                        }));
                    }
                    break;
                case Key.D:
                    keyD = true;
                    if (!e.IsRepeat)
                    {
                        player1.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/sprites/p1_right.png")));
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Send("dir" + e.Key.ToString() + ",0,0");
                        }));
                    }
                    break;
                case Key.Up:
                    stopwatchShot.Stop();
                    if (!e.IsRepeat && stopwatchShot.ElapsedMilliseconds >= shotDelay)
                    {
                        CreateShot(Key.Up, Convert.ToInt32(Canvas.GetTop(player1)), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2), 's');
                        stopwatchShot.Restart();
                    }
                    else
                    {
                        stopwatchShot.Start();
                    }
                    break;
                case Key.Down:
                    stopwatchShot.Stop();
                    if (!e.IsRepeat && stopwatchShot.ElapsedMilliseconds >= shotDelay)
                    {
                        CreateShot(Key.Down, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width / 2), 's');
                        stopwatchShot.Restart();
                    }
                    else
                    {
                        stopwatchShot.Start();
                    }
                    break;
                case Key.Right:
                    stopwatchShot.Stop();
                    if (!e.IsRepeat && stopwatchShot.ElapsedMilliseconds >= shotDelay)
                    {
                        CreateShot(Key.Right, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1) + player1.Width), 's');
                        stopwatchShot.Restart();
                    }
                    else
                    {
                        stopwatchShot.Start();
                    }
                    break;
                case Key.Left:
                    stopwatchShot.Stop();
                    if (!e.IsRepeat && stopwatchShot.ElapsedMilliseconds >= shotDelay)
                    {
                        CreateShot(Key.Left, Convert.ToInt32(Canvas.GetTop(player1) + player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(player1)), 's');
                        stopwatchShot.Restart();
                    }
                    else
                    {
                        stopwatchShot.Start();
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
