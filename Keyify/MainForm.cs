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
            transformedDisplay.Image = _model.GetTransformedImage() + _transformedMarkup;
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
            inputDisplay.Image = _model.GetInputImage() + _inputMarkup;

            if(_transformedMarkup == null)
                _transformedMarkup = new Image<Bgr, byte>(_model.GetTransformedImage().Width, _model.GetTransformedImage().Height);
            else
                _transformedMarkup.SetValue(new Bgr(Color.Black));

            foreach (Point p in _model.Cuts)
            {
                _transformedMarkup.Draw(new Cross2DF(new PointF(p.X, p.Y), 20, 300), new Bgr(Color.Green), 3);
            }

             transformedDisplay.Image = _model.GetTransformedImage() + _transformedMarkup; 
        }

        void _model_OnInputImageChanged(object sender, EventArgs e)
        {
            // Create a markup image the same size as the image we are marking up
            _inputMarkup = new Image<Bgr, byte>(_model.GetInputImage().Width, _model.GetInputImage().Height);
            _transformedMarkup = new Image<Bgr, byte>(_model.GetTransformedImage().Width, _model.GetTransformedImage().Height);
            // Paint to imageBox
            inputDisplay.Image = _model.GetInputImage() + _inputMarkup;
        }

        private void imageBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // Compute where the user actually clicked in the image
                Point p = new Point((int)(inputDisplay.HorizontalScrollBar.Value + e.Location.X / inputDisplay.ZoomScale), 
                    (int)(inputDisplay.VerticalScrollBar.Value + e.Location.Y / inputDisplay.ZoomScale));

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

            // Numbers 1 to 3 select tab
            if (e.KeyCode.ToString() == "D1")
                tabControl1.SelectedIndex = 0;
            if (e.KeyCode.ToString() == "D2")
                tabControl1.SelectedIndex = 1;
            if (e.KeyCode.ToString() == "D3")
                tabControl1.SelectedIndex = 2;

            // Numbers 4 to 9 select the number of bites
            if (e.KeyCode.ToString() == "D4")
                _model.NumberOfCuts = 4;
            if (e.KeyCode.ToString() == "D5")
                _model.NumberOfCuts = 5;
            if (e.KeyCode.ToString() == "D6")
                _model.NumberOfCuts = 6;
            if (e.KeyCode.ToString() == "D7")
                _model.NumberOfCuts = 7;
            if (e.KeyCode.ToString() == "D8")
                _model.NumberOfCuts = 8;
            if (e.KeyCode.ToString() == "D9")
                _model.NumberOfCuts = 9;


            // Up/Down modifys intercut distance
            if (e.KeyCode == Keys.Up)
                _model.InterCutDistance += 1;
            if (e.KeyCode == Keys.Down)
                _model.InterCutDistance -= 1;

            if (e.KeyCode == Keys.Left)
                _model.FirstCut -= 1;
            if (e.KeyCode == Keys.Right)
                _model.FirstCut += 1;
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            // Calculate new transform when we change tabs
            _model.CalculateTransform();

            GenerateStats();
        }

        private void GenerateStats()
        {
            statsTextBox.Text = "Intercut Distance (pixels): " + _model.InterCutDistance.ToString();
        }

        private void tabControl1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                _model.LoadInput(file);
            }
        }

        private void tabControl1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void transformedDisplay_MouseClick(object sender, MouseEventArgs e)
        {
            //
            // Update key depths
            //

            // Compute where the user actually clicked in the image
            Point clicked = new Point((int)(transformedDisplay.HorizontalScrollBar.Value + e.Location.X / transformedDisplay.ZoomScale),
                (int)(transformedDisplay.VerticalScrollBar.Value + e.Location.Y / transformedDisplay.ZoomScale));

            // See if user clicked within a few X pixels of a cut
            List<Point> cuts = _model.Cuts;
            for (int i=0; i<cuts.Count; i++)
            {
                if (Math.Abs(cuts[i].X - clicked.X) < 10)
                {
                    Point p = cuts[i];
                    p.Y = clicked.Y;
                    cuts[i] = p;
                }
            }
            _model.Cuts = cuts;
        }

        private void inputDisplay_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // If user double clicks they want to set the shoulder end of the baseline
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                // Compute where the user actually clicked in the image
                Point p = new Point((int)(inputDisplay.HorizontalScrollBar.Value + e.Location.X / inputDisplay.ZoomScale),
                    (int)(inputDisplay.VerticalScrollBar.Value + e.Location.Y / inputDisplay.ZoomScale));

                _model.BaseLineStart = new Point(p.X, p.Y);
            }
        }

        
    }
}
