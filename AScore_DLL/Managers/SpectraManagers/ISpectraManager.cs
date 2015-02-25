namespace AScore_DLL.Managers.SpectraManagers
{
    public interface ISpectraManager
    {
        string DatasetName { get; }
        bool Initialized { get; }
        void Abort();
        void OpenFile(string filePath);
        ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int chargeState);
        string GetFilePath(string datasetFilePath, string datasetName); // Explicitly implement interface, and call a static member function by the same name.
    }
}
