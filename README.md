# AScore Console

The AScore program can process first-hits or synopsis files from MSGF+, SEQUEST, or X!Tandem
to compute confidence scores for the position of phosphorylated residues.

# Usage

Use the following steps to analyze results from a set of DMS analysis jobs.
When retrieving the PHRP data files, you can either run AScore on all of the identifications in the first-hits or synopsis file,
or you can filter the data using an MSGF cutoff (which will result in a faster AScore runtime due to fewer peptides to process)

## Retrieve Files using Mage File Processor

Use [Mage File Processor](https://github.com/PNNL-Comp-Mass-Spec/Mage) to retrieve the required files
* Search datasets by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
  * Find *_dta.zip
* Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
  * Find *_ModSummary.txt  (just need one of them)

## Retrieve PHRP data files (no filter)

* Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
  * Find *_fht.txt
Create a single, combined file using "Process files to local folder"
  * Use the "Add Job Column" mapping to assure that the Job number is the first column 
  * Name the file Leishmania_TMT_NiNTA_msgfdb_fht.txt

## Retrieve PHRP data files (filter on MSGF SpecProb)

Use [Mage Extractor](https://github.com/PNNL-Comp-Mass-Spec/Mage) to filter the MSGF+ results with MSGF_SpecProb < 1E-10
* Search jobs by dataset ID: 340332, 340360, 340380, 340369, 340366, 340356, 340336, 340354, 340359, 340371, 340372, 340363
* Select the MSGFPlus jobs
* Set the MSGF Cutoff to "1E-10" and Result Type to Extract to "MSGF+ Synopsis First Protein"
* Define the output file: Leishmania_TMT_NiNTA_filtered_results.txt
* Click "Extract results from Selected Jobs"

Alternatively, manually create the PHRP data files using 
[Peptide Hit Results Processor (PHRP)](https://github.com/PNNL-Comp-Mass-Spec/PHRP)

## Prepare Files for AScore

Unzip the _DTA.zip files

Copy the AScore parameter file from `\\gigasax\DMS_Parameter_FilesAScore` to your local computer.
Use either of these files:
* AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml
* AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da_MSGF1E-12.xml

## Create the Job to Dataset Map file

This file is only required if the input file has results from multiple jobs.
It is a tab-delimited file with two columns, Job and Dataset

## Run AScore

Run AScore Console, using the switches described below.

Results files will be the input file, but with `_AScore.txt` in the filename, for example:
* CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_msgfplus_fht_AScore.txt

## Console Switches

AScore_Console.exe is a console application, and must be run from the Windows command prompt.

```
 -T:search_engine
 -F:fht_file_path
 -D:spectra_file_path
 -JM:job_to_dataset_mapfile_path
 -P:parameter_file_path
 -O:output_folder_path
 -L:log_file_path
 -noFM
 -U:updated_fht_file_name
 -Skip
 -Fasta:Fasta_file_path
 -PD
```

Use -T to specify the search engine type, for example -T:msgfplus\
Allowed values for search_engine are: sequest, xtandem, inspect, msgfdb, or msgfplus

Use -F to specify the input file (in first hits file / synopsis file format).
* See [Peptide Hit Results Processor (PHRP)](https://github.com/PNNL-Comp-Mass-Spec/PHRP)
* Example synopsis file: [QC_Shew_13_05b_HCD_500ng_24Mar14_Tiger_14-03-04_msgfplus_syn.txt](https://raw.githubusercontent.com/PNNL-Comp-Mass-Spec/PHRP/master/Data/MSGFPlus_Example/QC_Shew_13_05b_HCD_500ng_24Mar14_Tiger_14-03-04_msgfplus_syn.txt)

Use -D to specify the file with spectra data.  This can be a concatenated DTA file (_dta.txt) or a .mzML file.

If the first hits files specified by -F includes job numbers in the first column, use -JM to specify a job to dataset map file.
When using -JM, do not use -D.  Columns in the job to dataset map file are Job and Dataset (tab-separated).

Use -P for the AScore parameter file

Optionally use -O to specify the output folder

Optionally use -L to create a log file

Use -noFM to disable filtering on data in column MSGF_SpecProb. By default, data is filtered
using the MSGFPreFilter score specified in the AScore parameter file.  For example, to filter on MSGF SpecProb 1E-12 use:
* `<MSGFPreFilter>1E-12</MSGFPreFilter>`

Use -U to create an updated version of the input file, but with AScore columns appended to each row

Use -Skip to not re-run AScore if an existing results file already exists

Optionally use -Fasta to add Protein Data from Fasta_file to the output

When using -fasta, use -PD to include Protein Descriptions in the output

## Example command line #1
```
AScore_Console.exe -T:sequest
 -F:"C:\Temp\DatasetName_fht.txt"
 -D:"C:\Temp\DatasetName_dta.txt"
 -O:"C:\Temp"
 -P:C:\Temp\DynMetOx_stat_4plex_iodo_hcd.xml
 -L:LogFile.txt
```

## Example command line #2
```
AScore_Console.exe -T:msgfplus 
 -F:Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt 
 -D:Dataset_W_S2_Fr_04_2May17.mzML 
 -P:AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml 
 -U:W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt
 -L:LogFile.txt
```

## Example command line #3
```
AScore_Console.exe -T:msgfplus
 -F:"C:\Temp\Multi_Job_Results_fht.txt"
 -JM:"C:\Temp\JobToDatasetNameMap.txt"
 -O:"C:\Temp\"
 -P:C:\Temp\DynPhos_stat_6plex_iodo_hcd.xml
 -L:C:\temp\LogFile.txt
 -noFM
```

## Example command line #4
```
AScore_Console.exe -T:msgfplus
 -F:"C:\Temp\Multi_Job_Results_fht.txt"
 -JM:"C:\Temp\JobToDatasetNameMap.txt"
 -O:"C:\Temp\"
 -P:C:\Temp\DynPhos_stat_6plex_iodo_hcd.xml
 -L:LogFile.txt
 -Fasta:C:\Temp\H_sapiens_Uniprot_SPROT_2013-09-18.fasta
 -PD
```

## Contacts

Written by Josh Aldrich for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov\
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

AScore Console is licensed under the Apache License, Version 2.0; you may not use this 
file except in compliance with the License.  You may obtain a copy of the 
License at https://opensource.org/licenses/Apache-2.0
