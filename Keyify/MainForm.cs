using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;

namespace Keyify
{
    public partial class MainForm : Form
    {
        private Keyify _model;

        private Image<Bgr, Byte> img;
        LineSegment2D baseLine = new LineSegment2D(new Point(0, 800), new Point(2000, 800));
        private int startEdge = 500;
        Image<Bgr, Byte> overlay;
        double angle;

        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
 
            // Load the image file
            //img = new Image<Bgr, byte>("F:\\Projects\\Keyify\\data\\iphone\\IMG_0114.JPG");
            img = new Image<Bgr, byte>("F:\\Projects\\Keyify\\data\\iphone\\peterkey1.JPG");
            //img = new Image<Bgr, byte>("C:\\Users\\Andy\\Desktop\\SDPullups.jpg");
            imageBox1.Image = img;
            imageBox1.SetZoomScale(0.5, new Point(0, 0));
        }

        private void imageBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Point p = new Point((int)(imageBox1.HorizontalScrollBar.Value + e.Location.X / imageBox1.ZoomScale), 
                    (int)(imageBox1.VerticalScrollBar.Value + e.Location.Y / imageBox1.ZoomScale));


                if (Math.Abs(p.X - baseLine.P1.X) < Math.Abs(p.X - baseLine.P2.X))
                {
                    baseLine.P1 = new Point(p.X, p.Y);
                }
                else
                {
                    baseLine.P2 = new Point(p.X, p.Y);
                }

                overlay = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(Color.Black));
                overlay.Draw(baseLine, new Bgr(Color.Blue), 5);

                imageBox1.Image = img + overlay;

                angle = Math.Atan2((baseLine.P1.Y - baseLine.P2.Y), (baseLine.P1.X - baseLine.P2.X)) / Math.PI * 180;
                imageBox2.Image = (img + overlay).Rotate(Math.Abs(angle), new Bgr(Color.Black));

                toolStripStatusLabel1.Text = p.ToString() + " " + angle.ToString();
   
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                startEdge--;
            }
            if (e.KeyCode == Keys.Right)
            {
                startEdge++;
            }

            Image<Bgr, Byte> i = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(Color.Black));  
            i.Draw(new LineSegment2D(new Point(startEdge, 0), new Point(startEdge, 1500)), new Bgr(Color.Black), 5);

            imageBox2.Image = (img + overlay).Rotate(Math.Abs(angle), new Bgr(Color.Black));     
        }

        
    }
}
