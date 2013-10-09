﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Catrobat.Core.Services;

namespace Catrobat.IDEWindowsPhone.Services
{
    public class ImageSourceConversionServicePhone : IImageSourceConversionService
    {
        public object ConvertToLocalImageSource(byte[] data, int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height);
            bitmap.FromByteArray(data);

            return bitmap;
        }

        public void ConvertToBytes(object inputData, out byte[] outputData, out int outputWidth, out int outputHeight)
        {
            WriteableBitmap writableBitmap = null;

            if (inputData is BitmapImage)
            {
                writableBitmap = new WriteableBitmap((BitmapImage)inputData);
            }
            else if (inputData is WriteableBitmap)
            {
                writableBitmap = (WriteableBitmap)inputData;
            }
            else
            {
                throw new ArgumentException("data must be of type WritableBitmap or BitmaoImage");
            }

            outputData = writableBitmap.ToByteArray();
            outputWidth = writableBitmap.PixelWidth;
            outputHeight = writableBitmap.PixelWidth;
        }
    }
}
