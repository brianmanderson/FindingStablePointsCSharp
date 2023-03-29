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
        static NDarray return_array_from_image(SitkImage image)
        {
            VectorDouble voxel_size = image.GetSpacing();
            image = SimpleITK.Cast(image, PixelId.sitkFloat32);

            // calculate the number of pixels
            VectorUInt32 size = image.GetSize();
            int len = 1;
            for (int dim = 0; dim < image.GetDimension(); dim++)
            {
                len *= (int)size[dim];
            }
            NDarray dose_np = new NDarray(image.GetBufferAsFloat(), len, np.float32);
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
            var max_dose = np.max(dose_np);
            ConnectedComponentImageFilter Connected_Component_Filter = new ConnectedComponentImageFilter();
            ConnectedThresholdImageFilter Connected_Threshold = new ConnectedThresholdImageFilter();
            Connected_Threshold.SetLower(1);
            Connected_Threshold.SetUpper(2);
            LabelShapeStatisticsImageFilter truth_stats = new LabelShapeStatisticsImageFilter();
            /*
            Next, identify each independent segmentation in both
            */
            var masked_np = (dose_np >= limit * max_dose).astype(np.int32);
            var base_mask = itk.GetImageFromArray(masked_np);
            var connected_image_handle = Connected_Component_Filter.Execute(base_mask);

            var RelabelComponentFilter = new itk.RelabelComponentImageFilter();
            var connected_image = RelabelComponentFilter.Execute(connected_image_handle);
            truth_stats.Execute(connected_image);
            var bounding_boxes = np.asarray([truth_stats.GetBoundingBox(_) for _ in truth_stats.GetLabels()]) ;
            for (int index = 0; index < bounding_boxes.shape[0]; index++)
            {
                var bounding_box = bounding_boxes[index];
                var c_start = bounding_box[0];
                var r_start = bounding_box[1];
                var z_start = bounding_box[2];
                var c_stop = c_start + bounding_box[3];
                var r_stop = r_start + bounding_box[4];
                var z_stop = z_start + bounding_box[5];
                var dose_cube = dose_np[z_start: z_stop, r_start: r_stop, c_start: c_stop];
                var gradient_np = np.abs(np.gradient(dose_cube, 2));
                var super_imposed_gradient_np = np.sum(gradient_np, axis = 0);
                // We want to convolve across three axis to make sure we are not near a gradient edge
                for (int i = 0; i < camera_dimensions.Length; i++)
                {
                    var k = camera_dimensions[i];
                    var voxels_needed = round_up_to_odd(k / voxel_size[i]);
                    super_imposed_gradient_np = scipy.ndimage.convolve1d(super_imposed_gradient_np,
                                                                         np.array([1 / voxels_needed
                                                                                   for _ in range(voxels_needed)]), axis = i);
        }
        var min_gradient = np.min(super_imposed_gradient_np);
        var min_location = np.where(super_imposed_gradient_np <= min_gradient * 1.1);
        var min_z = (int)z_start + min_location[0][0];
        var min_row = (int)r_start + min_location[1][0];
        var min_col = (int)c_start + min_location[2][0];
        var physical_location = dose_handle.TransformContinuousIndexToPhysicalPoint(new[] { min_col, min_row, min_z });
        Console.WriteLine($"Identified position for one site: {physical_location}");
}

}
    }
}
