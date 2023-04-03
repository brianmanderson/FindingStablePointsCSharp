using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using itk.simple;
using Numpy.Models;
using Numpy;
using SitkImage = itk.simple.Image;
using PixelId = itk.simple.PixelIDValueEnum;
using System.Threading.Tasks;
using Stability.StaticTools;
using Stability.FinderClass.Services;

namespace Stability.FinderClass
{
    public class StablePointFinder
    {
        ImageFileReader reader = new ImageFileReader();
        List<float> camera_dimensions = new List<float> { 5, 5, 5 };
        SitkImage dose_handle;
        string dose_file;
        double dose_limit = 0.85;
        public StablePointFinder(SitkImage dose_handle)
        {
            this.dose_handle = dose_handle;
        }
        public StablePointFinder(string dose_file)
        {
            this.dose_file = dose_file;
            load_image();
        }
        public StablePointFinder(string dose_file, double dose_limit)
        {
            this.dose_file = dose_file;
            this.dose_limit = dose_limit;
            load_image();
        }
        public StablePointFinder(string dose_file, List<float> camera_dimensions)
        {
            this.dose_file = dose_file;
            this.camera_dimensions = camera_dimensions;
            load_image();
        }
        public StablePointFinder(string dose_file, double dose_limit, List<float> camera_dimensions)
        {
            this.dose_file = dose_file;
            this.camera_dimensions = camera_dimensions;
            this.dose_limit = dose_limit;
            load_image();
        }
        public void load_image()
        {
            reader.SetFileName(dose_file);
            reader.ReadImageInformation();
            dose_handle = reader.Execute();
        }
        public void execute()
        {

            VectorDouble voxel_size = dose_handle.GetSpacing();
            List<int> voxels_needed = new List<int> { Tools.RoundUpToOdd(camera_dimensions[0] / voxel_size[0]) , 
                Tools.RoundUpToOdd(camera_dimensions[1] / voxel_size[1]), Tools.RoundUpToOdd(camera_dimensions[2] / voxel_size[2])};
            NDarray dose_np = Services.StaticTools.return_array_from_image(dose_handle);
            ConnectedComponentImageFilter Connected_Component_Filter = new ConnectedComponentImageFilter();
            ConnectedThresholdImageFilter Connected_Threshold = new ConnectedThresholdImageFilter();
            Connected_Threshold.SetLower(1);
            Connected_Threshold.SetUpper(2);
            LabelShapeStatisticsImageFilter truth_stats = new LabelShapeStatisticsImageFilter();
            /*
            Next, identify each independent segmentation in both
            */
            NDarray masked_np = (dose_np >= dose_limit * np.max(dose_np));
            SitkImage base_mask = Services.StaticTools.return_image_from_array(masked_np);
            base_mask = SimpleITK.Cast(base_mask, PixelId.sitkInt32);

            SitkImage connected_image_handle = Connected_Component_Filter.Execute(base_mask);

            RelabelComponentImageFilter RelabelComponentFilter = new RelabelComponentImageFilter();
            SitkImage connected_image = RelabelComponentFilter.Execute(connected_image_handle);
            truth_stats.Execute(connected_image);
            List<VectorUInt32> bounding_boxes = new List<VectorUInt32>();
            foreach (long label in truth_stats.GetLabels())
            {
                bounding_boxes.Add(truth_stats.GetBoundingBox(label));
            }
            foreach (VectorUInt32 bounding_box in bounding_boxes)
            {
                uint c_start = bounding_box[0];
                uint r_start = bounding_box[1];
                uint z_start = bounding_box[2];
                uint c_stop = c_start + bounding_box[3];
                uint r_stop = r_start + bounding_box[4];
                uint z_stop = z_start + bounding_box[5];
                NDarray dose_cube = dose_np[$"{z_start}:{z_stop},{r_start}:{r_stop},{c_start}:{c_stop}"];
                NDarray gradient_np = np.abs(np.gradient(dose_cube, 2));
                gradient_np = np.sum(gradient_np, 0);
                SitkImage gradient_handle = Services.StaticTools.return_image_from_array(gradient_np);
                NDarray kernel = np.ones(voxels_needed.ToArray());
                kernel /= np.sum(kernel);
                SitkImage kernel_handle = Services.StaticTools.return_image_from_array(kernel);
                SitkImage convolved_handle = SimpleITK.Convolution(gradient_handle, kernel_handle);

                gradient_np = Services.StaticTools.return_array_from_image(convolved_handle);
                NDarray min_gradient = np.min(gradient_np);
                NDarray[] min_location = np.where(gradient_np <= min_gradient * 1.1);
                VectorDouble continous_index = new VectorDouble();
                NDarray z_location = min_location[0];
                NDarray r_location = min_location[1];
                NDarray c_location = min_location[2];
                uint min_z = z_start + (uint)z_location[z_location.size / 2];
                uint min_row = r_start + (uint)r_location[z_location.size / 2];
                uint min_col = c_start + (uint)c_location[z_location.size / 2];
                continous_index.Add(min_col);
                continous_index.Add(min_row);
                continous_index.Add(min_z);
                VectorDouble physical_location = dose_handle.TransformContinuousIndexToPhysicalPoint(continous_index);
                Console.WriteLine($"Best location was found to be at {physical_location[0]}, {physical_location[1]}, {physical_location[2]}");
            }
        }
    }
}
