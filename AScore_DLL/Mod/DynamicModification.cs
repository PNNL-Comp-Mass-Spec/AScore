namespace AScore_DLL.Mod
{
    /// <summary>
    /// Dynamic modification object
    /// Extends the Modification class by adding several fields specific to Dynamic mods
    /// </summary>
    public class DynamicModification : Modification
    {
        public int MaxPerSite { get; set; }
        public int Position { get; set; }
        public int Count { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DynamicModification()
        {
        }

        /// <summary>
        /// Constructor whose source is a dynamic mod entry
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public DynamicModification(DynamicModification itemToCopy)
        {
            CopyFrom(itemToCopy);
        }

        protected void CopyFrom(DynamicModification itemToCopy)
        {
            base.CopyFrom(itemToCopy);

            MaxPerSite = itemToCopy.MaxPerSite;
            Position = itemToCopy.Position;
            Count = itemToCopy.Count;
        }
    }
}
