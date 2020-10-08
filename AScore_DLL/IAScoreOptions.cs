using System.IO;

namespace AScore_DLL
{
    public enum SearchMode
    {
        /// <summary>
        /// SEQUEST
        /// </summary>
        Sequest,
        /// <summary>
        /// X!Tandem
        /// </summary>
        XTandem,
        /// <summary>
        /// Inspect
        /// </summary>
        Inspect,
        /// <summary>
        /// Old name for MS-GF+
        /// </summary>
        Msgfdb,
        /// <summary>
        /// MS-GF+
        /// </summary>
        Msgfplus
    }

    public enum DbSearchResultsType
    {
        /// <summary>
        /// PHRP First Hits file or Synopsis file
        /// </summary>
        Fht,
        /// <summary>
        /// .mzid file
        /// </summary>
        Mzid,
    }

    public interface IAScoreOptions
    {
        SearchMode SearchType { get; }

        string DbSearchResultsFile { get; }

        string MassSpecFile { get; }

        string JobToDatasetMapFile { get; }

        string AScoreParamFile { get; }

        string OutputDirectoryPath { get; }

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
