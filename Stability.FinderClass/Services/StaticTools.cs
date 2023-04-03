using Numpy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SitkImage = itk.simple.Image;
using PixelId = itk.simple.PixelIDValueEnum;
using itk.simple;
using Numpy.Models;

namespace Stability.FinderClass.Services
{
    internal class StaticTools
    {
        public static SitkImage return_image_from_array(NDarray np_array)
        {
            np_array = np_array.astype(np.float32);
            int len = np_array.shape[0] * np_array.shape[1] * np_array.shape[2];
            SitkImage outImage = new SitkImage((uint)np_array.shape[2], (uint)np_array.shape[1], (uint)np_array.shape[0], PixelId.sitkFloat32);
            IntPtr outImageBuffer = outImage.GetBufferAsFloat();
            Marshal.Copy(np_array.GetData<float>(), 0, outImageBuffer, len);
            return outImage;
        }

        public static NDarray return_array_from_image(SitkImage image)
        {
            image = SimpleITK.Cast(image, PixelId.sitkFloat32);

            // calculate the number of pixels
            VectorUInt32 size = image.GetSize();
            Shape output_shape = new Shape((int)size[2], (int)size[1], (int)size[0]);
            int len = 1;
            for (int dim = 0; dim < image.GetDimension(); dim++)
            {
                len *= (int)size[dim];
            }
            float[] bufferAsArray = new float[len]; // Allocates new memory the size of input
            IntPtr buffer = image.GetBufferAsFloat();
            Marshal.Copy(buffer, bufferAsArray, 0, len);
            NDarray dose_np = np.array(bufferAsArray);
            dose_np = np.reshape(dose_np, output_shape);
            return dose_np;
        }
    }
}
