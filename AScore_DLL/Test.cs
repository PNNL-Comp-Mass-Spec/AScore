using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using System.IO;


namespace AScore_DLL
{
	[TestFixture]
	class Test
	{
		[Test]
		public void NewTest()
		{
			string[] ascore = new string[] { "SOSM_May_P1_R2_13Jun11_Hawk_11-04-02p_fht.txt"};
				//"SOSM_May_G2_R2_13Jun11_Hawk_11-04-02p_fht.txt",
				//"SOSM_May_M_R2_6Jun11_Hawk_11-04-02p_fht.txt",
				//"SOSM_May_P1_R1_6Jun11_Hawk_11-04-02p_fht.txt",
				//"SOSM_May_P1_R2_6Jun11_Hawk_11-04-02p_fht.txt"};

			string fhtPath = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM\CID\ForAScore";
			string dtapath = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM";
			string parFile = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM\parameterFile.xml";

			foreach (string a in ascore)
			{
				string myFht = Path.Combine(fhtPath, a);
				string myDta = System.IO.Path.Combine(dtapath, a.Substring(0, a.Length - 8) + "_dta.txt");
				string outFile = System.IO.Path.Combine(fhtPath, a.Substring(0, a.Length - 8) + "_AScore.txt");
				DatasetManager datasetMan = new SequestFHT(myFht);
				DtaManager dtaManager = new DtaManager(myDta);
				ParameterFileManager paramFile = new ParameterFileManager(parFile);

				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);

			}
		}

