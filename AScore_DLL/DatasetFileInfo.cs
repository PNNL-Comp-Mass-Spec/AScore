
namespace AScore_DLL
{
    public class DatasetFileInfo
    {
        /// <summary>
        /// Spectrum file path
        /// </summary>
        /// <remarks>
        /// Spectrum file path if processing a single file
        /// Dataset name if read from a job to dataset map file (defined with -JM)
        /// </remarks>
        public string SpectrumFilePath { get; }

        public string ModSummaryFilePath { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spectrumFilePath"></param>
        public DatasetFileInfo(string spectrumFilePath) : this(spectrumFilePath, "")
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="spectrumFilePath">
        /// Spectrum file path if processing a single file
        /// Dataset name if read from a job to dataset map file (defined with -JM)
        /// </param>
        /// <param name="modSummaryFilePath"></param>
        public DatasetFileInfo(string spectrumFilePath, string modSummaryFilePath)
        {
            SpectrumFilePath = spectrumFilePath;
            ModSummaryFilePath = modSummaryFilePath;
        }
    }
}
