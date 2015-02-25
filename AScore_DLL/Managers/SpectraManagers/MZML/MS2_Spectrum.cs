using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AScore_DLL.Managers.SpectraManagers.MZML
{
    public class MS2_Spectrum
    {
        public IsolationWindow IsolationWindow { get; set; }

        public void SetMsLevel(int msLevel)
        {
            MsLevel = msLevel;
        }

        public MS2_Spectrum(IList<double> mzArr, IList<double> intensityArr, int scanNum)
        {
            Peaks = new Peak[mzArr.Count];
            for(var i=0; i<mzArr.Count; i++) Peaks[i] = new Peak(mzArr[i], intensityArr[i]);
            ScanNum = scanNum;
        }

        public MS2_Spectrum(ICollection<Peak> peaks, int scanNum)
        {
            Peaks = new Peak[peaks.Count];
            peaks.CopyTo(Peaks, 0);
            ScanNum = scanNum;
        }

        public int ScanNum { get; private set; }

        public string NativeId { get; set; }

        public int MsLevel
        {
            get { return _msLevel; }
            set { _msLevel = value; }
        }
        protected int _msLevel = 1;

        public double ElutionTime { get; set; }

        // Peaks are assumed to be sorted according to m/z
        public Peak[] Peaks { get; private set; }

        public void Display()
        {
            var sb = new StringBuilder();
            //sb.Append("--------- Spectrum -----------------\n");
            foreach (var peak in Peaks)
            {
                sb.Append(peak.Mz);
                sb.Append("\t");
                sb.Append(peak.Intensity);
                sb.Append("\n");
            }
            //sb.Append("--------------------------- end ---------------------------------------\n");

            Console.Write(sb.ToString());
        }

        public void FilterNoise(double signalToNoiseRatio = 1.4826)
        {
            if (Peaks.Length < 2) return;
            Array.Sort(Peaks, new IntensityComparer());
            var medianIntPeak = Peaks[Peaks.Length / 2];
            var noiseLevel = medianIntPeak.Intensity;

            var filteredPeaks = Peaks.TakeWhile(peak => !(peak.Intensity < noiseLevel * signalToNoiseRatio)).ToList();

            filteredPeaks.Sort();
            Peaks = filteredPeaks.ToArray();
        }

        public MS2_Spectrum GetFilteredSpectrumBySignalToNoiseRatio(double signalToNoiseRatio = 1.4826)
        {
            var filteredSpec = (MS2_Spectrum)MemberwiseClone();
            filteredSpec.FilterNoise(signalToNoiseRatio);
            return filteredSpec;
        }

    }
}
