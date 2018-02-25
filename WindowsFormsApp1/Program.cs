//#define CONSOLE_OUTPUT
#define WINFORM_OUTPUT
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
			ConsoleGame.AllocConsole();
			ConsoleGame.GameStart();
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
#endif
#if CONSOLE_OUTPUT
			Console.ReadKey();
			ConsoleGame.FreeConsole();
#endif
		}
#if DEBUG
		public static class ConsoleGame
		{
			[DllImport("kernel32.dll")]
			public static extern Boolean AllocConsole();
			[DllImport("kernel32.dll")]
			public static extern Boolean FreeConsole();

			public static void GameStart()
			{
				Console.WindowHeight = 30;
				TetrisGame game = TetrisGame.Initialize(0, ConsolePaintBoard, PrintFailInfo);
				while (game.State == TetrisGame.States.Paused && game.State == TetrisGame.States.Playing)
				{
					InputMonitor(game);
				}
			}

			static void InputMonitor(TetrisGame game)
			{
				ConsoleKeyInfo key = Console.ReadKey();
				switch(key.Key)
				{
					case ConsoleKey.DownArrow:
						{
							game.FallToBottom();
							break;
						}
					case ConsoleKey.LeftArrow:
						{
							game.MoveLeft();
							break;
						}
					case ConsoleKey.RightArrow:
						{
							game.MoveRight();
							break;
						}
					case ConsoleKey.Z:
						{
							game.AntiClockwiseRotate();
							break;
						}
					case ConsoleKey.X:
						{
							game.ClockwiseRotate();
							break;
						}
					case ConsoleKey.UpArrow:
						{
							game.ClockwiseRotate();
							break;
						}
					case ConsoleKey.Spacebar:
						{
							game.Pause();
							break;
						}
					case ConsoleKey.S:
						{
							game.Start();
							break;
						}
					default:
						break;
				}
			}

			static void ConsolePaintBoard(object sender, EventArgs eventArgs)
			{
				TetrisGame game = (TetrisGame)sender; 
				Console.Clear();
				StringBuilder builder = new StringBuilder((game.Row * 2 ) * (game.Column * 2 + 2));
				Color?[,] newBoard = game.GetBoard();
				Color?[,] nextPieceBoard = game.GetNextPieceBoard();

				builder.Append("┏━");
				for (int X = 0; X < game.Column; X++)
					builder.Append("━━");
				builder.Append("┓ ");
				builder.Append("Next Piece: ");
				builder.AppendLine();
				for (int Y = game.Row - 1; Y >= 0; Y--)
				{
					builder.Append("┃ ");
					for (int X = 0; X < game.Column; X++)
					{
						if (newBoard[X, Y] == null)
							builder.Append("  ");
						else
							builder.Append("█");
					}
					builder.Append("┃ ");
					//PaintEvent Next Piece
					{
						if (Y == game.Row - 1)
						{
							builder.Append("┏━");
							for (int X = 0; X < 4; X++)
								builder.Append("━━");
							builder.Append("┓ ");
						}
						else if (game.Row - 1 > Y && Y > game.Row - 6)
						{
							builder.Append("┃ ");
							for (int X = 0; X < 4; X++)
							{
								if (nextPieceBoard[X, 5 - game.Row + Y] == null)
									builder.Append("  ");
								else
									builder.Append("█");
							}
							builder.Append("┃ ");
						}
						else if (Y == game.Row - 6)
						{
							builder.Append("┗━");
							for (int X = 0; X < 4; X++)
								builder.Append("━━");
							builder.Append("┛ ");
						}
						else if (Y == game.Row - 7)
						{
							builder.Append($"Scores: {game.Score}");
						}
						else if (Y == game.Row - 8)
						{
							builder.Append($"Line: {game.EliminatedLine}");
						}
						else if (Y == game.Row - 9)
						{
							builder.Append($"Level: {game.Level}");
						}
						else if (Y == game.Row - 10)
						{
							builder.Append($"Time: {game.PlayingTime}");
						}
					}
					builder.AppendLine();
				}
				builder.Append("┗━");
				for (int X = 0; X < game.Column; X++)
					builder.Append("━━");
				builder.Append("┛");
				builder.AppendLine();
				Console.WriteLine(builder);
			}

			static void PrintFailInfo(object sender, EventArgs eventArgs)
			{
				Console.WriteLine("You Lose!!");
			}
		}
#endif
	}
}
