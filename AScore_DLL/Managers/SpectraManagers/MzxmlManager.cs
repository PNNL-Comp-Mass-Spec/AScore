using System;
using System.Collections.Generic;
using System.IO;
using MSDataFileReader;

namespace AScore_DLL.Managers.SpectraManagers
{
    [Obsolete("Unused")]
    public class MzxmlManager : ISpectraManager
    {
        private readonly clsMzXMLFileAccessor mzAccessor;

        private readonly PHRPReader.clsPeptideMassCalculator mPeptideMassCalculator;

        public bool Initialized { get; } = false;

        public string DatasetName { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mzxmlPath"></param>
        /// <param name="peptideMassCalculator"></param>
        private MzxmlManager(string mzxmlPath, PHRPReader.clsPeptideMassCalculator peptideMassCalculator)
        {
            mPeptideMassCalculator = peptideMassCalculator;

            try
            {
                var datasetName  = Path.GetFileNameWithoutExtension(mzxmlPath);

                // Note: prior to October 2017 the dataset name was determined by removing the last 4 characters from datasetName
                // That logic seemed flawed and has thus been removed
                DatasetName = datasetName;

                mzAccessor = new clsMzXMLFileAccessor();
                mzAccessor.OpenFile(mzxmlPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException("The specified directory for " +
                    "the mzxml could not be found!");
            }
            catch (FileNotFoundException)
            {
                var filename = Path.GetFileName(mzxmlPath);
                throw new FileNotFoundException("The specified mzxml file \"" +
                    filename + "\" could not be found!");
            }
        }

        ~MzxmlManager()
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

            var mzlist = specInfo.MZList;
            var intlist = specInfo.IntensityList;

            for (var i = 0; i < mzlist.Length; i++)
            {
                var val1 = mzlist[i];
                double val2 = intlist[i];

                entries.Add(new ExperimentalSpectraEntry(val1, val2));
            }

            var expSpec = new ExperimentalSpectra(scanNumber, chargeState,
                                                                  precursorMass, precursorChargeState, entries, mPeptideMassCalculator);
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
