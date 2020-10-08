using System;
using System.Collections.Generic;
using System.IO;
using AScore_DLL.Managers;
using AScore_DLL.Managers.PSM_Managers;
using AScore_DLL.Managers.SpectraManagers;
using NUnit.Framework;

namespace AScore_UnitTests
{
    [TestFixture]
    class Test
    {
        [Test]
        [Ignore("Local testing")]
        public void NewTest()
        {
            var ascore = new string[] { "SOSM_May_P1_R2_13Jun11_Hawk_11-04-02p_fht.txt"};
                //"SOSM_May_G2_R2_13Jun11_Hawk_11-04-02p_fht.txt",
                //"SOSM_May_M_R2_6Jun11_Hawk_11-04-02p_fht.txt",
                //"SOSM_May_P1_R1_6Jun11_Hawk_11-04-02p_fht.txt",
                //"SOSM_May_P1_R2_6Jun11_Hawk_11-04-02p_fht.txt"};

            var fhtPath = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM\CID\ForAScore";
            var dtapath = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM";
            var parFile = @"C:\Documents and Settings\aldr699\My Documents2011\SOSM\parameterFile.xml";

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            foreach (var a in ascore)
            {
                var myFht = Path.Combine(fhtPath, a);
                var myDta = Path.Combine(dtapath, a.Substring(0, a.Length - 8) + "_dta.txt");
                var outFile = Path.Combine(fhtPath, a.Substring(0, a.Length - 8) + "_AScore.txt");
                PsmResultsManager datasetMan = new SequestFHT(myFht);
                //DtaManager dtaManager = new DtaManager(myDta);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(myDta);
                var paramFile = new ParameterFileManager(parFile);

                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, datasetMan, paramFile, outFile);
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void ForJohnJacobs()
        {
            var myfht = @"C:\DMS_WorkDir\Step_1_ASCORE\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_xt.txt";
            var mydta = @"C:\DMS_WorkDir\Step_1_ASCORE\dtas\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_dta.txt";
            var mypar = @"C:\DMS_WorkDir\Step_1_ASCORE\DynMWPYOx_EmH20_QmNH3_cid.xml";

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            PsmResultsManager dataman = new XTandemFHT(myfht);
            //DtaManager dta = new DtaManager(mydta);
            var spectraCache = new SpectraManagerCache(peptideMassCalculator);
            spectraCache.OpenFile(mydta);
            var par = new ParameterFileManager(mypar);
            var fileOutput = @"C:\DMS_WorkDir\Step_1_ASCORE\U54_HPp1_LoBMI_NS_11_5Sep08_Draco_08-07-15_xt_ascore.txt";
            var ascoreEngine = new AScore_DLL.AScoreProcessor();
            //ascoreEngine.AlgorithmRun(dta, dataman, par, fileOutput);
            ascoreEngine.RunAScoreOnSingleFile(spectraCache, dataman, par, fileOutput);
        }

        [Test]
        [Ignore("Local testing")]
        public void SisiConfirm()
        {
            var dtaname = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\09302010_MG1655_phospho_S_08_B_100930113735_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_S_09_B_101001105913_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s06_101002094806_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s10_101001105913_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_S11_rerun_101005165350_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s12_101002094806_dta.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s7_101002094806_dta.txt"
            };
            var datasetname = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\09302010_MG1655_phospho_S_08_B_100930113735_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_S_09_B_101001105913_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s06_101002094806_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10012010_MG1655_phospho_s10_101001105913_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_S11_rerun_101005165350_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s12_101002094806_msgfdb_fht.txt",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\10022010_MG1655_phospho_s7_101002094806_msgfdb_fht.txt"
            };
            var ascParam = new string[]{
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml",
                @"C:\Users\aldr699\Documents\2012\Sisi_Work\PhosphohistidineConfirmation\HistPhos.xml"
            };

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < dtaname.Length; i++)
            {
                var outFile = Path.Combine(
                    Path.GetDirectoryName(datasetname[i]),
                    Path.GetFileNameWithoutExtension(datasetname[i]) + "_AScore.txt");
                PsmResultsManager datasetMan = new MsgfdbFHT(datasetname[i]);
                //DtaManager dtaManager = new DtaManager(dtaname[i]);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(dtaname[i]);
                var paramFile = new ParameterFileManager(ascParam[i]);
                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, datasetMan, paramFile, outFile);
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void FengTest()
        {
            var datasetname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD_13Oct10_Hawk_03-10-10p_fht.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD-2_15Oct10_Hawk_03-10-10p_fht.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con_10Oct10_Hawk_03-10-10p_fht.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con-rr_15Oct10_Hawk_03-10-10p_fht.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_AD_13Oct10_Hawk_03-10-10p_fht.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_fht.txt"};

            var dtaname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD_13Oct10_Hawk_03-10-10p_dta.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_AD-2_15Oct10_Hawk_03-10-10p_dta.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con_10Oct10_Hawk_03-10-10p_dta.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\A_Mus_con-rr_15Oct10_Hawk_03-10-10p_dta.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_AD_13Oct10_Hawk_03-10-10p_dta.txt",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_dta.txt"};

