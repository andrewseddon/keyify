﻿  using System;
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
        // How much error in measurement of cut depth are we going to allow, research indiciates <0.5mm
        // should produce a working key
        const double MaxPermissableCutDepthError = 0.25;
        public const string KeyFileExtenstion = ".keyify";

        public event ImageChangedEventHandler OnInputImageChanged;
        public event ImageChangedEventHandler OnTransformedImageChanged;
        public event ImageChangedEventHandler OnCalibrationImageChanged;
        public event MarkupChangedEventHandler OnMarkupChanged;

        private string _inputImageFileName;

        private Image<Bgr, byte> _inputImage;
        public Image<Bgr, byte>  InputImage
        {
            get { return _inputImage; }
            set
            {
                _inputImage = value;
                //_transformedImage = _inputImage.Copy();
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

        private Image<Bgr, byte> _calibrationImage;
        public Image<Bgr, byte>  CalibrationImage
        {
            get { return _calibrationImage; }
        }

        IntrinsicCameraParameters _intrinsicParameters = new IntrinsicCameraParameters();

        private double _rotationAngle = 0;

        bool _baseLineHasChanged = true;

        private bool _hasMeasurementError = false;
        public bool HasMeasurementError
        {
            get { return _hasMeasurementError; }
        }

        public Key _key = new Key();

        public Keyify()
        {
            // Setup some defaults
            NumberOfCuts = 5;
        }

        public void SaveXml(string filename)
        {
            _key._filename = _inputImageFileName;
            //_key._keyCode = "";
            _key._baseLineStart = BaseLineStart;
            _key._baseLineEnd = BaseLineEnd;
            _key._cuts = _cuts;
            _key._numberOfCuts = NumberOfCuts;
            _key._interCutDistance = InterCutDistance;
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
            NumberOfCuts = _key._numberOfCuts;
            InterCutDistance = _key._interCutDistance;
            CoinBottomLeft = _key._coinBottomLeft;
            CoinTopRight = _key._coinTopRight;
            //_measuredCuts = new Point[20];               
        }

        public void LoadInput(string path)
        {
            _inputImageFileName = path;
            _inputImage = new Image<Bgr, byte>(path);
            //_inputImage = CorrectCameraDistortion(path + "\\1.calibration", _inputImage);
            _transformedImage = _inputImage.Copy();

            // Reset position of cuts
            _cuts[0] = new Point(_inputImage.Width / 2, _inputImage.Height / 2);
            //CalculateCutPositions();

            OnInputImageChanged(this, EventArgs.Empty);     
        }

        private void UpdateImages()
        {
            if (OnInputImageChanged != null)
                OnInputImageChanged(this, EventArgs.Empty);
            if (OnMarkupChanged != null)
                OnMarkupChanged(this, EventArgs.Empty);
        }

        public void CorrectCameraDistortion(string calibrationFile)
        {
             FileStream fs = new FileStream(calibrationFile, FileMode.Open);
             System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_intrinsicParameters.GetType());
             _intrinsicParameters = (IntrinsicCameraParameters)x.Deserialize(fs);
             fs.Close();

             Matrix<float> mapx = new Matrix<float>(new Size(_inputImage.Width, _inputImage.Height));
             Matrix<float> mapy = new Matrix<float>(new Size(_inputImage.Width, _inputImage.Height));
             _intrinsicParameters.InitUndistortMap(_inputImage.Width, _inputImage.Height, out mapx, out mapy);  

             Image<Bgr, byte> img = _inputImage.Clone();
             CvInvoke.cvRemap(_inputImage.Ptr, img.Ptr, mapx.Ptr, mapy.Ptr, 8 /*(int)INTER.CV_INTER_LINEAR | (int)WARP.CV_WARP_FILL_OUTLIERS*/, new MCvScalar(0));

             _inputImage = img;

             UpdateImages();
        }

        public void CalibrateCamera(string directory)
        {
            DirectoryInfo di = new DirectoryInfo(directory);
            FileInfo[] rgFiles = di.GetFiles("*.jpg");

            Size chessboardSize = new Size(5, 8);
            int successes = 0;
            int numberOfCorners = chessboardSize.Width * chessboardSize.Height;
            int chessboard_num = rgFiles.Length;
            int counter = 0;

            MCvPoint3D32f[][] object_points1 = new MCvPoint3D32f[chessboard_num][];
            PointF[][] image_points1 = new PointF[chessboard_num][];

             // Process all chessboard images in this directory
             foreach(FileInfo fi in rgFiles)
             {
                // Find all the corners on a given chessboard image
                PointF[] corners;
                _calibrationImage = new Image<Bgr, byte>(fi.FullName);
                Image<Gray, byte> grayImage = _calibrationImage.Convert<Gray, byte>();
                bool patternFound = CameraCalibration.FindChessboardCorners(grayImage, new Size(5, 8), Emgu.CV.CvEnum.CALIB_CB_TYPE.ADAPTIVE_THRESH, out corners);
                // grayImage.FindCornerSubPix(new PointF[][] { corners }, new Size(10, 10), new Size(-1, -1), new MCvTermCriteria(0.05));
                CvInvoke.cvDrawChessboardCorners(_calibrationImage.Ptr, chessboardSize, corners, corners.Length, patternFound ? 1 : 0);

                MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_PLAIN, 5, 5);
                _calibrationImage.Draw(counter++.ToString() + ":" + fi.Name + " " + patternFound.ToString(), ref font , new Point(0, 200), new Bgr(Color.Red)); 

                if (OnCalibrationImageChanged != null)
                    OnCalibrationImageChanged(this, EventArgs.Empty);

                // TODO bit of a hack so that the display updates, should probably do this in a BackgroundWorker
                System.Windows.Forms.Application.DoEvents();

                if (patternFound && (corners.Length == (chessboardSize.Height * chessboardSize.Width)))
                {
                    object_points1[successes] = new MCvPoint3D32f[numberOfCorners];
                    for (int j = 0; j < numberOfCorners; j++)
                    {
                        image_points1[successes] = corners;
                        object_points1[successes][j].x = j / chessboardSize.Width;
                        object_points1[successes][j].y = j % chessboardSize.Width;
                        object_points1[successes][j].z = 0.0f;
                    }
                    successes++;
                }
             }

             // TODO Hack to get arrays of the correct length, should probably use a collection
             MCvPoint3D32f[][] object_points = new MCvPoint3D32f[successes][];
             PointF[][] image_points = new PointF[successes][];
             for (int i = 0; i < successes; i++)
             {
                 object_points[i] = object_points1[i];
                 image_points[i] = image_points1[i];
             }


             _intrinsicParameters = new IntrinsicCameraParameters();
             ExtrinsicCameraParameters[] extrinsicParameters = new ExtrinsicCameraParameters[successes];
             for (int i = 0; i < successes; i++)
                 extrinsicParameters[i] = new ExtrinsicCameraParameters();

             CameraCalibration.CalibrateCamera(object_points, image_points, new Size(_calibrationImage.Width, _calibrationImage.Height), _intrinsicParameters,Emgu.CV.CvEnum.CALIB_TYPE.DEFAULT, out extrinsicParameters);

             FileStream fs = new FileStream(directory + "\\1.calibration", FileMode.Create);
             System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(_intrinsicParameters.GetType());
             x.Serialize(fs, _intrinsicParameters);
             fs.Close();
             
             CorrectCameraDistortion(directory + "\\1.calibration");
        }

        public void Homography()
        {
#if false
            PointF[] src = new PointF[] 
            { 
                new PointF(_coinBottomLeft.X, _coinBottomLeft.Y) ,
                new PointF(_coinBottomLeft.X, _coinTopRight.Y) ,
                new PointF(_coinTopRight.X, _coinTopRight.Y) ,
                new PointF(_coinTopRight.X, _coinBottomLeft.Y) ,
            };
            
            float coinDiameter = Math.Abs(_coinBottomLeft.Y - _coinTopRight.Y);
            PointF[] dst = new PointF[] 
            { 
                new PointF(_coinBottomLeft.X, _coinBottomLeft.Y) ,
                new PointF(_coinBottomLeft.X, _coinTopRight.Y) ,
                new PointF(_coinBottomLeft.X + coinDiameter, _coinTopRight.Y) ,
                new PointF(_coinBottomLeft.X + coinDiameter, _coinBottomLeft.Y) ,
            };
#endif
#if true
            PointF[] src = new PointF[] 
            { 
                new PointF(311, 1178) ,
                new PointF(298, 362) ,
                new PointF(1697, 342) ,
                new PointF(1721, 1137) ,
            };

            float coinDiameter = Math.Abs(_coinBottomLeft.Y - _coinTopRight.Y);
            PointF[] dst = new PointF[] 
            { 
                new PointF(0, _inputImage.Height) ,
                new PointF(0, 300) ,
                new PointF(_inputImage.Width, 300) ,
                new PointF(_inputImage.Width, _inputImage.Height) ,
            };
#endif
            HomographyMatrix homographyMatrix;
            homographyMatrix = CameraCalibration.FindHomography(src, dst, Emgu.CV.CvEnum.HOMOGRAPHY_METHOD.LMEDS, 2.0);
            _transformedImage = _inputImage.WarpPerspective(homographyMatrix, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, Emgu.CV.CvEnum.WARP.CV_WARP_DEFAULT, new Bgr(Color.Black));
        }

        public Image<Bgr, byte> GetTransformedImage()
        {
            //_transformedImage._EqualizeHist();
            return _transformedImage;
        }

        public void CalculateTransform()
        {
          //  Homography();

#if true
            if (_baseLineHasChanged)
            {
                _rotationAngle = 180.0 - Math.Atan2((_baseLineStart.Y - _baseLineEnd.Y), (_baseLineStart.X - _baseLineEnd.X)) / Math.PI * 180;

                _transformedImage.Dispose();
                _transformedImage = _inputImage.Rotate(_rotationAngle, new Bgr(Color.Black));            

                if (OnTransformedImageChanged != null)
                    OnTransformedImageChanged(this, EventArgs.Empty);

                _baseLineHasChanged = false;
            }
#endif
            //_transformedImage = _inputImage.GrabCut(new Rectangle(_coinTopRight, new Size(_coinTopRight.X - _coinBottomLeft.X, _coinBottomLeft.Y - _coinTopRight.Y)), 5).Convert<Bgr, byte>();
            //_transformedImage = _inputImage.GrabCut(new Rectangle(200, 200, 100, 100), 1).Convert<Bgr, byte>();
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

            CalculateErrorReport();

            if (OnMarkupChanged != null)
                OnMarkupChanged(this, EventArgs.Empty);
        }

        public void CalculateErrorReport()
        {
            _hasMeasurementError = false;  

            for (int i = 0; i < _numberOfCuts; i++)
            {
                _key.CalculatedCuts[i].Y = (float)GetCutRealDepth(i);
                _key.CalculatedCuts[i].X = (float)GetCutRealPosition(i);
                _key.CutError[i].X = _key.CalculatedCuts[i].X - _key.MeasuredCuts[i].X;
                _key.CutError[i].Y = _key.CalculatedCuts[i].Y - _key.MeasuredCuts[i].Y;
                if (Math.Abs(_key.CutError[i].Y) > MaxPermissableCutDepthError)
                    _hasMeasurementError = true;
            }
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

        public SizeF CoinSize
        {
            get
            {
                return new SizeF((float)(_coinTopRight.X - _coinBottomLeft.X), (float)(_coinBottomLeft.Y - _coinTopRight.Y));
            }
        }

        public double CoinDiameter = 20.335;

        public double GetCutRealDepth(int index)
        {
            return (double)Math.Abs((_cuts[index].Y - transformedBaseLineStart.Y) / ((double)(CoinBottomLeft.Y - CoinTopRight.Y) / CoinDiameter));
        }

        public double GetCutRealPosition(int index)
        {
            return (double)Math.Abs((_cuts[index].X - transformedBaseLineStart.X) / ((double)(CoinBottomLeft.X - CoinTopRight.X) / CoinDiameter));
        }

        
    }

    /// <summary>
    /// Parameters for a single key
    /// </summary>
    public class Key
    {
        public Key()
        {
        }

        // User given name for the key
        public string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        // Input image filename
        public string _filename;

        // Any manufactuer info stamped on the key
        public string _keyCode;
        public string KeyCode
        {
            get { return _keyCode; }
            set { _keyCode = value; }
        }

        //
        // Markup
        //
        public Point _baseLineStart;
        public Point _baseLineEnd;

        public Point[] _cuts; // = new Point[20];
        public int _numberOfCuts;
        public int _interCutDistance;
        /*
        public Point[] Cuts 
        {
            get { return _cuts; }
            set { _cuts = value; }
        }
         */

        public Point _coinBottomLeft;
        /*
        public Point CoinBottomLeft
        {
            get { return _coinBottomLeft; }
            set { _coinBottomLeft = value; }
        }
         * */

        public Point _coinTopRight;


        public PointF[] _calculatedCuts = new PointF[20];
        public PointF[] CalculatedCuts
        {
            get { return _calculatedCuts; }
            set { _calculatedCuts = value; }
        }

   
        //
        // Micrometer
        //
        public PointF[] _measuredCuts = new PointF[20];
        public PointF[] MeasuredCuts
        {
            get { return _measuredCuts; }
            set { _measuredCuts = value; }
        }


        public PointF[] _cutError = new PointF[20];
        public PointF[] CutError
        {
            get { return _cutError; }
            set { _cutError = value; }
        }

    }

    /*
    public class Cut
    {
        public static readonly Cut Empty;

        public Cut()
        {
            Position = 0;
            Depth = 0;
        }

        public double _position = 0;
        public double Position
        {
            get { return _position; }
            set { _position = value; }
        }
        public double _depth = 0;
        public double Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }
    }
     */
}
