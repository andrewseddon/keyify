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
    public partial class Form1 : Form
    {
        private Image<Bgr, Byte> img;


        LineSegment2D baseLine = new LineSegment2D(new Point(0, 800), new Point(2000, 800));

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
 
            // Load the image file
            //img = new Image<Bgr, byte>("F:\\Projects\\Keyify\\data\\iphone\\IMG_0114.JPG");
            img = new Image<Bgr, byte>("F:\\Projects\\Keyify\\data\\iphone\\peterkey1.JPG");
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

                Image<Bgr, Byte> overlay = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(Color.Black));
                overlay.Draw(baseLine, new Bgr(Color.Blue), 5);

               // overlay.Draw(new Ellipse(new MCvBox2D(

                imageBox1.Image = img + overlay;//.Rotate(45.0, new Bgr(Color.Black));



                double angle = Math.Atan2((baseLine.P1.Y - baseLine.P2.Y), (baseLine.P1.X - baseLine.P2.X)) / Math.PI * 180;
                imageBox2.Image = (img + overlay).Rotate(Math.Abs(angle), new Bgr(Color.Black));

                toolStripStatusLabel1.Text = p.ToString() + " " + angle.ToString();
   
            }
        }

        
    }
}
