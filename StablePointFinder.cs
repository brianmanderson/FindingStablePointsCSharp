using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using SitkImage = itk.simple.Image;
using itk.simple;
using Stability.StablePointFinderClass;
using System.Data;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.9")]
[assembly: AssemblyFileVersion("1.0.0.9")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace Stability.EsapiApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static SitkImage DoseToImage(Dose dose)
        {
            int W = dose.XSize;
            int H = dose.YSize;
            int D = dose.ZSize;
            double sx = dose.XRes;
            double sy = dose.YRes;
            double sz = dose.ZRes;
            VVector origin = dose.Origin;
            VVector rowDirection = dose.XDirection;
            VVector colDirection = dose.YDirection;
            double xsign = rowDirection.x > 0 ? 1.0 : -1.0;
            double ysign = colDirection.y > 0 ? 1.0 : -1.0;
            double zsign = GetZDirection(rowDirection, colDirection).z > 0 ? 1.0 : -1.0;

            SitkImage dose_handle = new SitkImage((uint)W, (uint)H, (uint)D, PixelIDValueEnum.sitkFloat32);
            VectorDouble spacing = new VectorDouble() { sx * xsign, sy * ysign, sz * zsign };
            VectorDouble sitk_origin = new VectorDouble() { origin.x, origin.y, origin.z };
            dose_handle.SetSpacing(spacing);
            dose_handle.SetOrigin(sitk_origin);
            int[,] buffer = new int[W, H];
            for (int z = 0; z < D; z++)
            {
                dose.GetVoxels(z, buffer);
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        float value = (float)buffer[x, y];
                        dose_handle.SetPixelAsFloat(new VectorUInt32() { (uint)x, (uint)y, (uint)z }, value);
                    }
                }
            }
            return dose_handle;
        }
        static void Execute(Application app)
    {
            // TODO: Add your code here.
            Patient p = app.OpenPatientById("");
            foreach (Course c in p.Courses)
            {
                var plans = c.ExternalPlanSetups.Where(x => x.IsDoseValid);
                foreach (ExternalPlanSetup plan in plans)
                {
                    Dose dose = plan.Dose;
                    //int maxValueForScaling = FindMaxValue(dose);
                    SitkImage dose_handle = DoseToImage(dose);
                    StablePointFinder finder = new StablePointFinder(dose_handle);
                    finder.execute();
                }
            }

    }
  }
}
