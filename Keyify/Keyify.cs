using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Keyify
{
    class Keyify
    {
        Image<Bgr, byte> _inputImage;

        public void LoadInput(string path)
        {
            _inputImage = new Image<Bgr, byte>(path);
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
        List<PointF> _cutPositions = new List<double>();
    }
}
