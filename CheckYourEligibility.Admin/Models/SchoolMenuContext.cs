namespace CheckYourEligibility.Admin.Models
{
    public class SchoolMenuContext
    {
        public bool IsSchool { get; set; }
        public bool IsPartOfMat { get; set; }
        public int? MatId { get; set; }
        public int? LaCode { get; set; }
        public bool ShowReviewEvidenceTiles { get; set; }
    }
}
