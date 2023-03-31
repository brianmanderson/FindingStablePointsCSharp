using Stability.StablePointFinderClass;

string dose_file = @"C:\Users\markb\Modular_Projects\FindingStablePoints\dose.nii.gz";
StablePointFinder finder = new StablePointFinder(dose_file);
finder.execute();