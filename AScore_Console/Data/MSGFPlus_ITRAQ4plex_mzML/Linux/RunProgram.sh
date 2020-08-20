#!/bin/bash

mono AScore_Console.exe \
	-T:msgfplus \
	-F:data/MSGFPlus_ITRAQ4plex_mzML/Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt \
	-D:data/MSGFPlus_ITRAQ4plex_mzML/Dataset_W_S2_Fr_04_2May17.mzML \
	-P:data/MSGFPlus_ITRAQ4plex_mzML/AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml \
	-U:Dataset_W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt \
    -O:data/MSGFPlus_ITRAQ4plex_mzML \
	-L:data/MSGFPlus_ITRAQ4plex_mzML/AScore_LogFile.txt \
	-Fasta:data/H_sapiens_M_musculus_RefSeq_Excerpt.fasta


