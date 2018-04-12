using System.IO;

namespace AScore_DLL
{
    public enum SearchMode
    {
        Sequest,
        XTandem,
        Inspect,
        Msgfdb,
        Msgfplus
    }

    public enum DbSearchResultsType
    {
        Fht,
        Mzid,
    }

    public interface IAScoreOptions
    {
        SearchMode SearchType { get; }

        string DbSearchResultsFile { get; }

        string MassSpecFile { get; }

        string JobToDatasetMapFile { get; }

        string AScoreParamFile { get; }

        string OutputFolderPath { get; }

        bool FilterOnMSGFScore { get; }

        string UpdatedDbSearchResultsFileName { get; }

        bool CreateUpdatedDbSearchResultsFile { get; }

        bool SkipExistingResults { get; }

        string FastaFilePath { get; }

        bool OutputProteinDescriptions { get; }

        bool MultiJobMode { get; }

        DbSearchResultsType SearchResultsType { get; }

        string AScoreResultsFilePath { get; }

        DirectoryInfo OutputDirectoryInfo { get; }
    }
}
