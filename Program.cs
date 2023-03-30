using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using itk.simple;
using Numpy.Models;
using Numpy;
using SitkImage = itk.simple.Image;
using PixelId = itk.simple.PixelIDValueEnum;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Reflection;
using StablePointFinderClass;

namespace FindingStablePointsCSharp
{
    internal class Program
    {
        public static int RoundUpToOdd(double f)
        {
            return (int)(Math.Ceiling(f) / 2) * 2 + 1;
        }
        static SitkImage copy_base_information(SitkImage bare_image, SitkImage base_image)
        {
            bare_image.CopyInformation(base_image);
            return SimpleITK.Cast(bare_image, base_image.GetPixelID());
        }
        static SitkImage return_image_from_array(NDarray np_array)
        {
            np_array = np_array.astype(np.float32);
            int len = np_array.shape[0] * np_array.shape[1] * np_array.shape[2];
            SitkImage outImage = new SitkImage((uint)np_array.shape[2], (uint)np_array.shape[1], (uint)np_array.shape[0], PixelId.sitkFloat32);
            IntPtr outImageBuffer = outImage.GetBufferAsFloat();
            Marshal.Copy(np_array.GetData<float>(), 0, outImageBuffer, len);
            return outImage;
        }
        static NDarray return_array_from_image(SitkImage image)
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
        static void write_image(SitkImage image)
        {
            SimpleITK.WriteImage(image, @"C:\Users\markb\Modular_Projects\FindingStablePoints\compared.nii.gz");
        }
        static void Main(string[] args)
        {
            string dose_file = @"C:\Users\markb\Modular_Projects\FindingStablePoints\dose.nii.gz";
            StablePointFinder finder = new StablePointFinder(dose_file, 0.85, new List<float> { 5, 5, 5 });
            finder.execute();
            //bounding_boxes = np.asarray([truth_stats.GetBoundingBox(_) for _ in truth_stats.GetLabels()])
        }
    }
}