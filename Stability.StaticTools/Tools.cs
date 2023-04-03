using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitkImage = itk.simple.Image;
using itk.simple;

namespace Stability.StaticTools
{
    public class Tools
    {
        public static int RoundUpToOdd(double f)
        {
            return (int)(Math.Ceiling(f) / 2) * 2 + 1;
        }
        static void write_image(SitkImage image)
        {
            SimpleITK.WriteImage(image, @"C:\Users\fpenaloza\Downloads\compared.nii.gz");
        }
        static void write_image(SitkImage image, string file_name)
        {
            SimpleITK.WriteImage(image, file_name);
        }
        static SitkImage copy_base_information(SitkImage bare_image, SitkImage base_image)
        {
            bare_image.CopyInformation(base_image);
            return SimpleITK.Cast(bare_image, base_image.GetPixelID());
        }
    }
}
