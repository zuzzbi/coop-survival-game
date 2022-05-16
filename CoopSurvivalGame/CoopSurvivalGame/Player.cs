using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Media;
using System.Windows.Controls;


namespace CoopSurvivalGame
{
    public class Player
    {
        public string Name;
        public Rectangle Figure;
        public  Controller Controller;
        public dynamic Parent;
        public string color;
        
        public Player (string playerName, string figureColor, dynamic parent)
        {
            this.Name = playerName;
            this.createFigure(figureColor);
            this.Parent = parent;
            this.Controller = new Controller(this);
            this.color = figureColor;


        }

        private Brush convertStringToBrush(string color)
        {
            var converter = new System.Windows.Media.BrushConverter();
            var brush = (Brush)converter.ConvertFromString(color);
            return brush;

        }

        private void createFigure(string figureColor)
        {
            Figure = new Rectangle();
            Figure.Width = 10;
            Figure.Height = 10;
            Figure.Fill = convertStringToBrush(figureColor);

        }

        private void setFigurePosition(int startPositionFromTop, int startPositionFromLeft)
        {
            Canvas.SetTop(Figure, startPositionFromTop);
            Canvas.SetLeft(Figure, startPositionFromLeft);
        }
    }

    public class Coordinate
    {
        public int positionFromTop;
        public int positionFromLeft;

        public Coordinate(int positionFromTop, int positionFromLeft)
        {
            this.positionFromTop = positionFromTop;
            this.positionFromLeft = positionFromLeft;
        }

        public void setPlayerAtCoordinates(Rectangle figure)
        {
            Canvas.SetTop(figure, this.positionFromTop);
            Canvas.SetLeft(figure, this.positionFromLeft);
        }
    }
    public enum PLAYER
    {
        ONE = 0,
        TWO = 1
    }

    public enum RECEIVED
    {
        ACTION_TYPE = 0,
        PLAYER_NAME = 1,
        POSITION_FROM_TOP = 2,
        POSITION_FROM_LEFT = 3,
        FIGURE_COLOR = 4,
    }
}
