using System;
using System.Collections.Generic;
using System.IO;
using MSDataFileReader;

namespace AScore_DLL.Managers.DatasetManagers
{
    public class MzxmlManager : SpectraManager
    {

        private string datasetName;
        private clsMzXMLFileAccessor mzAccessor;

        private MzxmlManager(string mzxmlPath)
        {
            try
            {
                datasetName = Path.GetFileNameWithoutExtension(mzxmlPath);
                datasetName = datasetName.Substring(0, datasetName.Length - 4);
                mzAccessor = new clsMzXMLFileAccessor();
                mzAccessor.OpenFile(mzxmlPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException("The specificied directory for " +
                    "the mzxml could not be found!");
            }
            catch (FileNotFoundException)
            {
                string filename = Path.GetFileName(mzxmlPath);
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

        public string GetDtaFileName(int scanNumber, int scanCount, int chargeState)
        {
            throw new NotImplementedException();

        }

        public ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int chargeState)
        {
            ExperimentalSpectra expSpec = null;
            clsSpectrumInfo specInfo = null;
            if (mzAccessor.GetSpectrumByScanNumber(scanNumber, ref specInfo))
            {
                double precursorMass = 0.0;
                int precursorChargeState = 0;
                List<ExperimentalSpectraEntry> entries =
                    new List<ExperimentalSpectraEntry>();


                precursorMass = specInfo.ParentIonMZ;
                precursorChargeState = chargeState;

                double[] mzlist = specInfo.MZList;
                float[] intlist = specInfo.IntensityList;



                for (int i = 0; i < mzlist.Length; i++)
                {
                    double val1 = mzlist[i];
                    double val2 = intlist[i];

                    entries.Add(new ExperimentalSpectraEntry(val1, val2));
                }

                expSpec = new ExperimentalSpectra(scanNumber, chargeState,
                    precursorMass, precursorChargeState, entries);


            }
            return expSpec;
            
           
        }




    }
}
