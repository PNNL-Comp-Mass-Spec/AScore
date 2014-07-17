The Ascore program can process first-hits files from MSGF+, Sequest, or X!Tandem
to compute confidence scores for the position of phosphorylated residues.  Use the
following steps to manually analyze results from a set of DMS analysis jobs.

Use Mage File Processor to retrieve the required files
	- Search datasets by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
		- Find *_dta.zip
	- Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
		- Find *_ModSummary.txt  (just need one of them)
	- Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
		- Find *_fht.txt
		- Create a single, combined file using "Process files to local folder"
			- Use the "Add Job Column" mapping to assure that the Job number is the first column 
			- Name the file Leishmania_TMT_NiNTA_msgfdb_fht.txt

Use Mage Extractor to filter the MSGF+ results with MSGF_SpecProb < 1E-10
	- Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
		- Select the MSGFPlus jobs
		- Set the MSGF Cutoff to "1E-10" and Result Type to Extract to "MSGF+ Synopsis First Protein"
		- Define the output file: Leishmania_TMT_NiNTA_filtered_results.txt
		- Click "Extract results from Selected Jobs"

Unzip the _DTA.zip files

Copy the AScore parameter file from \\gigasax\dms_parameter_Files\AScore to local computer:
	DynPhos_stat_6plex_iodo_hcd.xml

Create the JobToDatasetMap.txt file with columns Job and Dataset

Run AScore, using the -JM switch

pushd \\floyd\software\AScore
AScore_Console.exe -T:msgfplus -F:"F:\Temp\AScore\Leishmania_TMT_NiNTA_msgfdb_fht.txt" -JM:"F:\Temp\AScore\JobToDatasetMap.txt" -O:"F:\Temp\AScore\" -P:F:\Temp\AScore\DynPhos_stat_6plex_iodo_hcd.xml -L:LogFile.txt -FM:true
AScore_Console.exe -T:msgfplus -F:"F:\Temp\AScore\Leishmania_TMT_NiNTA_filtered_results.txt" -JM:"F:\Temp\AScore\JobToDatasetMap.txt" -O:"F:\Temp\AScore\" -P:F:\Temp\AScore\DynPhos_stat_6plex_iodo_hcd.xml -L:LogFile.txt -FM:true
popd 

Results files are Leishmania_TMT_NiNTA_msgfdb_fht_ascore.txt and
                  Leishmania_TMT_NiNTA_filtered_results_ascore.txt
