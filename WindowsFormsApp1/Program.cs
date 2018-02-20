#define CONSOLE_OUTPUT

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
	static class Program
	{
		/// <summary>
		/// 应用程序的主入口点。
		/// </summary>
		[STAThread]
		static void Main()
		{
#if CONSOLE_OUTPUT
			ConsoleGame.AllocConsole();
			ConsoleGame.GameStart();
#elif WINFORM_OUTPUT
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
#endif 
			Console.ReadKey();
			ConsoleGame.FreeConsole();
		}

		static class ConsoleGame
		{
			[DllImport("kernel32.dll")]
			public static extern Boolean AllocConsole();
			[DllImport("kernel32.dll")]
			public static extern Boolean FreeConsole();

			public static void GameStart()
			{
				Console.WindowHeight = 30;
				TetrisGame.Board board = new TetrisGame.Board();
				board.Start();
				Thread fallingThread = new Thread(new ParameterizedThreadStart(FallingThread));
				fallingThread.Start(board);
				while (!board.isFailing)
				{
					InputMonitor(board);
				}
			}

			static void FallingThread(object B)
			{
				TetrisGame.Board board = (TetrisGame.Board)B;
				while (!board.isFailing)
				{
					lock (board)
					{
						if (board.Falling())
							ConsolePaintBoard(board);
					}
					Thread.Sleep(200);
				}
				Console.WriteLine("You Lose!");
			}

			static void InputMonitor(TetrisGame.Board board)
			{
				ConsoleKeyInfo key = Console.ReadKey();
				switch(key.Key)
				{
					case ConsoleKey.DownArrow:
						{
							lock (board)
							{
								if (board.Falling())
									ConsolePaintBoard(board);
							}
							break;
						}
					case ConsoleKey.LeftArrow:
						{
							lock (board)
							{
								if (board.MoveLeft(1))
									ConsolePaintBoard(board);
							}
							break;
						}
					case ConsoleKey.RightArrow:
						{
							lock (board)
							{
								if (board.MoveRight(1))
									ConsolePaintBoard(board);
							}
							break;
						}
					case ConsoleKey.Z:
						{
							lock (board)
							{
								if (board.AntiClockwiseRotate())
									ConsolePaintBoard(board);
							}
							break;
						}
					case ConsoleKey.X:
						{
							lock(board)
							{
								if (board.ClockwiseRotate())
									ConsolePaintBoard(board);
							}
							break;
						}
					default:
						break;
				}
			}

			static void ConsolePaintBoard(TetrisGame.Board board)
			{
				Console.Clear();
				StringBuilder builder = new StringBuilder((TetrisGame.Board.Row ) * (TetrisGame.Board.Column * 2 + 2));
				Color?[,] newBoard = board.GetBoard();
				builder.Append("┏━");
				for (int X = 0; X < TetrisGame.Board.Column; X++)
					builder.Append("━━");
				builder.Append("┓");
				builder.AppendLine();
				for (int Y = TetrisGame.Board.Row - 1; Y >= 0; Y--)
				{
					builder.Append("┃ ");
					for (int X = 0; X < TetrisGame.Board.Column; X++)
					{
						if (newBoard[X, Y] == null)
							builder.Append("  ");
						else
							builder.Append("█");
					}
					builder.Append("┃");
					builder.AppendLine();
				}
				builder.Append("┗━");
				for (int X = 0; X < TetrisGame.Board.Column; X++)
					builder.Append("━━");
				builder.Append("┛");
				builder.AppendLine();
				Console.WriteLine(builder);
			}

		}
	}
}
