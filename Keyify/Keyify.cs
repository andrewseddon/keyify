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

        public Keyify()
        {
            // Setup some defaults
            NumberOfCuts = 7;
        }

        public void LoadInput(string path)
        {
            _inputImage = new Image<Bgr, byte>(path);
            OnInputImageChanged(this, EventArgs.Empty);
        }

        public Image<Bgr, byte> GetInputImage()
        {
            // Make a copy and return as we dont want anything messing around with our image
            return  _inputImage.Copy();
        }

        public Image<Bgr, byte> GetTransformedImage()
        {
            // TODO Probably need to do proper homography correction
            // TODO this calculation does not always work
            double angle = Math.Atan2((_baseLineStart.Y - _baseLineEnd.Y), (_baseLineStart.X - _baseLineEnd.X)) / Math.PI * 180;
            return _inputImage.Copy().Rotate((180-angle), new Bgr(Color.Black));
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
                InterCutDistance = InterCutDistance;
            }
        }

        private Point transformedBaseLineStart;
        private Point transformedBaseLineEnd;

        private List<Point> _cuts = new List<Point>();
        
        public List<Point> Cuts
        {
            get { return _cuts; }
            set
            {
                _cuts = value;
                if (OnMarkupChanged != null)
                {
                    OnMarkupChanged(this, EventArgs.Empty);
                }
            }
        }

        private int _interCutDistance = 200;
        public int InterCutDistance
        {
            get { return _interCutDistance; }
            set
            {
                _interCutDistance = value;
                // Recalculate all existing cuts
                List<Point> newCuts = new List<Point>();
                for(int i=0; i<_cuts.Count; i++)
                {
                    Point p = new Point(_cuts[0].X + i * _interCutDistance, _cuts[0].Y); 
                    newCuts.Add(p);
                }
                Cuts = newCuts;
            }
        }

        public int NumberOfCuts
        {
            get { return _cuts.Count; }
            set
            {
                if (value > _cuts.Count)
                {
                    Point p = new Point(transformedBaseLineStart.X + 200, (transformedBaseLineStart.Y + 200));
                    for (int i = 0; i < (value - _cuts.Count); i++)
                    {
                        _cuts.Add(p);
                    }   
                }
                else if (value < _cuts.Count)
                {
                    for (int i = 0; i < (_cuts.Count - value); i++)
                    {
                        _cuts.Remove(_cuts.Last());
                    }
                }

                // Calculate where new cuts should go
                InterCutDistance = InterCutDistance;
            }
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
