using Stability.FinderClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stability.ConsoleAppNet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string dose_file = @"C:\Users\markb\Modular_Projects\FindingStablePoints\dose.nii.gz";
            StablePointFinder finder = new StablePointFinder(dose_file: dose_file);
            finder.execute();
        }
    }
}
