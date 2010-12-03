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
        private Keyify _model = new Keyify();

        private Image<Bgr, Byte> _inputImage;
        private Image<Bgr, Byte> _inputMarkup;
        private Image<Bgr, Byte> _transformedMarkup;
        
        public MainForm()
        {
            InitializeComponent(); 
            this.WindowState = FormWindowState.Maximized;
            //imageBox1.SetZoomScale(0.5, new Point(0, 0));

            _model.OnInputImageChanged += new ImageChangedEventHandler(_model_OnInputImageChanged);
            _model.OnMarkupChanged += new MarkupChangedEventHandler(_model_OnMarkupChanged);
            _model.OnTransformedImageChanged += new ImageChangedEventHandler(_model_OnTransformedImageChanged);

            // "F:\\Projects\\Keyify\\data\\iphone\\peterkey1.JPG"
            // "C:\\Users\\Andy\\Desktop\\SDPullups.jpg"
            _model.LoadInput("F:\\Projects\\Keyify\\data\\iphone\\IMG_0114.JPG"); 
        }

        void _model_OnTransformedImageChanged(object sender, EventArgs e)
        {
            imageBox2.Image = _model.GetTransformedImage() + _transformedMarkup;
        }

        void _model_OnMarkupChanged(object sender, EventArgs e)
        {
            // Paint all the markup
            _inputMarkup.SetValue(new Bgr(Color.Black));
            _inputMarkup.Draw(new Cross2DF(new PointF(_model.BaseLineStart.X, _model.BaseLineStart.Y), 20, 100), new Bgr(Color.Green), 3);
            if ((_model.BaseLineStart.X != 0) && (_model.BaseLineEnd.X != 0))
            {
                _inputMarkup.Draw(new LineSegment2D(_model.BaseLineStart, _model.BaseLineEnd), new Bgr(Color.Blue), 3);
            }  
            imageBox1.Image = _model.GetInputImage() + _inputMarkup;

            _transformedMarkup = new Image<Bgr, byte>(_model.GetTransformedImage().Width, _model.GetTransformedImage().Height);
            foreach (Point p in _model.Cuts)
            {
                _transformedMarkup.Draw(new Cross2DF(new PointF(p.X, p.Y), 20, 100), new Bgr(Color.Green), 3);
            }

            //_transformedMarkup.Draw(new LineSegment2D(_model.BaseLineStart, _model.BaseLineEnd), new Bgr(Color.Blue), 3);

            imageBox2.Image = _model.GetTransformedImage() + _transformedMarkup;
        }

        void _model_OnInputImageChanged(object sender, EventArgs e)
        {
            // Create a markup image the same size as the image we are marking up
            _inputMarkup = new Image<Bgr, byte>(_model.GetInputImage().Width, _model.GetInputImage().Height);
            _transformedMarkup = new Image<Bgr, byte>(_model.GetTransformedImage().Width, _model.GetTransformedImage().Height);
            // Paint to imageBox
            imageBox1.Image = _model.GetInputImage() + _inputMarkup;
        }

        private void imageBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // Compute where the user actually clicked in the image
                Point p = new Point((int)(imageBox1.HorizontalScrollBar.Value + e.Location.X / imageBox1.ZoomScale), 
                    (int)(imageBox1.VerticalScrollBar.Value + e.Location.Y / imageBox1.ZoomScale));

                if (_model.BaseLineStart.X == 0)
                {
                    _model.BaseLineStart = new Point(p.X, p.Y);
                }
                else if (_model.BaseLineEnd.X == 0)
                {
                    _model.BaseLineEnd = new Point(p.X, p.Y);
                }
                // Move the start/end of the baseline to that point depending on which was closer to the click
                // TODO Should probably use distance vector here
                else if (Math.Abs(p.X - _model.BaseLineStart.X) < Math.Abs(p.X - _model.BaseLineEnd.X))
                {
                    _model.BaseLineStart = new Point(p.X, p.Y);
                }
                else
                {
                    _model.BaseLineEnd = new Point(p.X, p.Y);
                }

                // Display coordinates where the user clicked
                toolStripStatusLabel1.Text = p.ToString();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            toolStripStatusLabel1.Text = e.KeyCode.ToString();
            /*
            if (e.KeyCode == Keys.Left)
            {
                startEdge--;
            }
            if (e.KeyCode == Keys.Right)
            {
                startEdge++;
            }

            Image<Bgr, Byte> i = new Image<Bgr, byte>(_inputImage.Width, _inputImage.Height, new Bgr(Color.Black));  
            i.Draw(new LineSegment2D(new Point(startEdge, 0), new Point(startEdge, 1500)), new Bgr(Color.Black), 5);

            imageBox2.Image = (_inputImage + _inputMarkup).Rotate(Math.Abs(angle), new Bgr(Color.Black));     
             */
        }

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            toolStripStatusLabel1.Text = e.KeyCode.ToString();
            if (e.KeyCode.ToString() == "D5")
                _model.NumberOfCuts = 5;
            if (e.KeyCode.ToString() == "D9")
                _model.NumberOfCuts = 9;

            if (e.KeyCode == Keys.Up)
                _model.InterCutDistance += 5;
            if (e.KeyCode == Keys.Down)
                _model.InterCutDistance -= 5;
        }

        
    }
}
