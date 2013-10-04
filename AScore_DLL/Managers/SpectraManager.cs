namespace AScore_DLL.Managers
{
    interface SpectraManager
    {
        void Abort();
        string GetDtaFileName(int scanNumber, int scanCount, int chargeState);
        ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int chargeState);
    }
}
