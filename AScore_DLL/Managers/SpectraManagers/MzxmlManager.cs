using System;
using System.Collections.Generic;
using System.IO;
using MSDataFileReader;

namespace AScore_DLL.Managers.SpectraManagers
{
    [Obsolete("Unused")]
    public class MzXmlManager : ISpectraManager
    {
        private readonly clsMzXMLFileAccessor mzAccessor;

        private readonly PHRPReader.PeptideMassCalculator mPeptideMassCalculator;

        public bool Initialized { get; } = false;

        public string DatasetName { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mzXmlPath"></param>
        /// <param name="peptideMassCalculator"></param>
        private MzXmlManager(string mzXmlPath, PHRPReader.PeptideMassCalculator peptideMassCalculator)
        {
            mPeptideMassCalculator = peptideMassCalculator;

            try
            {
                // Note: prior to October 2017 the dataset name was determined by removing the last 4 characters from datasetName
                // That logic seemed flawed and has thus been removed
                if (mzXmlPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    DatasetName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(mzXmlPath));
                else
                    DatasetName = Path.GetFileNameWithoutExtension(mzXmlPath);

                mzAccessor = new clsMzXMLFileAccessor();
                mzAccessor.OpenFile(mzXmlPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException("The specified directory for " +
                    "the mzXml could not be found!");
            }
            catch (FileNotFoundException)
            {
                var filename = Path.GetFileName(mzXmlPath);
                throw new FileNotFoundException("The specified mzXml file \"" +
                    filename + "\" could not be found!");
            }
        }

        ~MzXmlManager()
        {
            mzAccessor.CloseFile();
        }

        public void Abort()
        {
            mzAccessor.CloseFile();
        }

        public ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int chargeState)
        {
            if (!mzAccessor.GetSpectrumByScanNumber(scanNumber, out var specInfo))
                return null;

            var entries = new List<ExperimentalSpectraEntry>();

            var precursorMass = specInfo.ParentIonMZ;
            var precursorChargeState = chargeState;

            var mzList = specInfo.MZList;
            var intensityList = specInfo.IntensityList;

            for (var i = 0; i < mzList.Length; i++)
            {
                var mz = mzList[i];
                double intensity = intensityList[i];

                entries.Add(new ExperimentalSpectraEntry(mz, intensity));
            }

            var expSpec = new ExperimentalSpectra(scanNumber, chargeState, precursorMass, precursorChargeState, entries, mPeptideMassCalculator);
            return expSpec;
        }

        public void OpenFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public string GetFilePath(string datasetFilePath, string datasetName)
        {
            throw new NotImplementedException();
        }
    }
}
