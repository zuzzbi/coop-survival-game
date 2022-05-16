﻿using System;
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
            Score1.Text = Convert.ToString(playerPoints1);
            Score2.Text = Convert.ToString(playerPoints2);
            stopwatchShot.Start();
            stopwatchEnemy.Start();
            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            //enemyTimer.Interval = TimeSpan.FromMilliseconds(10000);
            //enemyTimer.Tick += EnemyLoop;
            //enemyTimer.Start();
            canvas.Focus();
        }

        private Socket _socketForReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket _socketForSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        DispatcherTimer gameTimer = new DispatcherTimer();
        //DispatcherTimer enemyTimer = new DispatcherTimer();
        List<Rectangle> shotsActive = new List<Rectangle>();
        List<Rectangle> itemsToRemove = new List<Rectangle>();
        List<Rectangle> enemyActive = new List<Rectangle>();
        bool keyA = false;
        bool keyW = false;
        bool keyS = false;
        bool keyD = false;
        int shotCounterServer = 0;
        int enemyCounterServer = 0;
        Stopwatch stopwatchShot = new Stopwatch();
        Stopwatch stopwatchEnemy = new Stopwatch();
        int shotDelay = 500;
        int playerPoints1 = 0;
        int playerPoints2 = 0;

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
            string elemntType = position.Split(',')[0];
            int positionFromTop = Convert.ToInt32(position.Split(',')[1]);
            int positionFromLeft = Convert.ToInt32(position.Split(',')[2]);

            if (elemntType == "player2")
            {
                if (positionFromTop > 0 && positionFromTop < canvas.ActualHeight - player1.Height && positionFromLeft > 0 && positionFromLeft < canvas.ActualWidth - player1.Width)
                {
                    Canvas.SetLeft(player2, positionFromLeft);
                    Canvas.SetTop(player2, positionFromTop);

                    Send("player2," + Canvas.GetTop(player2).ToString() + "," + Canvas.GetLeft(player2).ToString());
                }
            }
            else if (elemntType == "shotUp")
            {
                CreateShot(Key.Up, positionFromTop, positionFromLeft, 'c');
            }
            else if (elemntType == "shotDown")
            {
                CreateShot(Key.Down, positionFromTop, positionFromLeft, 'c');
            }
            else if (elemntType == "shotLeft")
            {
                CreateShot(Key.Left, positionFromTop, positionFromLeft, 'c');
            }
            else if (elemntType == "shotRight")
            {
                CreateShot(Key.Right, positionFromTop, positionFromLeft, 'c');
            }
        }

        private void CreateShot(Key key, int positionTop, int positionLeft, char player)
        {
            Rectangle shot = new Rectangle();
            shot.Width = 5;
            shot.Height = 5;
            shot.Fill = System.Windows.Media.Brushes.Cyan;
            shot.Name = "shot"+ player + shotCounterServer.ToString();
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
            Send(shot.Name + "," + Canvas.GetTop(shot).ToString() + "," + Canvas.GetLeft(shot).ToString());
        }

        private void CreateEnemy()
        {
            Rectangle enemy = new Rectangle();
            enemy.Width = 40;
            enemy.Height = 40;
            enemy.Fill = System.Windows.Media.Brushes.Red;
            enemy.Name = "enemy" + enemyCounterServer.ToString();
            enemy.Tag = "2";
            enemyCounterServer++;
            Random random = new Random();
            Canvas.SetTop(enemy, random.Next(0, Convert.ToInt32(canvas.ActualHeight) - Convert.ToInt32(enemy.Height)));
            Canvas.SetLeft(enemy, random.Next(0, Convert.ToInt32(canvas.ActualWidth) - Convert.ToInt32(enemy.Width)));
            canvas.Children.Add(enemy);
            enemyActive.Add(enemy);
            Send(enemy.Name + "," + Canvas.GetTop(enemy).ToString() + "," + Canvas.GetLeft(enemy).ToString());
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
                        itemsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotDown")
                {
                    Canvas.SetTop(item, Canvas.GetTop(item) + 7);
                    if (Canvas.GetTop(item) > canvas.ActualHeight + 5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotRight")
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) + 7);
                    if (Canvas.GetLeft(item) > canvas.ActualWidth + 5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
                if (item is Rectangle && (string)item.Tag == "shotLeft")
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) - 7);
                    if (Canvas.GetLeft(item) < -5)
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }
            foreach (Rectangle shot in shotsActive)
            {
                foreach (var enemy in enemyActive)
                {
                    if (Overlap(enemy, shot))
                    {
                        itemsToRemove.Add(shot);
                        int lifeLeft = Convert.ToInt32(enemy.Tag) - 1;
                        if (lifeLeft <= 0)
                        {
                            itemsToRemove.Add(enemy);
                            if (shot.Name.Contains("shotc"))
                            {
                                playerPoints2++;
                                Score2.Text = Convert.ToString(playerPoints2);
                            }
                            else
                            {
                                playerPoints1++;
                                Score1.Text = Convert.ToString(playerPoints1);
                            }
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
                Send(item.Name + ",-1,-1");
                shotsActive.Remove(item);
                enemyActive.Remove(item);
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
            Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            foreach (var shot in shotsActive)
            {
                Send(shot.Name + "," + Canvas.GetTop(shot).ToString() + "," + Canvas.GetLeft(shot).ToString());
            }
            stopwatchEnemy.Stop();
            if (stopwatchEnemy.ElapsedMilliseconds > 3000 && enemyActive.Count <= 4)
            {
                CreateEnemy();
                stopwatchEnemy.Restart();
            }
            else
            {
                stopwatchEnemy.Start();
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
                    break;
                case Key.W:
                    keyW = true;
                    break;
                case Key.S:
                    keyS = true;
                    break;
                case Key.D:
                    keyD = true;
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
