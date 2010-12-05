using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Keyify
{
    public delegate void ImageChangedEventHandler(object sender, EventArgs e);
    public delegate void MarkupChangedEventHandler(object sender, EventArgs e);

    class Keyify
    {
        public event ImageChangedEventHandler OnInputImageChanged;
        public event ImageChangedEventHandler OnTransformedImageChanged;
        public event MarkupChangedEventHandler OnMarkupChanged;

        private Image<Bgr, byte> _inputImage;
        private Image<Bgr, byte> _transformedImage;

        bool _baseLineHasChanged = true;

        public Keyify()
        {
            // Setup some defaults
            NumberOfCuts = 5;
        }

        public void LoadInput(string path)
        {
            _inputImage = new Image<Bgr, byte>(path);
            _transformedImage = _inputImage.Copy();
            OnInputImageChanged(this, EventArgs.Empty);
            
        }

        public Image<Bgr, byte> GetInputImage()
        {
            // Make a copy and return as we dont want anything messing around with our image
            return _inputImage;//.Copy();
        }

        public Image<Bgr, byte> GetTransformedImage()
        {
            return _transformedImage;//.Copy();
        }

        public void CalculateTransform()
        {
            if (_baseLineHasChanged)
            {
                // TODO Probably need to do proper homography correction
                // TODO this calculation does not always work
                double angle = Math.Atan2((_baseLineStart.Y - _baseLineEnd.Y), (_baseLineStart.X - _baseLineEnd.X)) / Math.PI * 180;
                _transformedImage = _inputImage.Copy().Rotate((180 - angle), new Bgr(Color.Black));

                // Reset position of cuts
                _cuts[0] = new Point(_baseLineStart.X + 100, _baseLineStart.Y - 100);

                CalculateCutPositions();

                if (OnTransformedImageChanged != null)
                {
                    OnTransformedImageChanged(this, EventArgs.Empty);
                }

                _baseLineHasChanged = false;
            }
        }

        private Point _baseLineStart, _baseLineEnd;
        public Point BaseLineStart
        {
            get { return _baseLineStart;  }
            set
            {
                _baseLineStart = value;
                if (OnMarkupChanged!=null)
                {
                    OnMarkupChanged(this, EventArgs.Empty);
                }
                if (OnTransformedImageChanged != null)
                {
                    OnTransformedImageChanged(this, EventArgs.Empty);
                }

                // TODO Calculate transformed coordinate
                transformedBaseLineStart = value;
                InterCutDistance = InterCutDistance;

                _baseLineHasChanged = true;
            }   
        }
        public Point BaseLineEnd
        {
            get { return _baseLineEnd; }
            set 
            { 
                _baseLineEnd = value;
                if (OnMarkupChanged != null)
                {
                    OnMarkupChanged(this, EventArgs.Empty);
                    OnTransformedImageChanged(this, EventArgs.Empty);
                }
                if (OnTransformedImageChanged != null)
                {
                    OnTransformedImageChanged(this, EventArgs.Empty);
                }

                // TODO Calculate transformed coordinate
                transformedBaseLineEnd = value;
                _baseLineHasChanged = true;
            }
        }

        public Point transformedBaseLineStart;
        public Point transformedBaseLineEnd;

        //private List<Point> _cuts = new List<Point>();
        private Point[] _cuts = new Point[20];
       
        public void SetCut(int cutIndex, int depth)
        {
            if (cutIndex < NumberOfCuts)
            {
                _cuts[cutIndex].Y = depth;
            }

            if (OnMarkupChanged != null)
                OnMarkupChanged(this, EventArgs.Empty);
        }

        public Point GetCut(int cutIndex)
        {
            return _cuts[cutIndex];
        }

        private int _interCutDistance = 100;
        public int InterCutDistance
        {
            get { return _interCutDistance; }
            set
            {
                _interCutDistance = value;
                CalculateCutPositions();      
            }
        }

        private void CalculateCutPositions()
        {
            // Recalculate all existing cuts
            for (int i = 0; i < _numberOfCuts; i++)
            {
                _cuts[i].X = _cuts[0].X + i * _interCutDistance;
            }

            if (OnMarkupChanged != null)
                OnMarkupChanged(this, EventArgs.Empty);
        }

        public int FirstCut
        {
            get { return _cuts[0].X; }
            set
            {
                _cuts[0].X = value;
                CalculateCutPositions();
            }
        }

        private int _numberOfCuts = 5;
        public int NumberOfCuts
        {
            get { return _numberOfCuts; }
            set
            {
                _numberOfCuts = value;
                CalculateCutPositions();
            }
        }

        private Point _coinBottomLeft = new Point(1000,1000);
        private Point _coinTopRight = new Point(1500,100);
        public Point CoinBottomLeft
        {
            get { return _coinBottomLeft; }
            set 
            { 
                _coinBottomLeft = value;

                if (OnMarkupChanged != null)
                    OnMarkupChanged(this, EventArgs.Empty);
            }
        }
        
        public Point CoinTopRight
        {
            get { return _coinTopRight; }
            set 
            { 
                _coinTopRight = value;

                if (OnMarkupChanged != null)
                    OnMarkupChanged(this, EventArgs.Empty);
            }
        }

        public double CoinDiameter = 20.335;

        public double GetCutRealDepth(int index)
        {
            return (double)(_cuts[index].Y - transformedBaseLineStart.Y) / ((double)(CoinBottomLeft.Y - CoinTopRight.Y) / CoinDiameter);
        }
    }

    /// <summary>
    /// Parameters for a single key
    /// </summary>
    class Key
    {
        // User given name for the key
        public string Name;

        // Any manufactuer info stamped on the key
        public string KeyCode;

        // Cut positions are given relative to the baseline
        List<PointF> _cutPositions = new List<PointF>();
    }
}
