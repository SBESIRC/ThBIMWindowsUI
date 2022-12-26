namespace THBimEngine.Domain
{
    public class ShortProjectFile
    {
        public string ProjectFileId { get; set; }
        public string ProjectMainFileUploadId { get; set; }
        public string MainFileId { get; set; }
        public string PrjId { get; set; }
        public string PrjName { get; set; }
        public string SubPrjId { get; set; }
        public string SubPrjName { get; set; }
        public EMajor Major { get; set; }
        public string MajorName
        {
            get
            {
                return EnumUtil.GetEnumDescription(Major);
            }
        }
        public EApplcationName ApplcationName { get; set; }
    }
}
