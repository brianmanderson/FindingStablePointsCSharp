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

namespace Stability.FinderClass
{
    public class StablePointFinder
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
        static void write_image(SitkImage image, string file_path)
        {
            SimpleITK.WriteImage(image, file_path);
        }
        static void write_image(SitkImage image)
        {
            SimpleITK.WriteImage(image, @"C:\Users\markb\Modular_Projects\FindingStablePoints\compared.nii.gz");
        }
        ImageFileReader reader = new ImageFileReader();
        List<float> camera_dimensions = new List<float> { 5, 5, 5 };
        string dose_file;
        double dose_limit = 0.85;
        public StablePointFinder(string dose_file)
        {
            this.dose_file = dose_file;
        }
        public StablePointFinder(string dose_file, double dose_limit)
        {
            this.dose_file = dose_file;
            this.dose_limit = dose_limit;
        }
        public StablePointFinder(string dose_file, List<float> camera_dimensions)
        {
            this.dose_file = dose_file;
            this.camera_dimensions = camera_dimensions;
        }
        public StablePointFinder(string dose_file, double dose_limit, List<float> camera_dimensions)
        {
            this.dose_file = dose_file;
            this.camera_dimensions = camera_dimensions;
            this.dose_limit = dose_limit;
        }
        public void execute()
        {
            reader.SetFileName(dose_file);
            reader.ReadImageInformation();
            SitkImage dose_handle = reader.Execute();
            VectorDouble voxel_size = dose_handle.GetSpacing();

            List<int> voxels_needed = new List<int> { RoundUpToOdd(camera_dimensions[0] / voxel_size[0]) ,
            RoundUpToOdd(camera_dimensions[1] / voxel_size[1]), RoundUpToOdd(camera_dimensions[2] / voxel_size[2])};

            NDarray dose_np = return_array_from_image(dose_handle);
            ConnectedComponentImageFilter Connected_Component_Filter = new ConnectedComponentImageFilter();
            ConnectedThresholdImageFilter Connected_Threshold = new ConnectedThresholdImageFilter();
            Connected_Threshold.SetLower(1);
            Connected_Threshold.SetUpper(2);
            LabelShapeStatisticsImageFilter truth_stats = new LabelShapeStatisticsImageFilter();
            /*
            Next, identify each independent segmentation in both
            */
            NDarray masked_np = (dose_np >= dose_limit * np.max(dose_np));
            SitkImage base_mask = return_image_from_array(masked_np);
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
                SitkImage gradient_handle = return_image_from_array(gradient_np);
                NDarray kernel = np.ones(voxels_needed.ToArray());
                kernel /= np.sum(kernel);
                SitkImage kernel_handle = return_image_from_array(kernel);
                SitkImage convolved_handle = SimpleITK.Convolution(gradient_handle, kernel_handle);

                gradient_np = return_array_from_image(convolved_handle);
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
