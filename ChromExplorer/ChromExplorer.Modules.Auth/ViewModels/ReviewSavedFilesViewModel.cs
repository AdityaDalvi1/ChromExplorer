using ChromExplorer.Modules.Auth.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ChromExplorer.Modules.Auth.ViewModels
{
    public class ReviewSavedFilesViewModel
    {
        private readonly string _connectionString = @"Data Source=source.db3;Version=3;BusyTimeout=15000;";

        // recordType filters to one tab's category (SavedFileRegistry.TypeSampleSequence /
        // TypeReport / TypeSampleAnalysis); null/empty returns everything.
        //
        // Only a Reviewer sees every creator's files — a regular analyst only sees the files
        // they themselves saved, so pass isReviewer=false with the current user's id to scope
        // the list down to "my files" (currentUserId is ignored when isReviewer is true).
        public List<SavedFileRecord> GetSavedFiles(string recordType = null, int currentUserId = 0, bool isReviewer = true)
        {
            var list = new List<SavedFileRecord>();

            ChromExplorer.Infrastructure.SavedFileRegistry.EnsureTableExists();

            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT
                            f.file_id,
                            f.record_type,
                            f.file_path,
                            f.file_name,
                            f.file_ext,
                            f.is_active,
                            f.inserted_date,
                            f.updated_date,
                            f.is_reviewed,
                            f.reviewer_remark,
                            f.reviewed_date,
                            f.reviewed_by_username,
                            f.reviewed_by_role,
                            f.review_verdict,
                            f.review_status,
                            f.submitted_by_username,
                            f.submitted_date,
                            IFNULL(NULLIF(iu.user_fullname,''), IFNULL(iu.user_name,'Unknown User')) AS inserted_by_name,
                            ir.role_description AS inserted_by_role,
                            uu.user_name AS updated_by_name
                        FROM FileM f
                        LEFT JOIN UserM iu ON iu.user_id = f.inserted_by
                        LEFT JOIN roleM ir ON ir.role_id = iu.role_id
                        LEFT JOIN UserM uu ON uu.user_id = f.updated_by";

                    var conditions = new List<string>();
                    if (!string.IsNullOrEmpty(recordType))
                        conditions.Add("f.record_type = @recordType");
                    if (!isReviewer)
                        conditions.Add("f.inserted_by = @currentUserId");

                    if (conditions.Count > 0)
                        sql += " WHERE " + string.Join(" AND ", conditions);

                    sql += " ORDER BY f.file_id DESC";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        if (!string.IsNullOrEmpty(recordType))
                            cmd.Parameters.AddWithValue("@recordType", recordType);
                        if (!isReviewer)
                            cmd.Parameters.AddWithValue("@currentUserId", currentUserId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int srNo = 1;
                            while (reader.Read())
                            {
                                list.Add(new SavedFileRecord
                                {
                                    SrNo = srNo++,
                                    SavedFileId = Convert.ToInt64(reader["file_id"]),
                                    RecordType = reader["record_type"]?.ToString(),
                                    FolderPath = reader["file_path"]?.ToString(),
                                    FileName = reader["file_name"]?.ToString(),
                                    FileExt = reader["file_ext"]?.ToString(),
                                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                                    InsertedByName = reader["inserted_by_name"]?.ToString(),
                                    InsertedByRole = reader["inserted_by_role"] == DBNull.Value ? "" : reader["inserted_by_role"].ToString(),
                                    InsertedDate = reader["inserted_date"]?.ToString(),
                                    UpdatedByName = reader["updated_by_name"] == DBNull.Value ? "" : reader["updated_by_name"].ToString(),
                                    UpdatedDate = reader["updated_date"] == DBNull.Value ? "" : reader["updated_date"].ToString(),
                                    IsReviewed = reader["is_reviewed"] != DBNull.Value && Convert.ToInt32(reader["is_reviewed"]) == 1,
                                    ReviewerRemark = reader["reviewer_remark"] == DBNull.Value ? "" : reader["reviewer_remark"].ToString(),
                                    ReviewedByUsername = reader["reviewed_by_username"] == DBNull.Value ? "" : reader["reviewed_by_username"].ToString(),
                                    ReviewedByRole = reader["reviewed_by_role"] == DBNull.Value ? "" : reader["reviewed_by_role"].ToString(),
                                    ReviewedDate = reader["reviewed_date"] == DBNull.Value ? "" : reader["reviewed_date"].ToString(),
                                    ReviewVerdict = reader["review_verdict"] == DBNull.Value ? "" : reader["review_verdict"].ToString(),
                                    ReviewStatus = reader["review_status"] == DBNull.Value ? "Not Submitted" : reader["review_status"].ToString(),
                                    SubmittedByUsername = reader["submitted_by_username"] == DBNull.Value ? "" : reader["submitted_by_username"].ToString(),
                                    SubmittedDate = reader["submitted_date"] == DBNull.Value ? "" : reader["submitted_date"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return list;
        }
    }
}
