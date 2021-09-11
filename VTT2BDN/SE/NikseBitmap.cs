using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nikse.SubtitleEdit.Core.Common
{
    class NikseBitmap
    {
        private int _width;
        public int Width
        {
            get => _width;
            private set
            {
                _width = value;
                _widthX4 = _width * 4;
            }
        }

        public int Height { get; private set; }

        private byte[] _bitmapData;
        private int _pixelAddress;
        private int _widthX4;

        public NikseBitmap(Bitmap inputBitmap)
        {
            if (inputBitmap == null)
            {
                return;
            }

            Width = inputBitmap.Width;
            Height = inputBitmap.Height;
            bool createdNewBitmap = false;
            if (inputBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                var newBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height, PixelFormat.Format32bppArgb);
                using (var gr = Graphics.FromImage(newBitmap))
                {
                    gr.DrawImage(inputBitmap, 0, 0);
                }
                inputBitmap = newBitmap;
                createdNewBitmap = true;
            }

            var bitmapData = inputBitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            _bitmapData = new byte[bitmapData.Stride * Height];
            Marshal.Copy(bitmapData.Scan0, _bitmapData, 0, _bitmapData.Length);
            inputBitmap.UnlockBits(bitmapData);
            if (createdNewBitmap)
            {
                inputBitmap.Dispose();
            }
        }

        public void SetTransparentTo(Color transparent)
        {
            var buffer = new byte[4];
            buffer[0] = transparent.B;
            buffer[1] = transparent.G;
            buffer[2] = transparent.R;
            buffer[3] = transparent.A;
            for (var i = 0; i < _bitmapData.Length; i += 4)
            {
                if (_bitmapData[i + 3] == 0)
                {
                    Buffer.BlockCopy(buffer, 0, _bitmapData, i, 4);
                }
            }
        }

        public Color GetPixel(int x, int y)
        {
            _pixelAddress = (x * 4) + (y * _widthX4);
            return Color.FromArgb(_bitmapData[_pixelAddress + 3], _bitmapData[_pixelAddress + 2], _bitmapData[_pixelAddress + 1], _bitmapData[_pixelAddress]);
        }
    }
}
