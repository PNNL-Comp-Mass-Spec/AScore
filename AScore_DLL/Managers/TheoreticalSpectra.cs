﻿//Joshua Aldrich

using System.Collections.Generic;
using AScore_DLL.Mod;
using System.Collections;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// An object which holds the b and y ion uncharged masses
    /// </summary>
    public class TheoreticalSpectra:IEnumerable
    {

        private int chargeState;
        private int count;
        private double peptideMassWithStaticMods;

        readonly List<double> bIonsMass= new List<double>();
        readonly List<double> yIonsMass = new List<double>();
        readonly string peptideSequence;                         // Peptide sequence, without any mods

        public int Count => count;

        /// <summary>
        /// Computed, theoretical mass of the peptide, including static mods but not including dynamic mods
        /// </summary>
        public double PeptideNeutralMassWithStaticMods => peptideMassWithStaticMods;

        /// <summary>
        /// Creates the theoretical mass spectrum for the given peptide
        /// </summary>
        /// <param name="sequenceClean">Clean sequence (no mod symbols)</param>
        /// <param name="ascoreParameters"></param>
        /// <param name="chargeS"></param>
        /// <param name="moveMod"></param>
        /// <param name="massType"></param>
        /// <remarks>The theoretical mass spectrum takes into account static mods and static N-terminal mods.  It does not include dynamic mods</remarks>
        public TheoreticalSpectra(string sequenceClean, ParameterFileManager ascoreParameters, int chargeS,
            List<Mod.DynamicModification> moveMod, MassType massType)
        {
            chargeState = chargeS;
            this.peptideSequence = sequenceClean;
            Calculate(chargeState, ascoreParameters, massType);
        }

        /// <summary>
        /// Generates the b and y ions for a peptide, including adding static mods but excluding dynamic mods
        /// </summary>
        /// <param name="staticMods"></param>
        /// <param name="terminiMods"></param>
        /// <returns>The peptide mass, including static modifications</returns>
        private double GenerateIonMasses(IReadOnlyCollection<Modification> staticMods,
            List<Modification> terminiMods)
        {

            // Need to iterate through each amino acid in the peptide to
            // generate the ion masses
            for (var i = 0; i < peptideSequence.Length; ++i)
            {
                // Current amino acid
                var bAcid = peptideSequence[i];
                var yAcid = peptideSequence[peptideSequence.Length - i - 1];


                // Get the monoisotopic mass for the current amino acid

                bIonsMass.Add(AminoAcidMass.GetMassByLetter(bAcid));
                yIonsMass.Add(AminoAcidMass.GetMassByLetter(yAcid));

                foreach (var m in staticMods)
                {
                    if (m.Match(bAcid))
                    {
                        bIonsMass[i] += m.Mass;
                    }
                    if (m.Match(yAcid))
                    {
                        yIonsMass[i] += m.Mass;
                    }
                }


                // Add the previous mass to the current mass for a rolling total
                if (i > 0)
                {
                    bIonsMass[i] += bIonsMass[i - 1];
                    yIonsMass[i] += yIonsMass[i - 1];
                }

                // Check for c or n terminus modifications to be
                // applied to the first entry in the mass lists in order to
                // propagate to all the entries
                if (i == 0)
                {
                    bIonsMass[i] = EdgeCase(bIonsMass[i], terminiMods, bAcid, leftSide: true);
                    yIonsMass[i] = EdgeCase(yIonsMass[i], terminiMods, yAcid, leftSide: false);
                }

            }

            var peptideMass = bIonsMass[bIonsMass.Count - 1] - MolecularWeights.Hydrogen + MolecularWeights.Water;

            bIonsMass.RemoveAt(bIonsMass.Count - 1);
            //yIonsMonoisotopicMass[yIonsMonoisotopicMass.Count - 1] = EdgeCase(
            //    yIonsMonoisotopicMass[yIonsMonoisotopicMass.Count -1],
            //    terminiMods, peptideSequence[0], true);
            //yIonsMass.RemoveAt(yIonsMass.Count - 1);

            return peptideMass;
        }



        /// <summary>
        /// N and C terminal static mod cases
        /// </summary>
        /// <param name="mass">fragment ion mass</param>
        /// <param name="terminiMods">terminus mod list</param>
        /// <param name="aAcid">current sequence position amino acid letter</param>
        /// <param name="leftSide">true for n-terminus false for c-terminus</param>
        /// <returns>modified mass</returns>
        private static double EdgeCase(double mass, List<Modification> terminiMods, char aAcid, bool leftSide)
        {


            foreach (var t in terminiMods)
            {
                if (t.nTerminus && leftSide && t.Match(aAcid))
                {
                    mass += t.Mass;

                }
                if (t.cTerminus && !leftSide && t.Match(aAcid))
                {
                    mass += t.Mass;

                }
            }
            if (leftSide)
            {
                mass += MolecularWeights.Hydrogen;
            }
            else
            {
                mass += MolecularWeights.Hydrogen + MolecularWeights.Water;
            }
            return mass;
        }

        /// <summary>
        /// Makes the call to generate the ion masses
        /// Adjusts for etd fragmentation
        /// </summary>
        /// <param name="charge">charge, do i need this?</param>
        /// <param name="ascoreParameters">ascore parameters</param>
        /// <param name="massType"></param>
        private void Calculate(int charge, ParameterFileManager ascoreParameters, MassType massType)
        {
            chargeState = charge;

            AminoAcidMass.MassType = massType;
            MolecularWeights.MassType = massType;
            foreach (var sm in ascoreParameters.StaticMods)
            {
                sm.ModMassType = massType;
            }
            foreach (var tm in ascoreParameters.TerminiMods)
            {
                tm.ModMassType = massType;
            }

            // First thing to do is generate the base ion masses
            peptideMassWithStaticMods = GenerateIonMasses(ascoreParameters.StaticMods, ascoreParameters.TerminiMods);

            // If the fragment type is ETD, we need to convert the B and Y ions
            // into C and Z ions
            if (ascoreParameters.FragmentType == FragmentType.ETD)
            {
                for (var i = 0; i < yIonsMass.Count; ++i)
                {
                    if (i < bIonsMass.Count)
                    {
                        bIonsMass[i] += MolecularWeights.Ammonia;
                    }
                    yIonsMass[i] -= MolecularWeights.NH2;
                }
            }
        }


        /// <summary>
        /// Generates a theoretical spectra based on a modification list and positions for those modifications
        /// </summary>
        /// <param name="positions">int array has modifications at indice in which they occur in the sequence</param>
        /// <param name="myMods">dynamic modification list</param>
        /// <param name="massType"></param>
        /// <returns>Dictionary of theoretical ions organized by charge</returns>
        public Dictionary<int,ChargeStateIons> GetTempSpectra(int[] positions, List<DynamicModification> myMods,MassType massType)
        {
            var tempFragIons = new Dictionary<int, ChargeStateIons>();
            for (var i = 1; i < chargeState; ++i)
            {
                tempFragIons[i] = ChargeStateIons.GenerateFragmentIon(i, massType,
                    myMods, positions,
                    bIonsMass, yIonsMass, peptideSequence.Length);
            }
            return tempFragIons;
        }

        /// <summary>
        /// Provides an enumerable list of ions
        /// </summary>
        /// <returns>enumeration of b and y ion monoisotopic masses</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var bIon in bIonsMass)
                yield return bIon;

            foreach (var yIon in yIonsMass)
                yield return yIon;
        }

        /// <summary>
        /// Sets the spectra range... unused at this point can probably remove
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void SetSpectraRange(double min, double max)
        {
            // Set the range variables

            // Update the count to reflect the new range
            count = 0;
            foreach (double unused in this)
            {
                ++count;
            }
        }
    }
}