        [Test]
        public void ForJohnJacobs()
        {
            string myfht = @"C:\DMS_WorkDir\Step_1_ASCORE\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_xt.txt";
            string mydta = @"C:\DMS_WorkDir\Step_1_ASCORE\dtas\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_dta.txt";
            string mypar = @"C:\DMS_WorkDir\Step_1_ASCORE\DynMWPYOx_EmH20_QmNH3_cid.xml";

            DatasetManager dataman = new XTandemFHT(myfht);
            DtaManager dta = new DtaManager(mydta);
            ParameterFileManager par = new ParameterFileManager(mypar);
            string fileOutput = @"C:\DMS_WorkDir\Step_1_ASCORE\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_xt_ascore.txt";
			AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
			ascoreEngine.AlgorithmRun(dta, dataman, par, fileOutput);

        }
        [Test]
        public void SisiConfirm()
        {
            string[] dtaname = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\09302010_MG1655_phospho_S_08_B_100930113735_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_S_09_B_101001105913_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s06_101002094806_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s10_101001105913_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_S11_rerun_101005165350_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s12_101002094806_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s7_101002094806_dta.txt"
            };
            string[] datasetname = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\09302010_MG1655_phospho_S_08_B_100930113735_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_S_09_B_101001105913_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s06_101002094806_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s10_101001105913_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_S11_rerun_101005165350_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s12_101002094806_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s7_101002094806_msgfdb_fht.txt"

            };
            string[] ascParam = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml"
            };

            for (int i = 0; i < dtaname.Length; i++)
            {
                string outFile = Path.Combine(
                    Path.GetDirectoryName(datasetname[i]),
                    Path.GetFileNameWithoutExtension(datasetname[i]) + "_AScore.txt");
                DatasetManager datasetMan = new MsgfdbFHT(datasetname[i]);
                DtaManager dtaManager = new DtaManager(dtaname[i]);
                ParameterFileManager paramFile = new ParameterFileManager(ascParam[i]);
				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
            }

        }



		[Test]
		public void FengTest()
		{
			string[] datasetname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD_13Oct10_Hawk_03-10-10p_fht.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD-2_15Oct10_Hawk_03-10-10p_fht.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con_10Oct10_Hawk_03-10-10p_fht.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con-rr_15Oct10_Hawk_03-10-10p_fht.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_AD_13Oct10_Hawk_03-10-10p_fht.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_fht.txt"};

			string[] dtaname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD_13Oct10_Hawk_03-10-10p_dta.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD-2_15Oct10_Hawk_03-10-10p_dta.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con_10Oct10_Hawk_03-10-10p_dta.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con-rr_15Oct10_Hawk_03-10-10p_dta.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_AD_13Oct10_Hawk_03-10-10p_dta.txt",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_dta.txt"};

			string[] ascParam = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
				@"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml"};

			for (int i = 0; i < dtaname.Length; i++)
			{
				string outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreNoN2.txt";
				DatasetManager datasetMan = new SequestFHT(datasetname[i]);
				DtaManager dtaManager = new DtaManager(dtaname[i]);
				ParameterFileManager paramFile = new ParameterFileManager(ascParam[i]);
				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
			}


		}

		[Test]
		public void FengTest2()
		{
			string[] datasetname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_fht.txt"};

			string[] dtaname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_dta.txt"};

			string[] ascParam = new string[]{
				@"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml"};

			for (int i = 0; i < dtaname.Length; i++)
			{
				string outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreTry.txt";
				DatasetManager datasetMan = new SequestFHT(datasetname[i]);
				DtaManager dtaManager = new DtaManager(dtaname[i]);
				ParameterFileManager paramFile = new ParameterFileManager(ascParam[i]);
				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
			}


		}

		[Test]
		public void EcoliTest2()
		{
			string[] datasetname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\E_coli_BW_70_bottom_up_23Sep11_Draco_11-07-12_fht.txt"};

			string[] dtaname = new string[]{
			    @"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\E_coli_BW_70_bottom_up_23Sep11_Draco_11-07-12_dta.txt"};

			string[] ascParam = new string[]{
				@"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\parameterFileForMus.xml"};

			for (int i = 0; i < dtaname.Length; i++)
			{
				string outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreTry.txt";
				DatasetManager datasetMan = new SequestFHT(datasetname[i]);
				DtaManager dtaManager = new DtaManager(dtaname[i]);
				ParameterFileManager paramFile = new ParameterFileManager(ascParam[i]);
				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
			}


		}

		[Test]
		public void Testneut()
		{
			Console.WriteLine(PeptideScoresManager.GetPeptideScore(0.09,12,6));
			
		}

	

		[Test]
		public void QuYi()
		{
			string direct = @"C:\Documents and Settings\aldr699\My Documents2011\EColiPhos";

			string ascoreP = @"parameterFile.xml";

			List<string> fhtFiles = new List<string>{
				"775945_10012010_MG1655_phospho_s05_101002094806_msgfdb_fht.txt",
				"775946_10012010_MG1655_phospho_s11_101002094806_msgfdb_fht.txt",
				"775947_10022010_MG1655_phospho_s7_101002094806_msgfdb_fht.txt",
				"775948_10012010_MG1655_phospho_s10_101001105913_msgfdb_fht.txt",
				"775949_10012010_MG1655_phospho_s06_101002094806_msgfdb_fht.txt",
				"775950_10022010_MG1655_phospho_s12_101002094806_msgfdb_fht.txt",
				"775951_10022010_MG1655_phospho_S8FT_101005165350_msgfdb_fht.txt",
				"775944_10012010_MG1655_phospho_S_09_B_101001105913_msgfdb_fht.txt",
				"775942_09302010_MG1655_phospho_S_08_B_100930113735_msgfdb_fht.txt",
				"775952_10022010_MG1655_phospho_S11_rerun_101005165350_msgfdb_fht.txt",
				"775955_MG1655_phospho_S-01FT_5Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775956_MG1655_phospho_S-02FT_5Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775957_MG1655_phospho_S-03FT_8Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775958_MG1655_phospho_S-06FT_8Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775959_MG1655_phospho_S-09FT_12Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775960_MG1655_phospho_S-10FT_12Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775961_MG1655_phospho_S-12FT_15Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775962_MG1655_phospho_S-01_15Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775963_MG1655_phospho_S-02_17Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775964_MG1655_phospho_S-03_17Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775965_MG1655_phospho_S-04_17Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775966_MG1655_phospho_S-07_17Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775967_MG1655_phospho_S-12_19Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775968_MG1655_phospho_S-10_19Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775969_MG1655_phospho_S-09_19Nov10_Hawk_10-11-02p_msgfdb_fht.txt",
				"775953_10022010_MG1655_phospho_S05_rerun_101005165350_msgfdb_fht.txt",
				"775943_09302010_MG1655_phospho_S_04_B_100930113735_msgfdb_fht.txt",
				"775954_10022010_MG1655_phospho_S06_rerun_101005165350_msgfdb_fht.txt"};

			List<string> dtaFiles = new List<string>{
				"10012010_MG1655_phospho_s05_101002094806_dta.txt",
				"10012010_MG1655_phospho_s11_101002094806_dta.txt",
				"10022010_MG1655_phospho_s7_101002094806_dta.txt",
				"10012010_MG1655_phospho_s10_101001105913_dta.txt",
				"10012010_MG1655_phospho_s06_101002094806_dta.txt",
				"10022010_MG1655_phospho_s12_101002094806_dta.txt",
				"10022010_MG1655_phospho_S8FT_101005165350_dta.txt",
				"10012010_MG1655_phospho_S_09_B_101001105913_dta.txt",
				"09302010_MG1655_phospho_S_08_B_100930113735_dta.txt",
				"10022010_MG1655_phospho_S11_rerun_101005165350_dta.txt",
				"MG1655_phospho_S-01FT_5Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-02FT_5Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-03FT_8Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-06FT_8Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-09FT_12Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-10FT_12Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-12FT_15Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-01_15Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-02_17Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-03_17Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-04_17Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-07_17Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-12_19Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-10_19Nov10_Hawk_10-11-02p_dta.txt",
				"MG1655_phospho_S-09_19Nov10_Hawk_10-11-02p_dta.txt",
				"10022010_MG1655_phospho_S05_rerun_101005165350_dta.txt",
				"09302010_MG1655_phospho_S_04_B_100930113735_dta.txt",
				"10022010_MG1655_phospho_S06_rerun_101005165350_dta.txt"
			};


			for (int i = 0; i < fhtFiles.Count; i++)
			{
				string tempfht = System.IO.Path.Combine(direct, fhtFiles[i]);
				string tempdta = System.IO.Path.Combine(direct, dtaFiles[i]);
				string ascP = System.IO.Path.Combine(direct, ascoreP);
				string tempout = System.IO.Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");

				DatasetManager dsman = new MsgfdbFHT(tempfht);
				DtaManager dtman = new DtaManager(tempdta);
				ParameterFileManager pman = new ParameterFileManager(ascP);

				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);

			}




		}


        [Test]
        public void Sisi()
        {
            string ascoreP = "parameterFile.xml";
            string direct = @"C:\Users\aldr699\Documents\2012\Sisi_ASCORE\Group2";

            List<string> fhtFiles = new List<string>{
                "NMR_HetR_UG_01_20Jul11_Andromeda_11-06-19_fhtf.txt",
                "NMR_HetR_UG_02_20Jul11_Andromeda_11-06-29_fhtf.txt",
                "NMR_HetR_UG_03_20Jul11_Andromeda_11-06-19_fhtf.txt",
                "NMR_HetR_LG_01_20Jul11_Andromeda_11-06-29_fhtf.txt",
                "NMR_HetR_LG_02_20Jul11_Andromeda_11-06-29_fhtf.txt",
                "NMR_HetR_LG_03_20Jul11_Andromeda_11-06-19_fhtf.txt",
                "NMR_LaR80a_01_20Jul11_Andromeda_11-02-54_fhtf.txt",
                "NMR_LaR80a_02_20Jul11_Andromeda_11-02-56_fhtf.txt",
                "NMR_LaR80a_03_20Jul11_Andromeda_11-02-54_fhtf.txt"
                
            };

            List<string> dtaFiles = new List<string>{
                "NMR_HetR_UG_01_20Jul11_Andromeda_11-06-19_dta.txt",
                "NMR_HetR_UG_02_20Jul11_Andromeda_11-06-29_dta.txt",
                "NMR_HetR_UG_03_20Jul11_Andromeda_11-06-19_dta.txt",
                "NMR_HetR_LG_01_20Jul11_Andromeda_11-06-29_dta.txt",
                "NMR_HetR_LG_02_20Jul11_Andromeda_11-06-29_dta.txt",
                "NMR_HetR_LG_03_20Jul11_Andromeda_11-06-19_dta.txt",
                "NMR_LaR80a_01_20Jul11_Andromeda_11-02-54_dta.txt",
                "NMR_LaR80a_02_20Jul11_Andromeda_11-02-56_dta.txt",
                "NMR_LaR80a_03_20Jul11_Andromeda_11-02-54_dta.txt"
            };






            for (int i = 0; i < fhtFiles.Count; i++)
            {
                string tempfht = System.IO.Path.Combine(direct, fhtFiles[i]);
                string tempdta = System.IO.Path.Combine(direct, dtaFiles[i]);
                string ascP = System.IO.Path.Combine(direct, ascoreP);
                string tempout = System.IO.Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore2.txt");

                DatasetManager dsman = new SequestFHT(tempfht);
                DtaManager dtman = new DtaManager(tempdta);
                ParameterFileManager pman = new ParameterFileManager(ascP);

				AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
				ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);

            }
        }

        [Test]
        public void TestMSGFFilter()
        {
            string direct = @"C:\AldrichBackup\aldr699\My Documents\Visual Studio 2010\Projects\RevisedFinalAScore\AScore_DLL\TestCase";
            string fht = System.IO.Path.Combine(direct, "689706_GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_fht.txt");
            string param = System.IO.Path.Combine(direct, "parameterFileForGmax.xml");
            string dta = System.IO.Path.Combine(direct, "GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_dta.txt");

            DatasetManager dfht = new SequestFHT(fht);
            ParameterFileManager pfile = new ParameterFileManager(param);
            DtaManager dtaman = new DtaManager(dta);


            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

			AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
			ascoreEngine.AlgorithmRun(dtaman, dfht, pfile, @"C:\AldrichBackup\aldr699\My Documents\Visual Studio 2010\Projects\RevisedFinalAScore\AScore_DLL\TestCase\TestMSGFFilter.txt");

            sw.Stop();
            Console.WriteLine("Time used (float): {0} ms", sw.Elapsed.TotalMilliseconds);
        }

        [Test]
        public void QuickTest()
        {
            string s = "";
            double d = double.Parse(s);
        }

        [Test]
        public void OsmaniRedux()
        {
            string ascorePETD = "ETDPhos.xml";
            string ascorePCID = "CIDPhos.xml";
            string directETD = @"C:\Users\aldr699\Documents\2012\Osmani\ETD";
            string directCID = @"C:\Users\aldr699\Documents\2012\Osmani\CID";


            List<string> fhtFiles = new List<string>{
                "SOSM_May_G2_RR1_26Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_M_RR1_27Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_G2_RR2_27Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_M_RR2_29Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_G_RR1_31Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_P1_RR1_31Jan12_Hawk_11-11-03p_fhtf.txt"       
            };

            string direct2 = @"C:\Users\aldr699\Documents\2012\Osmani";

            List<string> dtaFiles = new List<string>{
                "SOSM_May_G2_RR1_26Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_M_RR1_27Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_G2_RR2_27Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_M_RR2_29Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_G_RR1_31Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_P1_RR1_31Jan12_Hawk_11-11-03p_dta.txt"   
            };





            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < fhtFiles.Count; i++)
                {
                    string direct = "";
                    string ascoreP = "";
                    if (j == 0)
                    {
                        direct = directETD;
                        ascoreP = ascorePETD;

                    }
                    else
                    {
                        direct = directCID;
                        ascoreP = ascorePCID;
                    }

                    string tempfht = System.IO.Path.Combine(direct, fhtFiles[i]);
                    string tempdta = System.IO.Path.Combine(direct2, dtaFiles[i]);
                    string ascP = System.IO.Path.Combine(direct, ascoreP);
                    string tempout = System.IO.Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");

                    DatasetManager dsman = new SequestFHT(tempfht);
                    DtaManager dtman = new DtaManager(tempdta);
                    ParameterFileManager pman = new ParameterFileManager(ascP);

					AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
					ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);

                }
            }
        }


        [Test]
        public void Sisi_Kidneys()
        {
            string ascoreP = "HCDPhos.xml";
            string directSeq = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas\Sequest";
            string directMsg = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas\MSGFDB";


            List<string> fhtFiles = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_fht.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_fht.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_fht.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_fht.txt"
            };

            List<string> msgfdbFile = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_msgfdb_fht.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_msgfdb_fht.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_msgfdb_fht.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_msgfdb_fht.txt"
            };


            string direct2 = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas";

            List<string> dtaFiles = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_dta.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_dta.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_dta.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_dta.txt" 
            };





            for (int j = 1; j < 2; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    string fht = "";
                    string direct = "";
                    if (j == 0)
                    {
                        direct = directSeq;
                        fht = fhtFiles[i];

                    }
                    else
                    {
                        direct = directMsg;
                        fht = msgfdbFile[i];
                    }

                    string tempfht = System.IO.Path.Combine(direct, fht);
                    string tempdta = System.IO.Path.Combine(direct2, dtaFiles[i]);
                    string ascP = System.IO.Path.Combine(direct2, ascoreP);
                    string tempout = System.IO.Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");
                    DatasetManager dsman = null;
                    if (j == 0)
                    {
                         dsman = new SequestFHT(tempfht);
                    }
                    else
                    {
                        dsman = new MsgfdbFHT(tempfht);
                    }
                    if (dsman == null)
                    {
                        Console.WriteLine("Failed to load fht");
                    }
                    DtaManager dtman = new DtaManager(tempdta);
                    ParameterFileManager pman = new ParameterFileManager(ascP);

					AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();
					ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);

                }
            }
        }


	}
}
 