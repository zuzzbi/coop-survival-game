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
    /// Logika interakcji dla klasy UDPClient.xaml
    /// </summary>
    public partial class UDPClient : Window
    {
       
        private Socket _socketForReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private Socket _socketForSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        List<Player> players = new List<Player>();
        List<Rectangle> shotsToRemove = new List<Rectangle>();

        public UDPClient(string playerName)
        {
            InitializeComponent();
            canvas.Focus();
        }

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
                string actionType = rec.Split(',')[(int)RECEIVED.ACTION_TYPE];

                Dispatcher.Invoke(new Action( () => {

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
            Coordinate startCoordinates = new Coordinate(250, 175);
            Player newPlayer = new Player(playerName, "blue", this);
            this.players.Add(newPlayer);
            startCoordinates.setPlayerAtCoordinates(newPlayer.Figure);
            canvas.Children.Add(newPlayer.Figure);

            Send(String.Format("{0},{1},{2},{3},{4}", "addPlayer", newPlayer.Name, startCoordinates.positionFromTop, startCoordinates.positionFromLeft, newPlayer.color));
        }

        private void dispatchAction(string action, string receivedMessage)
        {
            switch (action)
            {
                case "addPlayer":
                    addNewPlayer(receivedMessage);
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

            //if (this.getPlayer(playerName) == null)
            //{
                Player newPlayer = new Player(playerName, figureColor, this);
                Coordinate startCoordinate = new Coordinate(startPositionFromTop, startPositionFromLeft);
                startCoordinate.setPlayerAtCoordinates(newPlayer.Figure);
                this.players.Add(newPlayer);
                canvas.Children.Add(newPlayer.Figure);
            //}

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
            //if (elementType == "shot")
            //{
            //    //string elementName = position.Split(',')[3];
            //    //try
            //    //{
            //    //    var shot = (from Rectangle item in canvas.Children where elementName.Equals(item.Name) select item).First();
            //    //    Canvas.SetTop(shot, positionFromTop);
            //    //    Canvas.SetLeft(shot, positionFromLeft);
            //    //}
            //    //catch (Exception)
            //    //{
            //        Rectangle shot = new Rectangle();
            //        //shot.Name = elementName;
            //        shot.Width = 5;
            //        shot.Height = 5;
            //        shot.Tag = "shot";
            //        shot.Fill = System.Windows.Media.Brushes.Cyan;
            //        Canvas.SetTop(shot, positionFromTop);
            //        Canvas.SetLeft(shot, positionFromLeft);
            //        canvas.Children.Add(shot);
            //    //}
                
            //}
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            this.players[(int)PLAYER.TWO].Controller.move(e.Key);
        }
    }
}
