using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TaskTracker.API.Models;

namespace TaskTracker.API.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MstDocument> MstDocuments { get; set; }

    public virtual DbSet<MstRole> MstRoles { get; set; }

    public virtual DbSet<MstTaskStatus> MstTaskStatuses { get; set; }

    public virtual DbSet<MstUser> MstUsers { get; set; }

    public virtual DbSet<TrxTask> TrxTasks { get; set; }

    public virtual DbSet<TrxTaskComment> TrxTaskComments { get; set; }

    public virtual DbSet<TrxTaskDocument> TrxTaskDocuments { get; set; }

    public virtual DbSet<TrxTaskHistory> TrxTaskHistories { get; set; }

    public virtual DbSet<TrxUserRole> TrxUserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MstDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId).HasName("mst_document_pkey");

            entity.ToTable("mst_document");

            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.FileBase64).HasColumnName("file_base64");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<MstRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("mst_role_pkey");

            entity.ToTable("mst_role");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.RoleDescription)
                .HasMaxLength(255)
                .HasColumnName("role_description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(200)
                .HasColumnName("role_name");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<MstTaskStatus>(entity =>
        {
            entity.HasKey(e => e.TaskStatusId).HasName("mst_task_status_pkey");

            entity.ToTable("mst_task_status");

            entity.Property(e => e.TaskStatusId).HasColumnName("task_status_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.StatusDescription)
                .HasMaxLength(255)
                .HasColumnName("status_description");
            entity.Property(e => e.StatusName)
                .HasMaxLength(255)
                .HasColumnName("status_name");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<MstUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("mst_user_pkey");

            entity.ToTable("mst_user");

            entity.HasIndex(e => e.Email, "mst_user_email_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<TrxTask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("trx_task_pkey");

            entity.ToTable("trx_task");

            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.AssigneeUserId).HasColumnName("assignee_user_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeadlineDate).HasColumnName("deadline_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.ReviewerUserId).HasColumnName("reviewer_user_id");
            entity.Property(e => e.TaskDescription).HasColumnName("task_description");
            entity.Property(e => e.TaskStatusId).HasColumnName("task_status_id");
            entity.Property(e => e.TaskTitle)
                .HasMaxLength(255)
                .HasColumnName("task_title");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<TrxTaskComment>(entity =>
        {
            entity.HasKey(e => e.TaskCommentId).HasName("trx_task_comment_pkey");

            entity.ToTable("trx_task_comment");

            entity.Property(e => e.TaskCommentId).HasColumnName("task_comment_id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.ReviewerUserId).HasColumnName("reviewer_user_id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<TrxTaskDocument>(entity =>
        {
            entity.HasKey(e => e.TaskDocumentId).HasName("trx_task_document_pkey");

            entity.ToTable("trx_task_document");

            entity.Property(e => e.TaskDocumentId).HasColumnName("task_document_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.DocumentType)
                .HasMaxLength(50)
                .HasColumnName("document_type");
            entity.Property(e => e.DocumentVersion)
                .HasDefaultValue(1)
                .HasColumnName("document_version");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<TrxTaskHistory>(entity =>
        {
            entity.HasKey(e => e.TaskHistoryId).HasName("trx_task_history_pkey");

            entity.ToTable("trx_task_history");

            entity.Property(e => e.TaskHistoryId).HasColumnName("task_history_id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.NewTaskStatusId).HasColumnName("new_task_status_id");
            entity.Property(e => e.OldTaskStatusId).HasColumnName("old_task_status_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
        });

        modelBuilder.Entity<TrxUserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("trx_user_role_pkey");

            entity.ToTable("trx_user_role");

            entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_date");
            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100)
                .HasColumnName("deleted_by");
            entity.Property(e => e.DeletedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deleted_date");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
