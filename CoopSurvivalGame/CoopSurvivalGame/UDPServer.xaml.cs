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
        private Socket _socketForReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket _socketForSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;


        DispatcherTimer gameTimer = new DispatcherTimer();
        List<Rectangle> shotsActive = new List<Rectangle>();
        List<Rectangle> shotsToRemove = new List<Rectangle>();
        public List<Player> players = new List<Player>();

        public UDPServer()
        {
            InitializeComponent();

            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            canvas.Focus();
        }

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
                string actionType = rec.Split(',')[(int)RECEIVED.ACTION_TYPE];

                Dispatcher.Invoke( new Action( () => {

                    dispatchAction(actionType, rec);
                    
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
        //TODO change method name
        public void addPlayer(string playerName)
        {
            Coordinate startCoordinates = new Coordinate(150, 175);
            Player newPlayer = new Player(playerName, "red", this);
            this.players.Add(newPlayer);
            startCoordinates.setPlayerAtCoordinates(newPlayer.Figure);
            canvas.Children.Add(newPlayer.Figure);
        }

        private void dispatchAction(string action, string receivedMessage)
        {
            switch (action)
            {
                case "addPlayer":
                    addNewPlayer(receivedMessage);
                    sendCurrentPlayersToNewClient();
                    break;

                case "movePlayer":
                    ChangePosition(receivedMessage);
                    break;

                default:
                    break;

            }
                
        }

        private void addNewPlayer(string playerData)
        {
            string playerName = playerData.Split(',')[(int)RECEIVED.PLAYER_NAME];
            int startPositionFromTop = Convert.ToInt32(playerData.Split(',')[(int)RECEIVED.POSITION_FROM_LEFT]);
            int startPositionFromLeft = Convert.ToInt32(playerData.Split(',')[(int)RECEIVED.POSITION_FROM_LEFT]);
            string figureColor = playerData.Split(',')[(int)RECEIVED.FIGURE_COLOR];

            //if(this.getPlayer(playerName) == null)
            //{ 
            Player newPlayer = new Player(playerName, figureColor, this);
            Coordinate startCoordinate = new Coordinate(startPositionFromTop, startPositionFromLeft);
            startCoordinate.setPlayerAtCoordinates(newPlayer.Figure);
            this.players.Add(newPlayer);
            canvas.Children.Add(newPlayer.Figure);
            //}
        }

        private void sendCurrentPlayersToNewClient()
        {
            foreach (Player player in this.players)
            {
                Send(String.Format("{0},{1},{2},{3},{4}", "addPlayer", player.Name, Canvas.GetTop(player.Figure), Canvas.GetLeft(player.Figure), player.color));
            }
        }

        public Player getPlayer(string Name)
        {
            return this.players.Find(player => player.Name == Name);
        }

        private void ChangePosition(string position)
        {
           
            string playerName = position.Split(',')[(int)RECEIVED.PLAYER_NAME];
            int positionFromTop = Convert.ToInt32(position.Split(',')[(int)RECEIVED.POSITION_FROM_TOP]);
            int positionFromLeft = Convert.ToInt32(position.Split(',')[(int)RECEIVED.POSITION_FROM_LEFT]);

            Player player = getPlayer(playerName);

            Coordinate newCoordinate = new Coordinate(positionFromTop, positionFromLeft);
            newCoordinate.setPlayerAtCoordinates(player.Figure);
            Send(String.Format("{0},{1},{2},{3}", "movePlayer", player.Name, newCoordinate.positionFromTop, newCoordinate.positionFromLeft));
        }

        private void CreateShot(Key key, int positionTop, int positionLeft)
        {
            Rectangle shot = new Rectangle();
            shot.Width = 5;
            shot.Height = 5;
            shot.Fill = System.Windows.Media.Brushes.Cyan;
            //shot.Name = "s"+DateTime.Now.ToString("hh.mm.ss.ffffff").Replace('.','_');
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

            Send("player1," + Canvas.GetTop(this.player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            foreach (var item in shotsActive)
            {
                //Send("shot," + Canvas.GetTop(item).ToString() + "," + Canvas.GetLeft(item).ToString() + "," + item.Name);
                Send("shot," + Canvas.GetTop(item).ToString() + "," + Canvas.GetLeft(item).ToString());

            }

        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            this.players[(int)PLAYER.ONE].Controller.move(e.Key);

            //switch (e.Key)
            //{
            //    case Key.A:
            //        Canvas.SetLeft(this.player1, Canvas.GetLeft(this.player1) - 5);
            //        //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            //        break;
            //    case Key.W:
            //        Canvas.SetTop(this.player1, Canvas.GetTop(this.player1) - 5);
            //        //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            //        break;
            //    case Key.S:
            //        Canvas.SetTop(this.player1, Canvas.GetTop(this.player1) + 5);
            //        //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            //        break;
            //    case Key.D:
            //        Canvas.SetLeft(this.player1, Canvas.GetLeft(this.player1) + 5);
            //       //Send("player1," + Canvas.GetTop(player1).ToString() + "," + Canvas.GetLeft(player1).ToString());
            //        break;
            //    default:

            //        break;
            //}
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    CreateShot(Key.Up, Convert.ToInt32(Canvas.GetTop(this.player1)), Convert.ToInt32(Canvas.GetLeft(this.player1) + this.player1.Width / 2));
                    break;
                case Key.Down:
                    CreateShot(Key.Down, Convert.ToInt32(Canvas.GetTop(this.player1) + this.player1.Height), Convert.ToInt32(Canvas.GetLeft(this.player1) + this.player1.Width / 2));
                    break;
                case Key.Right:
                    CreateShot(Key.Right, Convert.ToInt32(Canvas.GetTop(this.player1) + this.player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(this.player1) + this.player1.Width));
                    break;
                case Key.Left:
                    CreateShot(Key.Left, Convert.ToInt32(Canvas.GetTop(this.player1) + this.player1.Height / 2), Convert.ToInt32(Canvas.GetLeft(this.player1)));
                    break;
                default:

                    break;
            }
        }
    }
}
