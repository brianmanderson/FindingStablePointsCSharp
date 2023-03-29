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

namespace FindingStablePointsCSharp
{
    internal class Program
    {
        static SitkImage copy_base_information(SitkImage bare_image, SitkImage base_image)
        {
            bare_image.CopyInformation(base_image);
            return SimpleITK.Cast(bare_image, base_image.GetPixelID());
        }
        static Image return_image_from_array(NDarray np_array)
        {
            int len = np_array.shape[0] * np_array.shape[1] * np_array.shape[2];
            SitkImage outImage = new SitkImage((uint)np_array.shape[2], (uint)np_array.shape[1], (uint)np_array.shape[0],PixelId.sitkFloat32);
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
        static void Main(string[] args)
        {
            string dose_file = @"C:\Users\markb\Modular_Projects\FindingStablePoints\dose.nii.gz";
            var reader = new ImageFileReader();
            double limit = 0.9;
            reader.SetFileName(dose_file);
            reader.ReadImageInformation();
            SitkImage dose_handle = reader.Execute();
            NDarray dose_np = return_array_from_image(dose_handle);
            SitkImage new_dose_handle = return_image_from_array(dose_np);
            new_dose_handle = copy_base_information(new_dose_handle, dose_handle);
            SimpleITK.WriteImage(new_dose_handle, @"C:\Users\markb\Modular_Projects\FindingStablePoints\dose_compared.nii.gz");
            NDarray max_dose = np.max(dose_np);
            ConnectedComponentImageFilter Connected_Component_Filter = new ConnectedComponentImageFilter();
            ConnectedThresholdImageFilter Connected_Threshold = new ConnectedThresholdImageFilter();
            Connected_Threshold.SetLower(1);
            Connected_Threshold.SetUpper(2);
            LabelShapeStatisticsImageFilter truth_stats = new LabelShapeStatisticsImageFilter();
            /*
            Next, identify each independent segmentation in both
            */
            NDarray masked_np = (dose_np >= limit * max_dose).astype(np.int32);
            //var connected_image_handle = Connected_Component_Filter.Execute(base_mask);

            var RelabelComponentFilter = new RelabelComponentImageFilter();
            //var connected_image = RelabelComponentFilter.Execute(connected_image_handle);
            //truth_stats.Execute(connected_image);
        }
    }
}