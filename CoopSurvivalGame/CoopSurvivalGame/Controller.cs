using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Input;
using System.Windows.Controls;

namespace CoopSurvivalGame
{
    public class Controller
    {

        Player player;

        public Controller(Player player)
        {
            this.player = player;
        }
        public void move(Key direction)
        {
            string playerParentType = this.player.Parent.GetType().ToString();
            //if (playerParentType.Contains("UDPServer"))
            //{
                switch (direction)  
                {
                    case Key.A:
                        Canvas.SetLeft(this.player.Figure, Canvas.GetLeft(this.player.Figure) - 5);
                        break;
                    case Key.W:
                        Canvas.SetTop(this.player.Figure, Canvas.GetTop(this.player.Figure) - 5);
                        break;
                    case Key.S:
                        Canvas.SetTop(this.player.Figure, Canvas.GetTop(this.player.Figure) + 5);
                        break;
                    case Key.D:
                        Canvas.SetLeft(this.player.Figure, Canvas.GetLeft(this.player.Figure) + 5);
                        break;
                    default:

                        break;
            }
            //}
            this.player.Parent.Send(String.Format("{0},{1},{2},{3}", "movePlayer", this.player.Name, Canvas.GetTop(this.player.Figure).ToString(), Canvas.GetLeft(player.Figure).ToString()));
        }
    }
}
