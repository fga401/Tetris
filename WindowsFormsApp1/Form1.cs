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

namespace WindowsFormsApp1
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			Console.Write("Hi");
			InitializeComponent();
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			//g.DrawString(DateTime.Now.ToString(), new Font("宋体", game.Times), Brushes.Black, new Point(50, 50));
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			Graphics g = CreateGraphics();
			g.Clear(this.BackColor);
			InvokePaint(this, new PaintEventArgs(g, ClientRectangle));
		}
	}
}
