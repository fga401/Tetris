using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WindowsFormsApp1
{
	public partial class Form1 : Form
	{
		public const int SquarePixelWidth = 24;
		private TetrisGame game;
		private Font defaultFont = new Font("Arial Black", 10);
		public Form1()
		{
			InitializeComponent();
		}

		private void PaintTetrisWithDoubleBuffer(object sender,EventArgs e)
		{
			BufferedGraphicsContext context = BufferedGraphicsManager.Current;
			using (BufferedGraphics bufferedGraphics = context.Allocate(this.CreateGraphics(), this.DisplayRectangle))
			{
				PaintTetris((TetrisGame)sender, bufferedGraphics.Graphics);
				bufferedGraphics.Render();
			}
		}
		private void PaintTetris(TetrisGame game, Graphics graphics)
		{
			//游戏区域
			graphics.FillRectangle(new SolidBrush(Color.BurlyWood), this.DisplayRectangle);
			graphics.FillRectangle(Brushes.DimGray, 20, 20, 250, 610);
			PaintFrame(20, 20, 250, 610);
			PaintStrip(Brushes.PaleGoldenrod, Brushes.SeaShell, 25, 25, 600, 10);
			//画方块
			Color?[,] board = game.GetBoard();
			for (int X = 0; X < game.Column; X++)
			{
				for (int Y = 0; Y < game.Row; Y++)
				{
					if (board[X, Y] != null)
					{
						PaintPieceSquare(board[X, Y].Value, 25 + X * SquarePixelWidth, 25 + (game.Row - Y - 1) * SquarePixelWidth);
					}
				}
			}
			//下一块
			graphics.DrawString("Next Piece:", defaultFont, Brushes.Black, 290, 20);
			graphics.FillRectangle(Brushes.DimGray, 290, 40, 106, 106);
			PaintFrame(290, 40, 106, 106);
			PaintStrip(Brushes.LightGray, Brushes.WhiteSmoke, 295, 45, 96, 4);
			//下一块方块
			Color?[,] nextPieceBoard = game.GetNextPieceBoard();
			for (int X = 0; X < 4; X++)
			{
				for (int Y = 0; Y < 4; Y++)
				{
					if (nextPieceBoard[X, Y] != null)
					{
						PaintPieceSquare(nextPieceBoard[X, Y].Value, 295 + X * SquarePixelWidth, 45 + (4 - Y - 1) * SquarePixelWidth);
					}
				}
			}
			//其他文字信息
			graphics.DrawString("Score:", defaultFont, Brushes.Black, 290, 170);
			graphics.DrawString($"{game.Score,8}", defaultFont, Brushes.Black, 290, 190);
			graphics.DrawString("Line:", defaultFont, Brushes.Black, 290, 230);
			graphics.DrawString($"{game.EliminatedLine,8}", defaultFont, Brushes.Black, 290, 250);
			graphics.DrawString("Level:", defaultFont, Brushes.Black, 290, 290);
			graphics.DrawString($"{game.Level,8}", defaultFont, Brushes.Black, 290, 310);
			graphics.DrawString("Time:", defaultFont, Brushes.Black, 290, 350);
			graphics.DrawString($"{game.PlayingTime,12}", defaultFont, Brushes.Black, 290, 370);
			graphics.DrawString("Time:", defaultFont, Brushes.Black, 290, 350);
			graphics.DrawString($"{game.PlayingTime,12}", defaultFont, Brushes.Black, 290, 370);
			graphics.DrawString("State:", defaultFont, Brushes.Black, 290, 410);
			graphics.DrawString($"{game.State,12}", defaultFont, Brushes.Black, 290, 430);

			void PaintPieceSquare(Color color, int PixelX, int PixelY, int width = SquarePixelWidth, int height = SquarePixelWidth)
			{
				graphics.FillRectangle(new SolidBrush(color), PixelX, PixelY, width, height);
				graphics.DrawLine(new Pen(Color.FromArgb(63, 255, 255, 255), 1), PixelX + width, PixelY - 1, PixelX + width - 4, PixelY + 3);
				graphics.DrawLine(new Pen(Color.FromArgb(95, 127, 127, 127), 1), PixelX, PixelY, PixelX + 4, PixelY + 4);
				graphics.DrawLine(new Pen(Color.FromArgb(95, 127, 127, 127), 1), PixelX + width - 1, PixelY + height - 1, PixelX + width - 4, PixelY + height - 4);
				graphics.DrawLine(new Pen(Color.FromArgb(63, 15, 15, 15), 1), PixelX, PixelY + height - 1, PixelX + 4, PixelY + height - 5);
				graphics.FillRectangle(
					new LinearGradientBrush(
						new Point(PixelX + SquarePixelWidth, PixelY),
						new Point(PixelX, PixelY + SquarePixelWidth),
						Color.FromArgb(0x7F, 0xFF, 0xFF, 0xFF),
						Color.FromArgb(0x7F, 0x00, 0x00, 0x00)
						),
					PixelX, PixelY, width, height);
				graphics.FillRectangle(new SolidBrush(Color.FromArgb(63, color)), PixelX + 4, PixelY + 4, width - 8, height - 8);
				//graphics.DrawLine(new Pen(Color.FromArgb(63, 255, 255, 255), 1), PixelX + 4, PixelY + 3, PixelX + width - 4, PixelY + 3);
				//graphics.DrawLine(new Pen(Color.FromArgb(63, 255, 255, 255), 1), PixelX + width - 4, PixelY + height - 5, PixelX + width - 4, PixelY + 3);
				//graphics.DrawLine(new Pen(Color.FromArgb(63, 15, 15, 15), 1), PixelX + 4, PixelY + 4, PixelX + 4, PixelY + height - 5);
				//graphics.DrawLine(new Pen(Color.FromArgb(63, 15, 15, 15), 1), PixelX + width - 4, PixelY + height - 5, PixelX + 4, PixelY + height - 5);
			}
			void PaintFrame(int x, int y, int width, int height)
			{
				graphics.DrawLines(Pens.White, new Point[] { new Point { X = x + 2, Y = y + 1 }, new Point { X = x + width - 2, Y = y + 1 }, new Point { X = x + width - 2, Y = y + height - 3 } });
				graphics.DrawLines(Pens.Black, new Point[] { new Point { X = x + 4, Y = y + 4 }, new Point { X = x + width - 5, Y = y + 4 }, new Point { X = x + width - 5, Y = y + height - 5 } });
				graphics.DrawLines(Pens.Black, new Point[] { new Point { X = x, Y = y + 1 }, new Point { X = x, Y = y + height - 1 }, new Point { X = x + width - 2, Y = y + height - 1 } });
				graphics.DrawLines(Pens.White, new Point[] { new Point { X = x + 3, Y = y + 5 }, new Point { X = x + 3, Y = y + height - 4 }, new Point { X = x + width - 6, Y = y + height - 4 } });

			}
			void PaintStrip(Brush firstBrush, Brush secondBrush, int x, int y, int height, int stripNumber, int width = SquarePixelWidth)
			{
				for (int i = 0; i < stripNumber; i++)
				{
					if (i % 2 == 0)
						graphics.FillRectangle(firstBrush, x + i * width, y, width, height);
					else
						graphics.FillRectangle(secondBrush, x + i * width, y, width, height);
				}
			}

		}


		private void LoseGame(object sender,EventArgs e)
		{
			
		}

		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Space:
				case Keys.P:
					switch(game?.State)
					{
						case TetrisGame.States.Ready:
							game?.Start();
							break;
						case TetrisGame.States.Playing:
							game?.Pause();
							break;
						case TetrisGame.States.Paused:
							game?.Continue();
							break;
						default:
							break;
					}
					break;
				case Keys.Left:
					game?.MoveLeft();
					break;
				case Keys.Right:
					game?.MoveRight();
					break;
				case Keys.Down:
					game?.FallToBottom();
					break;
				case Keys.Up:
					game?.ClockwiseRotate();
					break;
				case Keys.Z:
					game?.AntiClockwiseRotate();
					break;
				case Keys.X:
					game?.ClockwiseRotate();
					break;
				case Keys.S:
					game?.Save("data.bin");
					break;
				case Keys.R:
					game?.Restart();
					break;
				case Keys.I:
					game = TetrisGame.Initialize(0, PaintTetrisWithDoubleBuffer, LoseGame);
					break;
				case Keys.L:
					game = TetrisGame.Load("data.bin", PaintTetrisWithDoubleBuffer, LoseGame);
					break;
				case Keys.Q:
					PaintTetrisWithDoubleBuffer(game, null);
					break;
				default:
					break;
			}
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			Program.ConsoleGame.FreeConsole();
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			//PaintTetris(CreateGraphics());
		}
	}
}