            var ascParam = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml",
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml"};

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < dtaname.Length; i++)
            {
                var outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreNoN2.txt";
                PsmResultsManager datasetMan = new SequestFHT(datasetname[i]);
                //DtaManager dtaManager = new DtaManager(dtaname[i]);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(dtaname[i]);
                var paramFile = new ParameterFileManager(ascParam[i]);
                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, datasetMan, paramFile, outFile);
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void FengTest2()
        {
            var datasetname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_fht.txt"};

            var dtaname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\ETD\B_Mus_con_15Oct10_Hawk_03-10-10p_dta.txt"};

            var ascParam = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\FengTest\parameterFileForMusETD2.xml"};

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < dtaname.Length; i++)
            {
                var outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreTry.txt";
                PsmResultsManager datasetMan = new SequestFHT(datasetname[i]);
                //DtaManager dtaManager = new DtaManager(dtaname[i]);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(dtaname[i]);
                var paramFile = new ParameterFileManager(ascParam[i]);
                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, datasetMan, paramFile, outFile);
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void EcoliTest2()
        {
            var datasetname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\E_coli_BW_70_bottom_up_23Sep11_Draco_11-07-12_fht.txt"};

            var dtaname = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\E_coli_BW_70_bottom_up_23Sep11_Draco_11-07-12_dta.txt"};

            var ascParam = new string[]{
                @"C:\Documents and Settings\aldr699\My Documents2011\SisiTopDown\parameterFileForMus.xml"};

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < dtaname.Length; i++)
            {
                var outFile = datasetname[i].Substring(0, datasetname[i].Length - 8) + "_AScoreTry.txt";
                PsmResultsManager datasetMan = new SequestFHT(datasetname[i]);
                //DtaManager dtaManager = new DtaManager(dtaname[i]);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(dtaname[i]);
                var paramFile = new ParameterFileManager(ascParam[i]);
                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtaManager, datasetMan, paramFile, outFile);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, datasetMan, paramFile, outFile);
            }
        }

        [Test]
        public void Testneut()
        {
            const double prob = 0.09;
            const int numPossMatch = 12;
            const int matches = 6;

            var score = PeptideScoresManager.GetPeptideScore(prob, numPossMatch, matches);
            Console.WriteLine("Match with probability {0}, {1} possible matches, and {2} matches results in score {3:F4}", prob, numPossMatch, matches, score);

            Assert.AreEqual(35.17035, score, 0.00001);
        }

        [Test]
        [Ignore("Local testing")]
        public void QuYi()
        {
            var direct = @"C:\Documents and Settings\aldr699\My Documents2011\EColiPhos";

            var ascoreP = @"parameterFile.xml";

            var fhtFiles = new List<string>{
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

            var dtaFiles = new List<string>{
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

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < fhtFiles.Count; i++)
            {
                var tempfht = Path.Combine(direct, fhtFiles[i]);
                var tempdta = Path.Combine(direct, dtaFiles[i]);
                var ascP = Path.Combine(direct, ascoreP);
                var tempout = Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");

                PsmResultsManager dsman = new MsgfdbFHT(tempfht);
                //DtaManager dtman = new DtaManager(tempdta);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(tempdta);
                var pman = new ParameterFileManager(ascP);

                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, dsman, pman, tempout);
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void Sisi()
        {
            var ascoreP = "parameterFile.xml";
            var direct = @"C:\Users\aldr699\Documents\2012\Sisi_ASCORE\Group2";

            var fhtFiles = new List<string>{
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

            var dtaFiles = new List<string>{
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

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var i = 0; i < fhtFiles.Count; i++)
            {
                var tempfht = Path.Combine(direct, fhtFiles[i]);
                var tempdta = Path.Combine(direct, dtaFiles[i]);
                var ascP = Path.Combine(direct, ascoreP);
                var tempout = Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore2.txt");

                PsmResultsManager dsman = new SequestFHT(tempfht);
                //DtaManager dtman = new DtaManager(tempdta);
                var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                spectraCache.OpenFile(tempdta);
                var pman = new ParameterFileManager(ascP);

                var ascoreEngine = new AScore_DLL.AScoreProcessor();
                //ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);
                ascoreEngine.RunAScoreOnSingleFile(spectraCache, dsman, pman, tempout);
            }
        }

        [Test]
        public void TestMSGFFilter()
        {
            var workDir = @"F:\My Documents\Projects\JoshAldrich\AScore\AScore_DLL\TestCase";
            var fht = Path.Combine(workDir, "GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_fht.txt");
            var param = Path.Combine(workDir, "AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da_MSGF1E-12.xml");
            var dta = Path.Combine(workDir, "GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_dta.txt");
            var resultsFile = Path.Combine(workDir, "GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_fht_ascore.txt");

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            PsmResultsManager dfht = new SequestFHT(fht);
            var pfile = new ParameterFileManager(param);

            var spectraCache = new SpectraManagerCache(peptideMassCalculator);
            spectraCache.OpenFile(dta);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var ascoreEngine = new AScore_DLL.AScoreProcessor();
            ascoreEngine.RunAScoreOnSingleFile(spectraCache, dfht, pfile, resultsFile);

            sw.Stop();
            Console.WriteLine("Time elapsed: {0:F1} seconds", sw.Elapsed.TotalSeconds);
            Console.WriteLine("Results in " + resultsFile);
        }

        [Test]
        public void TestMSGFPlusResults()
        {
            var workDir = @"F:\My Documents\Projects\JoshAldrich\AScore\AScore_DLL\TestCase";
            var fht = Path.Combine(workDir, "CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_msgfdb_fht.txt");
            var param = Path.Combine(workDir, "AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da_MSGF1E-12.xml");
            var dta = Path.Combine(workDir, "CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_dta.txt");
            var resultsFile = Path.Combine(workDir, "CPTAC_CompREF_00_iTRAQ_NiNTA_01b_22Mar12_Lynx_12-02-29_msgfdb_fht_ascore.txt");

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            PsmResultsManager dfht = new MsgfdbFHT(fht);
            var pfile = new ParameterFileManager(param);

            var spectraCache = new SpectraManagerCache(peptideMassCalculator);
            spectraCache.OpenFile(dta);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var ascoreEngine = new AScore_DLL.AScoreProcessor();
            ascoreEngine.RunAScoreOnSingleFile(spectraCache, dfht, pfile, resultsFile);

            sw.Stop();
            Console.WriteLine("Time elapsed: {0:F1} seconds", sw.Elapsed.TotalSeconds);
            Console.WriteLine("Results in " + resultsFile);
        }

        [Test]
        public void QuickTest()
        {
            var s = "3.432";
            var d = double.Parse(s);

            Console.WriteLine("{0} converts to {1}", s, d);
            Assert.AreEqual(3.432, d, 0.0001);
        }

        [Test]
        [Ignore("Local testing")]
        public void OsmaniRedux()
        {
            var ascorePETD = "ETDPhos.xml";
            var ascorePCID = "CIDPhos.xml";
            var directETD = @"C:\Users\aldr699\Documents\2012\Osmani\ETD";
            var directCID = @"C:\Users\aldr699\Documents\2012\Osmani\CID";

            var fhtFiles = new List<string>{
                "SOSM_May_G2_RR1_26Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_M_RR1_27Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_G2_RR2_27Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_M_RR2_29Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_G_RR1_31Jan12_Hawk_11-11-03p_fhtf.txt",
                "SOSM_May_P1_RR1_31Jan12_Hawk_11-11-03p_fhtf.txt"
            };

            var direct2 = @"C:\Users\aldr699\Documents\2012\Osmani";

            var dtaFiles = new List<string>{
                "SOSM_May_G2_RR1_26Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_M_RR1_27Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_G2_RR2_27Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_M_RR2_29Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_G_RR1_31Jan12_Hawk_11-11-03p_dta.txt",
                "SOSM_May_P1_RR1_31Jan12_Hawk_11-11-03p_dta.txt"
            };

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var j = 0; j < 2; j++)
            {
                for (var i = 0; i < fhtFiles.Count; i++)
                {
                    var direct = "";
                    var ascoreP = "";
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

                    var tempfht = Path.Combine(direct, fhtFiles[i]);
                    var tempdta = Path.Combine(direct2, dtaFiles[i]);
                    var ascP = Path.Combine(direct, ascoreP);
                    var tempout = Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");

                    PsmResultsManager dsman = new SequestFHT(tempfht);
                    //DtaManager dtman = new DtaManager(tempdta);
                    var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                    spectraCache.OpenFile(tempdta);
                    var pman = new ParameterFileManager(ascP);

                    var ascoreEngine = new AScore_DLL.AScoreProcessor();
                    //ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);
                    ascoreEngine.RunAScoreOnSingleFile(spectraCache, dsman, pman, tempout);
                }
            }
        }

        [Test]
        [Ignore("Local testing")]
        public void Sisi_Kidneys()
        {
            var ascoreP = "HCDPhos.xml";
            var directSeq = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas\Sequest";
            var directMsg = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas\MSGFDB";

            var fhtFiles = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_fht.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_fht.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_fht.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_fht.txt"
            };

            var msgfdbFile = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_msgfdb_fht.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_msgfdb_fht.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_msgfdb_fht.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_msgfdb_fht.txt"
            };

            var direct2 = @"C:\Users\aldr699\Documents\2012\Sisi_Work\DarthVehDas";

            var dtaFiles = new List<string>{
                "Kidney_ACHN_Das_1_pTyr_HCD_8May12_Lynx_12-02-29_dta.txt",
                "Kidney_ACHN_Veh_1_pTyr_HCD_10May12_Lynx_12-02-29_dta.txt",
                "Kidney_ACHN_Veh_2_IMAC_HCD_10May12_Lynx_12-02-31_dta.txt",
                "Kidney_ACHN_Das_2_IMAC_HCD_11May12_Lynx_12-02-31_dta.txt"
            };

            var peptideMassCalculator = GetDefaultPeptideMassCalculator();

            for (var j = 1; j < 2; j++)
            {
                for (var i = 0; i < 4; i++)
                {
                    var fht = "";
                    var direct = "";
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

                    var tempfht = Path.Combine(direct, fht);
                    var tempdta = Path.Combine(direct2, dtaFiles[i]);
                    var ascP = Path.Combine(direct2, ascoreP);
                    var tempout = Path.Combine(direct, fhtFiles[i].Substring(0, fhtFiles[i].Length - 4) + "_AScore.txt");
                    PsmResultsManager dsman = null;
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
                    //DtaManager dtman = new DtaManager(tempdta);
                    var spectraCache = new SpectraManagerCache(peptideMassCalculator);
                    spectraCache.OpenFile(tempdta);
                    var pman = new ParameterFileManager(ascP);

                    var ascoreEngine = new AScore_DLL.AScoreProcessor();
                    //ascoreEngine.AlgorithmRun(dtman, dsman, pman, tempout);
                    ascoreEngine.RunAScoreOnSingleFile(spectraCache, dsman, pman, tempout);
                }
            }
        }

        private PHRPReader.clsPeptideMassCalculator GetDefaultPeptideMassCalculator()
        {
            return new PHRPReader.clsPeptideMassCalculator();
        }
    }
}
