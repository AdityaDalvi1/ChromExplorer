using System.IO;

namespace ChromExplorer.Modules.Auth.Models
{
    // One row of the Reviewer's "Review Saved Files" grid — mirrors the FileM table
    // (ChromExplorer.Infrastructure.SavedFileRegistry writes it, this just reads it back).
    public class SavedFileRecord
    {
        public int SrNo { get; set; }
        public long SavedFileId { get; set; }
        public string RecordType { get; set; }
        public string FolderPath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public bool IsActive { get; set; }
        public string InsertedByName { get; set; }
        public string InsertedByRole { get; set; }
        public string InsertedDate { get; set; }
        public string UpdatedByName { get; set; }
        public string UpdatedDate { get; set; }
        public bool IsReviewed { get; set; }
        public string ReviewerRemark { get; set; }
        public string ReviewedByUsername { get; set; }
        public string ReviewedByRole { get; set; }
        public string ReviewedDate { get; set; }
        public string ReviewVerdict { get; set; }

        // Only meaningful for RecordType == "Method File": Not Submitted / Pending Review / Reviewed.
        public string ReviewStatus { get; set; }
        public string SubmittedByUsername { get; set; }
        public string SubmittedDate { get; set; }

        public string FullPath => Path.Combine(FolderPath ?? "", (FileName ?? "") + (FileExt ?? ""));

        public string FileNameWithExt => (FileName ?? "") + (FileExt ?? "");

        // "vaishnavi (Reviewer)" — the electronic signature, shown as one column in the grid.
        public string ReviewedBySignature =>
            string.IsNullOrEmpty(ReviewedByUsername) ? "" : $"{ReviewedByUsername} ({ReviewedByRole})";

        // "vaishnaviT (Analyst)" — who actually created this record, and in what role.
        public string InsertedBySignature =>
            string.IsNullOrEmpty(InsertedByRole) ? InsertedByName : $"{InsertedByName} ({InsertedByRole})";

        public bool IsMethodFile => RecordType == "Method File";

        // The "Submit for Review" action button is only offered for a Method File that the
        // analyst hasn't already handed off.
        public bool CanSubmitForReview => IsMethodFile && ReviewStatus == "Not Submitted";

        // The "Review" button (reason + password + remark/verdict) is only offered for a record
        // that hasn't already been reviewed, and — for a Method File specifically — only once the
        // analyst has actually submitted it. Whether the CURRENT USER is actually a Reviewer is
        // checked at click time (this is just per-record eligibility, not a role check).
        public bool CanReview => !IsReviewed && (!IsMethodFile || ReviewStatus == "Pending Review");

        // A "Download" action is only offered for a Report (PDF) that has already been reviewed
        // and electronically signed — gives the analyst/creator a way to grab their own copy of
        // the signed PDF instead of only being able to open it via View.
        public bool CanDownload => RecordType == "Report" && IsReviewed;

        // Not read from the DB — set once per grid load (see ReviewSavedFiles.xaml.cs LoadData)
        // from AuthService.IsCurrentUserReviewer() so the "Review" button's own Visibility
        // trigger can require BOTH per-record eligibility (CanReview) AND that the person looking
        // at the grid right now is actually a Reviewer, instead of showing the button to everyone
        // and only rejecting the click afterward.
        public bool IsReviewerSession { get; set; }
    }
}
