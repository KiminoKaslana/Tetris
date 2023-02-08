using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        System.Random random = new System.Random();

        const int blockSize = 50;
        bool[,] isOccupied = new bool[8, 15];//第15行为底边
        Rectangle[,] rectangles = new Rectangle[8, 14];

        int score = 0;
        //int next = 0;

        Figure currentFigure, nextFigure;

        public delegate void GameTick();

        public MainWindow()
        {
            InitializeComponent();
            timer.Elapsed += Timer_Elapsed;
        }


        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(new GameTick(Tick));
        }

        public void Tick()
        {
            Trace.WriteLine("Tick");
            currentFigure.Fall();
        }

        public void StartGame()
        {
            timer.Start();
            timer.AutoReset = true;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 14; j++)
                {
                    isOccupied[i, j] = false;
                    rectangles[i, j] = new Rectangle();
                    rectangles[i, j].Tag = new Point();
                }
                isOccupied[i, 14] = true;//底边始终为true
            }

            AddFigure(true);

            scoreBox.Content = score;
        }

        public void GameOver()
        {
            timer.Stop();
            timer.Close();
        }

        public void AddFigure(bool isFirst)
        {
            if (isFirst)
            {
                currentFigure = new Figure(random.Next(0, 7), blockSize, ref mainCanvas, ref isOccupied, ref rectangles);
                currentFigure.FallToBottom += ReplaceFigure;
                currentFigure.Show();
            }
            nextFigure = new Figure(random.Next(0, 7), blockSize, ref mainCanvas, ref isOccupied, ref rectangles);
            nextFigure.FallToBottom += ReplaceFigure;
            nextFigure.Death += GameOver;
            nextFigure.ShowInPreview(ref preview);
        }

        private void RemoveLine(int targetLine)
        {
            Point temp;

            for (int i = 0; i < 8; i++)
            {
                mainCanvas.Children.Remove(rectangles[i, targetLine]);
            }

            for (int j = targetLine; j > 0; j--)
            {
                for (int i = 0; i < 8; i++)
                {
                    isOccupied[i, j] = isOccupied[i, j - 1];

                    rectangles[i, j] = rectangles[i, j - 1];
                    temp = (Point)rectangles[i, j].Tag;
                    Canvas.SetTop(rectangles[i, j], (temp.Y + 1) * blockSize);
                    temp.Y++;
                    rectangles[i, j].Tag = temp;

                }
            }

            for (int i = 0; i < 8; i++)
            {
                isOccupied[i, 0] = false;
            }
        }

        private void CheckLineFull()
        {
            int fullLineCount = 0;
            bool isFull;
            for (int j = 0; j < 14; j++)
            {
                isFull = true;
                for (int i = 0; i < 8; i++)//此处循环的行列表达方式不同
                {
                    if (!isOccupied[i, j])
                    {
                        isFull = false;
                        break;
                    }
                }
                if (isFull)
                {
                    fullLineCount++;
                    RemoveLine(j);
                    Trace.WriteLine(j, "line is full");
                }
            }
            Trace.WriteLine(fullLineCount, "lines are full");
            CalculateScore(fullLineCount);
            scoreBox.Content = score.ToString();
        }

        private void CheckColumnFull()
        {

        }

        private void ReplaceFigure()
        {
            CheckLineFull();
            nextFigure.RemoveInPreview(ref preview);
            currentFigure = nextFigure;
            currentFigure.Show();
            AddFigure(false);
        }

        private void CalculateScore(int lineNum)
        {
            if (lineNum == 1) score += 10;
            else if (lineNum == 2) score += 30;
            else if (lineNum == 3) score += 100;
            else return;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                Trace.WriteLine("left");
                currentFigure.Move(true);
            }
            if (e.Key == Key.D)
            {
                Trace.WriteLine("right");
                currentFigure.Move(false);
            }
            if (e.Key == Key.W)
            {
                Trace.WriteLine("rotate");
                currentFigure.ChangeDirection();
            }
            if (e.Key == Key.S)
            {
                Trace.WriteLine("fall");
                currentFigure.Fall();
            }
        }
    }

    public class Figure
    {
        int blockSize;
        int type;//type用于表示图形的类型
        Point position = new Point(0, 0);//总体定位器
        int direction = 0;
        Rectangle[] rectangles = new Rectangle[4];

        Canvas fatherCanvas;
        Rectangle[,] gameArea;
        bool[,] isOccupied;

        Point temp;//tag中转变量

        public delegate void LifeEnd();
        public event LifeEnd FallToBottom;
        public event LifeEnd Death;

        public Figure(int type, int blockSize, ref Canvas fatherCanvas, ref bool[,] occupiedTable, ref Rectangle[,] gameArea)
        {
            this.type = type;
            this.fatherCanvas = fatherCanvas;
            this.isOccupied = occupiedTable;
            this.gameArea = gameArea;
            this.blockSize = blockSize;

            Draw();
        }

        private void Draw()
        {
            switch (type)
            {
                case 0:
                    DrawI();
                    break;
                case 1:
                    DrawL();
                    break;
                case 2:
                    DrawJ();
                    break;
                case 3:
                    DrawO();
                    break;
                case 4:
                    DrawZ();
                    break;
                case 5:
                    DrawS();
                    break;
                case 6:
                    DrawT();
                    break;
            }
        }

        public void Show()
        {
            for (int i = 0; i < 4; i++)
            {
                fatherCanvas.Children.Add(rectangles[i]);
            }
        }

        public void ShowInPreview(ref Canvas preview)
        {
            for (int i = 0; i < 4; i++)
            {
                Canvas.SetTop(rectangles[i], Canvas.GetTop(rectangles[i]) + 20);
                Canvas.SetLeft(rectangles[i], Canvas.GetLeft(rectangles[i]) - 30);
                preview.Children.Add(rectangles[i]);
            }
        }

        public void RemoveInPreview(ref Canvas preview)
        {
            for (int i = 0; i < 4; i++)
            {
                Canvas.SetTop(rectangles[i], Canvas.GetTop(rectangles[i]) - 20);
                Canvas.SetLeft(rectangles[i], Canvas.GetLeft(rectangles[i]) + 30);
                preview.Children.Remove(rectangles[i]);
            }
        }

        public void ChangeDirection()
        {
            if (direction > 3)
            {
                direction = 0;
            }

            switch (type)
            {
                case 0:
                    //DrawI();
                    ChangeI();
                    break;
                case 1:
                    //DrawL();
                    ChangeL();
                    break;
                case 2:
                    //DrawReverseL();J
                    ChangeJ();
                    break;
                case 3:
                    return;
                //DrawO();
                case 4:
                    //DrawZ();
                    ChangeZ();
                    break;
                case 5:
                    //DrawReverseZ();
                    Trace.WriteLine(direction, "reverseZ: ");
                    ChangeS();
                    break;
                case 6:
                    //DrawT();
                    ChangeT();
                    break;
            }

            direction++;
            Trace.WriteLine(direction);
        }

        private void ChangeI()
        {
            switch (direction)
            {
                case 0:
                    for (int i = 0; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i - 1))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 4; i++) //
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i - 1) * blockSize);
                        Canvas.SetLeft(rectangles[i], position.X * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y + i - 1);
                    }
                    break;
                case 1:
                    for (int i = 0; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 1, (int)position.Y))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X + i - 1) * blockSize);
                        rectangles[i].Tag = new Point(position.X + i - 1, position.Y);
                    }
                    break;
                case 2:
                    for (int i = 0; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i - 1))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i - 1) * blockSize);
                        Canvas.SetLeft(rectangles[i], position.X * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y + i - 1);
                    }
                    break;
                case 3:
                    for (int i = 0; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 1, (int)position.Y))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 4; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X + i - 1) * blockSize);
                        rectangles[i].Tag = new Point(position.X + i - 1, position.Y);
                    }
                    break;
            }
        }

        private void ChangeL()
        {
            switch (direction)
            {
                case 0:
                    //(X,Y) (X + 1,Y) (X + 2,Y)
                    //(X,Y + 1)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y + 1)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X + i) * blockSize);
                        rectangles[i].Tag = new Point(position.X + i, position.Y);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y + 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y + 1);
                    break;
                case 1:
                    //(X,Y - 2)
                    //(X,Y - 1)
                    //(X,Y) (X + 1,Y)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X + 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - 2 + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], position.X * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y - 2 + i); //标记矩形位置
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X + 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X + 1, position.Y);
                    break;
                case 2:
                    //                    (X,Y - 1)
                    //(X - 2,Y) (X - 1,Y) (X,Y)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y - 1)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X - 2 + i) * blockSize);
                        rectangles[i].Tag = new Point(position.X - 2 + i, position.Y);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y - 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y - 1);
                    break;
                case 3:
                    //(X - 1,Y) (X,Y)
                    //          (X,Y + 1)
                    //          (X,Y + 2)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X - 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize);
                        Canvas.SetLeft(rectangles[i], position.X * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y + i);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X - 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X - 1, position.Y);
                    break;

            }
        }

        private void ChangeJ()
        {
            switch (direction)
            {
                case 0:
                    //(X,Y - 1)
                    //(X,Y) (X + 1,Y) (X + 2,Y)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y - 1)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X + i) * blockSize);
                        rectangles[i].Tag = new Point(position.X + i, position.Y);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y - 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y - 1);
                    break;
                case 1:
                    //          (X,Y - 2)
                    //          (X,Y - 1)
                    //(X - 1,Y) (X,Y)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X - 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - 2 + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], position.X * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y - 2 + i); //标记矩形位置
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X - 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X - 1, position.Y);
                    break;

                case 2:
                    //(X - 2,Y) (X - 1,Y) (X,Y)
                    //                    (X,Y + 1)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y + 1)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], position.Y * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X - 2 + i) * blockSize);
                        rectangles[i].Tag = new Point(position.X - 2 + i, position.Y);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y + 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y + 1);
                    break;
                case 3:
                    //(X,Y)     (X + 1,Y)
                    //(X,Y + 1)
                    //(X,Y + 2)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X + 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize);
                        Canvas.SetLeft(rectangles[i], position.X * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y + i);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X + 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X + 1, position.Y);
                    break;

            }
        }

        private void ChangeZ()
        {
            switch (direction)
            {
                case 0:
                    //              (X,Y - 1)
                    //  (X - 1,Y)   (X,Y)
                    //(X - 1,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - i))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1, position.Y + i - 2))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1, position.Y + i); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - i + 2) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y - i + 2); //标记矩形位置
                    }
                    break;
                case 1:
                    //(X - 1,Y) (X,Y)
                    //          (X,Y + 1) (X + 1,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 2, position.Y + 1))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1 + i) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1 + i, position.Y); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + 1) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X + i - 2) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X + i - 2, position.Y + 1); //标记矩形位置
                    }
                    break;
                case 2:
                    //              (X,Y - 1)
                    //  (X - 1,Y)   (X,Y)
                    //(X - 1,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - i))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1, position.Y + i - 2))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1, position.Y + i); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - i + 2) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y - i + 2); //标记矩形位置
                    }
                    break;
                case 3:
                    //(X - 1,Y) (X,Y)
                    //          (X,Y + 1) (X + 1,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 2, position.Y + 1))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1 + i) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1 + i, position.Y); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + 1) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X + i - 2) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X + i - 2, position.Y + 1); //标记矩形位置
                    }
                    break;

            }
        }

        private void ChangeS()
        {
            switch (direction)
            {
                case 0:
                    //(X - 1,Y - 1)            
                    //(X - 1,Y)     (X,Y)
                    //              (X,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1, position.Y - i))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i - 2))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y + i); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - i + 2) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1, position.Y - i + 2); //标记矩形位置
                    }
                    break;
                case 1:
                    //                (X,Y)       (X + 1 , Y)
                    //(X - 1,Y + 1) (X,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y + 1))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 2, position.Y))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X + i) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X + i, position.Y); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + 1) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - i + 2) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - i + 2, position.Y + 1); //标记矩形位置
                    }
                    break;
                case 2:
                    //(X - 1,Y - 1)            
                    //(X - 1,Y)     (X,Y)
                    //              (X,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1, position.Y - i))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y + i - 2))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + i) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X, position.Y + i); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y - i + 2) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1, position.Y - i + 2); //标记矩形位置
                    }
                    break;
                case 3:
                    //                (X,Y)       (X + 1 , Y)
                    //(X - 1,Y + 1) (X,Y + 1)
                    for (int i = 0; i < 2; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - i, position.Y + 1))
                        {
                            return;
                        }
                    }
                    for (int i = 2; i < 4; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X + i - 2, position.Y))
                        {
                            return;
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X + i) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X + i, position.Y); //标记矩形位置
                    }
                    for (int i = 2; i < 4; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y + 1) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - i + 2) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - i + 2, position.Y + 1); //标记矩形位置
                    }
                    break;

            }
        }

        private void ChangeT()
        {
            switch (direction)
            {
                case 0:
                    //          (X,Y - 1)
                    //(X - 1,Y) (X,Y)
                    //          (X,Y + 1)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - 1 + i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X - 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], (position.Y - 1 + i) * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y - 1 + i);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X - 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X - 1, position.Y);
                    break;
                case 1:
                    //(X - 1,Y) (X,Y) (X + 1,Y)
                    //        (X,Y + 1)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1 + i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y + 1)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize); //画出转向后的矩形
                        Canvas.SetLeft(rectangles[i], (position.X - 1 + i) * blockSize); //画出转向后的矩形
                        rectangles[i].Tag = new Point(position.X - 1 + i, position.Y); //标记矩形位置
                    }
                    Canvas.SetTop(rectangles[3], (position.Y + 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y + 1);
                    break;
                case 2:
                    //(X,Y - 1)
                    //(X,Y)     (X + 1,Y)
                    //(X,Y + 1)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X, position.Y - 1 + i))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X + 1, position.Y)) return;

                    for (int i = 0; i < 3; i++)
                    {

                        Canvas.SetTop(rectangles[i], (position.Y - 1 + i) * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X) * blockSize);
                        rectangles[i].Tag = new Point(position.X, position.Y - 1 + i);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X + 1) * blockSize);
                    rectangles[3].Tag = new Point(position.X + 1, position.Y);
                    break;
                case 3:
                    //          (X,Y - 1)
                    //(X - 1,Y) (X,Y)     (X + 1,Y)
                    for (int i = 0; i < 3; i++) //判断是否可以转向
                    {
                        if (IsLegal(position.X - 1 + i, position.Y))
                        {
                            return;
                        }
                    }
                    if (IsLegal(position.X, position.Y - 1)) return;

                    for (int i = 0; i < 3; i++)
                    {
                        Canvas.SetTop(rectangles[i], (position.Y) * blockSize);
                        Canvas.SetLeft(rectangles[i], (position.X - 1 + i) * blockSize);
                        rectangles[i].Tag = new Point(position.X - 1 + i, position.Y);
                    }
                    Canvas.SetTop(rectangles[3], (position.Y - 1) * blockSize);
                    Canvas.SetLeft(rectangles[3], (position.X) * blockSize);
                    rectangles[3].Tag = new Point(position.X, position.Y - 1);
                    break;

            }
        }

        private bool IsLegal(double willCheckX, double willCheckY)
        {
            if (willCheckX < 0 || willCheckY < 0 || willCheckX > 7 || willCheckY > 14)
            {
                return true;
            }
            return isOccupied[(int)willCheckX, (int)willCheckY];
        }

        public void Fall()
        {
            for (int i = 0; i < 4; i++)
            {
                if (isOccupied[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y + 1])
                {
                    AdjustEnd();
                    return;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Canvas.SetTop(rectangles[i], (((Point)rectangles[i].Tag).Y + 1) * blockSize);
                temp = (Point)rectangles[i].Tag;
                temp.Y += 1;
                rectangles[i].Tag = temp;
            }
            position.Y++;
        }

        /// <summary>
        /// 左右移动，左true右false
        /// </summary>
        /// <param name="LeftOrRight">移动的方向：true为左，false为右</param>
        public void Move(bool LeftOrRight)
        {
            Trace.WriteLine("move");
            if (LeftOrRight)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (IsLegal(((Point)rectangles[i].Tag).X - 1, ((Point)rectangles[i].Tag).Y))
                    {
                        return;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Canvas.SetLeft(rectangles[i], (((Point)rectangles[i].Tag).X - 1) * blockSize);
                    temp = (Point)rectangles[i].Tag;
                    temp.X -= 1;
                    rectangles[i].Tag = temp;
                }

                position.X--;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (IsLegal(((Point)rectangles[i].Tag).X + 1, ((Point)rectangles[i].Tag).Y))
                    {
                        return;
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    Canvas.SetLeft(rectangles[i], (((Point)rectangles[i].Tag).X + 1) * blockSize);
                    temp = (Point)rectangles[i].Tag;
                    temp.X += 1;
                    rectangles[i].Tag = temp;
                }

                position.X++;
            }

        }

        void AdjustEnd()
        {
            Trace.WriteLine("figure death");
            for (int i = 0; i < 4; i++)
            {
                if ((int)((Point)rectangles[i].Tag).Y < 0)
                {
                    if (Death != null)
                    {
                        Death();
                    }
                    return;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                isOccupied[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y] = true;
                gameArea[(int)((Point)rectangles[i].Tag).X, (int)((Point)rectangles[i].Tag).Y] = rectangles[i];
            }
            if (FallToBottom != null)
            {
                FallToBottom();
            }
        }

        #region
        void DrawI()
        {
            Color color = Color.FromRgb(82, 255, 160);

            for (int i = 0; i < 4; i++)
            {
                rectangles[i] = new Rectangle();
                rectangles[i].Width = blockSize;
                rectangles[i].Height = blockSize;
                rectangles[i].Stroke = new SolidColorBrush(Colors.White);
                rectangles[i].Fill = new SolidColorBrush(color);
                rectangles[i].Tag = new Point(1 + i, -1);
                Canvas.SetTop(rectangles[i], (-1) * blockSize);
                Canvas.SetLeft(rectangles[i], (1 + i) * blockSize);
            }

            position.X = 2;
        }

        void DrawL()
        {
            Color color = Color.FromRgb(255, 100, 100);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (2) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(2, 1);
            Canvas.SetTop(rectangles[1], 1 * blockSize);
            Canvas.SetLeft(rectangles[1], (2) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(2, 2);
            Canvas.SetTop(rectangles[2], 2 * blockSize);
            Canvas.SetLeft(rectangles[2], (2) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(3, 2);
            Canvas.SetTop(rectangles[3], 2 * blockSize);
            Canvas.SetLeft(rectangles[3], (3) * blockSize);

            position.X = 2;
            position.Y = 2;
        }

        void DrawJ() //J
        {
            Color color = Color.FromRgb(102, 204, 255);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (2) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(2, 1);
            Canvas.SetTop(rectangles[1], 1 * blockSize);
            Canvas.SetLeft(rectangles[1], (2) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(2, 2);
            Canvas.SetTop(rectangles[2], 2 * blockSize);
            Canvas.SetLeft(rectangles[2], (2) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(1, 2);
            Canvas.SetTop(rectangles[3], 2 * blockSize);
            Canvas.SetLeft(rectangles[3], (1) * blockSize);

            position.X = 2;
            position.Y = 2;
        }

        void DrawO()
        {
            Color color = Color.FromRgb(255, 204, 60);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (2) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(2, 1);
            Canvas.SetTop(rectangles[1], 1 * blockSize);
            Canvas.SetLeft(rectangles[1], (2) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(3, 0);
            Canvas.SetTop(rectangles[2], 0);
            Canvas.SetLeft(rectangles[2], (3) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(3, 1);
            Canvas.SetTop(rectangles[3], 1 * blockSize);
            Canvas.SetLeft(rectangles[3], (3) * blockSize);

            position.X = 2;
        }

        void DrawZ()
        {
            Color color = Color.FromRgb(255, 104, 255);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (2) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(3, 0);
            Canvas.SetTop(rectangles[1], 0);
            Canvas.SetLeft(rectangles[1], (3) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(3, 1);
            Canvas.SetTop(rectangles[2], 1 * blockSize);
            Canvas.SetLeft(rectangles[2], (3) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(4, 1);
            Canvas.SetTop(rectangles[3], 1 * blockSize);
            Canvas.SetLeft(rectangles[3], (4) * blockSize);

            position.X = 3;
        }

        void DrawS() //S
        {
            Color color = Color.FromRgb(152, 54, 255);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(3, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (3) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[1], 0);
            Canvas.SetLeft(rectangles[1], (2) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(2, 1);
            Canvas.SetTop(rectangles[2], 1 * blockSize);
            Canvas.SetLeft(rectangles[2], (2) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(1, 1);
            Canvas.SetTop(rectangles[3], 1 * blockSize);
            Canvas.SetLeft(rectangles[3], (1) * blockSize);

            position.X = 2;
        }

        void DrawT()
        {
            Color color = Color.FromRgb(102, 204, 25);

            rectangles[0] = new Rectangle();
            rectangles[0].Width = blockSize;
            rectangles[0].Height = blockSize;
            rectangles[0].Stroke = new SolidColorBrush(Colors.White);
            rectangles[0].Fill = new SolidColorBrush(color);
            rectangles[0].Tag = new Point(2, 0);
            Canvas.SetTop(rectangles[0], 0);
            Canvas.SetLeft(rectangles[0], (2) * blockSize);

            rectangles[1] = new Rectangle();
            rectangles[1].Width = blockSize;
            rectangles[1].Height = blockSize;
            rectangles[1].Stroke = new SolidColorBrush(Colors.White);
            rectangles[1].Fill = new SolidColorBrush(color);
            rectangles[1].Tag = new Point(3, 0);
            Canvas.SetTop(rectangles[1], 0);
            Canvas.SetLeft(rectangles[1], (3) * blockSize);

            rectangles[2] = new Rectangle();
            rectangles[2].Width = blockSize;
            rectangles[2].Height = blockSize;
            rectangles[2].Stroke = new SolidColorBrush(Colors.White);
            rectangles[2].Fill = new SolidColorBrush(color);
            rectangles[2].Tag = new Point(4, 0);
            Canvas.SetTop(rectangles[2], 0);
            Canvas.SetLeft(rectangles[2], (4) * blockSize);

            rectangles[3] = new Rectangle();
            rectangles[3].Width = blockSize;
            rectangles[3].Height = blockSize;
            rectangles[3].Stroke = new SolidColorBrush(Colors.White);
            rectangles[3].Fill = new SolidColorBrush(color);
            rectangles[3].Tag = new Point(3, 1);
            Canvas.SetTop(rectangles[3], 1 * blockSize);
            Canvas.SetLeft(rectangles[3], (3) * blockSize);

            position.X = 3;
        }
        #endregion
    }
}
