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

        public Form1()
        {
            InitializeComponent();

            // Load the image file
            img = new Image<Bgr, byte>("F:\\Projects\\Keyify\\data\\iphone\\IMG_0114.JPG");

            Image<Bgr, Byte> overlay = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(Color.Black));
            overlay.Draw(new LineSegment2DF(new PointF(20.0f, 20.0f), new PointF(300.0f, 300.0f)), new Bgr(Color.Blue), 5);


            imageBox1.Image = img + overlay;//.Rotate(45.0, new Bgr(Color.Black));

        }

        private void imageBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Point p = new Point((int)(imageBox1.HorizontalScrollBar.Value + e.Location.X / imageBox1.ZoomScale), 
                    (int)(imageBox1.VerticalScrollBar.Value + e.Location.Y / imageBox1.ZoomScale));

                Image<Bgr, Byte> overlay = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(Color.Black));
                overlay.Draw(new LineSegment2D(p, new Point(300, 300)), new Bgr(Color.Blue), 5);

                imageBox1.Image = img + overlay;//.Rotate(45.0, new Bgr(Color.Black));

                toolStripStatusLabel1.Text = p.ToString();
   
            }
        }

        
    }
}
