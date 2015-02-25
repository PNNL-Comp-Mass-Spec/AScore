using System;

namespace AScore_DLL.Managers.SpectraManagers.MZML
{
    public class IsolationWindow: IComparable<IsolationWindow>
    {
        public const double Proton = 1.00727649;

        public IsolationWindow(
            double isolationWindowTargetMz,
            double isolationWindowLowerOffset,
            double isolationWindowUpperOffset,
            double? monoisotopicMz = null,
            int? charge = null
            )
        {
            IsolationWindowTargetMz = isolationWindowTargetMz;
            IsolationWindowLowerOffset = isolationWindowLowerOffset;
            IsolationWindowUpperOffset = isolationWindowUpperOffset;
            MonoisotopicMz = monoisotopicMz;
            SetCharge = charge;
        }

        public double IsolationWindowTargetMz { get; private set; }
        public double IsolationWindowLowerOffset { get; private set; }
        public double IsolationWindowUpperOffset { get; private set; }
        public double? MonoisotopicMz { get; set; }
        public double SelectedIonMz { get; set; }
        public int? SetCharge { get; set; }
        public int? OldCharge { get; set; }

        public int Charge
        {
            get
            {
                // Workaround for completeness, if one of the ProteoWizard charge determination filters is used.
                if (SetCharge != null && SetCharge != 0)
                {
                    return (int)SetCharge;
                }
                if (OldCharge != null && OldCharge != 0)
                {
                    return (int)OldCharge;
                }
                return 0;
            }
            set { SetCharge = value; }
        }

        public double MinMz
        {
            get
            {
                return IsolationWindowTargetMz - IsolationWindowLowerOffset;
            }
        }

        public double MaxMz
        {
            get
            {
                return IsolationWindowTargetMz + IsolationWindowUpperOffset;
            }
        }

        public double Width
        {
            get { return IsolationWindowUpperOffset + IsolationWindowLowerOffset; }
        }

        public double? MonoisotopicMass
        {
            get
            {
                if (MonoisotopicMz != null && Charge != 0) return (MonoisotopicMz - Proton)*Charge;
                return null;
            }
        }

        public double MPlusHMass
        {
            get
            {
                double mz = IsolationWindowTargetMz;
                mz = SelectedIonMz;
                if (MonoisotopicMz != null)
                {
                    mz = (double) MonoisotopicMz;
                }
                if (Charge != 0)
                {
                    return ((mz - Proton)*Charge) + Proton;
                    //return (double)((mz * Charge) - (Charge * Proton) + Proton);
                }
                return mz;
            }
        }

        public bool Contains(double mz)
        {
            return mz >= MinMz && mz < MaxMz;
        }

        protected bool Equals(IsolationWindow other)
        {
            if (Math.Abs(MinMz - other.MinMz) < 0.01 && Math.Abs(MaxMz - other.MaxMz) < 0.01) return true;
            return false;
        }

        public int CompareTo(IsolationWindow other)
        {
            return IsolationWindowTargetMz.CompareTo(other.IsolationWindowTargetMz);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IsolationWindow)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsolationWindowTargetMz.GetHashCode();
                hashCode = (hashCode * 397) ^ IsolationWindowLowerOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ IsolationWindowUpperOffset.GetHashCode();
                return hashCode;
            }
        }
    }
}
