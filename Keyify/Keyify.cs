using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

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

        private string _inputImageFileName;

        private Image<Bgr, byte> _inputImage;
        public Image<Bgr, byte>  InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value;
                _transformedImage = _inputImage.Copy();
                OnInputImageChanged(this, EventArgs.Empty);     
            }
        }

        private Image<Bgr, byte> _transformedImage;
        public Image<Bgr, byte> TransformedImage
        {
            get { return _transformedImage; }
            set
            {
                _transformedImage = value;
                OnTransformedImageChanged(this, EventArgs.Empty);
            }
        }

        private double _rotationAngle = 0;

        bool _baseLineHasChanged = true;

        public Key _key = new Key();

        public Keyify()
        {
            // Setup some defaults
            NumberOfCuts = 5; 
        }

        public void SaveXml(string filename)
        {
            _key._filename = _inputImageFileName;
            _key._keyCode = "";
            _key._baseLineStart = BaseLineStart;
            _key._baseLineEnd = BaseLineEnd;
            _key._cuts = _cuts;
            _key._coinBottomLeft = CoinBottomLeft;
            _key._coinTopRight = CoinTopRight;
            //_measuredCuts = new Point[20];

            FileStream fs = new FileStream(filename, FileMode.Create);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_key.GetType());
            x.Serialize(fs, _key);
            fs.Close();
        }

        public void LoadXml(string filename)
        {
                  FileStream fs = new FileStream(filename, FileMode.Open);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_key.GetType());
            _key = (Key)x.Deserialize(fs);
            fs.Close();

            LoadInput(_key._filename);
            //_key._keyCode = "";
            BaseLineEnd = _key._baseLineEnd;
            BaseLineStart = _key._baseLineStart; 
            _cuts = _key._cuts;
            CoinBottomLeft = _key._coinBottomLeft;
            CoinTopRight = _key._coinTopRight;
            //_measuredCuts = new Point[20];               
        }

        public void LoadInput(string path)
        {
            _inputImageFileName = path;
            _inputImage = new Image<Bgr, byte>(path);
            _transformedImage = _inputImage.Copy();

            // Reset position of cuts
            _cuts[0] = new Point(_inputImage.Width / 2, _inputImage.Height / 2);
            //CalculateCutPositions();

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
                _rotationAngle = 180.0 - Math.Atan2((_baseLineStart.Y - _baseLineEnd.Y), (_baseLineStart.X - _baseLineEnd.X)) / Math.PI * 180;
                _transformedImage = _inputImage.Copy().Rotate(_rotationAngle, new Bgr(Color.Black));

                if (OnTransformedImageChanged != null)
                    OnTransformedImageChanged(this, EventArgs.Empty);

                _baseLineHasChanged = false;
            }
        }

        private Point _baseLineStart = new Point(0,0);
        private Point _baseLineEnd = new Point(0,0);

        public Point BaseLineStart
        {
            get { return _baseLineStart;  }
            set
            {
                _baseLineStart = value;
                _baseLineHasChanged = true;

                CalculateCutPositions();
                CalculateTransformedBaseLines();
                
                if (OnMarkupChanged!=null)
                    OnMarkupChanged(this, EventArgs.Empty);
                if (OnTransformedImageChanged != null)
                    OnTransformedImageChanged(this, EventArgs.Empty);    
            }   
        }

        private void CalculateTransformedBaseLines()
        {
            PointF center = new PointF(_inputImage.Width * 0.5f, _inputImage.Height * 0.5f);
            _rotationAngle = 180.0 - Math.Atan2((_baseLineStart.Y - _baseLineEnd.Y), (_baseLineStart.X - _baseLineEnd.X)) / Math.PI * 180;
            RotationMatrix2D<float> rotationMatrix = new RotationMatrix2D<float>(center, -_rotationAngle, 1);

            PointF[] p = new PointF[] { new PointF(_baseLineStart.X, _baseLineStart.Y) ,
                new PointF(BaseLineEnd.X, BaseLineEnd.Y)};
            rotationMatrix.RotatePoints(p);
            transformedBaseLineStart = new Point((int)p[0].X, (int)p[0].Y);
            transformedBaseLineEnd = new Point((int)p[1].X, (int)p[1].Y);
        }

        public Point BaseLineEnd
        {
            get { return _baseLineEnd; }
            set 
            { 
                _baseLineEnd = value;
                _baseLineHasChanged = true;

                CalculateTransformedBaseLines();
             
                if (OnMarkupChanged != null)
                    OnMarkupChanged(this, EventArgs.Empty);
                    //OnTransformedImageChanged(this, EventArgs.Empty);
                if (OnTransformedImageChanged != null)
                    OnTransformedImageChanged(this, EventArgs.Empty);   
            }
        }

        public Point transformedBaseLineStart = new Point();
        public Point transformedBaseLineEnd = new Point();

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
    public class Key
    {
        // User given name for the key
        public string _name;

        // Input image filename
        public string _filename;

        // Any manufactuer info stamped on the key
        public string _keyCode;

        //
        // Markup
        //
        public Point _baseLineStart, _baseLineEnd;

        public Point[] _cuts; // = new Point[20];
        public Point[] Cuts 
        {
            get { return _cuts; }
            set { _cuts = value; }
        }

        public Point _coinBottomLeft;
        public Point CoinBottomLeft
        {
            get { return _coinBottomLeft; }
            set { _coinBottomLeft = value; }
        }

        public Point _coinTopRight;


        //
        // Micrometer
        //
        public Point[] _measuredCuts = new Point[20];
    }
}
