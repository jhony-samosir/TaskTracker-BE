# MyTask Backend Service Implementation Plan

Dokumen ini mendefinisikan rancangan teknis, batasan bisnis, kontrak API, dan pembagian GitHub Issue untuk fitur **MyTask** pada backend TaskTracker.

## 1. Tujuan Modul MyTask

Modul **MyTask** adalah service khusus untuk pengguna login agar dapat melihat dan mengelola tugas yang berkaitan langsung dengan dirinya, tanpa akses ke seluruh data task seperti modul **Task Management** milik Admin.

Fokus utama:
- Menampilkan daftar task milik user login sebagai **Assignee** atau **Reviewer**.
- Menampilkan detail task yang hanya bisa diakses oleh user terkait.
- Mengizinkan user memperbarui status task sesuai alur kerja.
- Mengizinkan user menambahkan komentar/review pada task terkait.
- Menyediakan response yang kompatibel dengan halaman frontend `/my-tasks`.

## 2. Scope dan Batasan Bisnis

### In Scope
- Endpoint khusus `/api/my-tasks`.
- Query task berdasarkan user dari JWT claim `NameIdentifier`.
- Filter `search`, `statusId`, `priority`, dan `sortBy` jika data pendukung tersedia.
- Detail task beserta history, comments, dan documents.
- Update status task oleh assignee/reviewer yang terkait.
- Add comment oleh assignee/reviewer yang terkait.
- Standardisasi response menggunakan `ApiResponse<T>`.

### Out of Scope
- Create task baru, karena tetap menjadi kewenangan Admin melalui Task Management.
- Update metadata task seperti assignee, reviewer, deadline, dan title.
- Delete task.
- Upload binary file langsung; attachment tetap mengikuti mekanisme document existing.

## 3. Arsitektur Modul

Modul mengikuti pola N-Layer Flat Architecture yang sudah digunakan project:

- **DTOs**: Tambahkan DTO khusus MyTask bila response frontend berbeda dari `TaskResponse`.
- **Validators**: Validasi request `UpdateMyTaskStatusRequest` dan `AddMyTaskCommentRequest`.
- **Interfaces**: Tambahkan abstraksi `IMyTaskService`.
- **Services**: Implementasi `MyTaskService` berisi query ownership, transition rules, dan audit history.
- **Controllers**: Tambahkan `MyTaskController` dengan `[Authorize]`.
- **Models/Data**: Tetap menggunakan entity hasil scaffold existing (`TrxTask`, `TrxTaskHistory`, `TrxTaskComment`, `TrxTaskDocument`, `MstTaskStatus`, `MstUser`).

## 4. Kontrak DTO

### Request DTOs

```csharp
public sealed record MyTaskQueryParameters(
    int? StatusId,
    string? Search,
    string? SortBy);

public sealed record UpdateMyTaskStatusRequest(
    int TaskStatusId,
    string? Remarks);

public sealed record AddMyTaskCommentRequest(
    string CommentText);
```

### Response DTOs

Gunakan kembali `TaskResponse`, `TaskHistoryResponse`, `TaskCommentResponse`, dan `TaskDocumentResponse` bila struktur sudah cukup. Jika frontend membutuhkan format berbeda, buat adapter response berikut:

```csharp
public sealed record MyTaskSummaryResponse(
    int TaskId,
    string TaskTitle,
    string? TaskDescription,
    int AssigneeUserId,
    string AssigneeName,
    int ReviewerUserId,
    string ReviewerName,
    DateTime DeadlineDate,
    int TaskStatusId,
    string StatusName,
    bool IsOverdue,
    DateTime CreatedDate,
    DateTime? UpdatedDate);
```

## 5. Interface Service yang Dibutuhkan

```csharp
public interface IMyTaskService
{
    Task<IEnumerable<TaskResponse>> GetMyTasksAsync(
        int currentUserId,
        int? statusId,
        string? search,
        string? sortBy,
        CancellationToken cancellationToken);

    Task<TaskResponse> GetMyTaskByIdAsync(
        int currentUserId,
        int taskId,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdateMyTaskStatusAsync(
        int currentUserId,
        int taskId,
        UpdateMyTaskStatusRequest request,
        CancellationToken cancellationToken);

    Task<TaskCommentResponse> AddMyTaskCommentAsync(
        int currentUserId,
        int taskId,
        AddMyTaskCommentRequest request,
        CancellationToken cancellationToken);
}
```

## 6. Rules Akses Data

User hanya boleh mengakses task jika memenuhi salah satu kondisi:

```csharp
task.AssigneeUserId == currentUserId || task.ReviewerUserId == currentUserId
```

Rules tambahan:
- Task dengan `DeletedDate != null` tidak boleh dikembalikan.
- Jika task tidak ditemukan atau bukan milik user, return `404 Not Found` atau exception domain yang dipetakan oleh middleware.
- Role Admin tetap boleh memakai endpoint `/api/tasks`; endpoint `/api/my-tasks` harus selalu berorientasi pada user login.

## 7. Rules Transisi Status

Minimal flow yang disarankan:

| Current Status | Allowed Next Status | Actor |
| :------------- | :------------------ | :---- |
| Assigned       | On Progress         | Assignee |
| On Progress    | Need Review         | Assignee |
| Need Review    | Need Revision       | Reviewer |
| Need Review    | Cleared             | Reviewer |
| Need Revision  | On Progress         | Assignee |

Catatan:
- Mapping status menggunakan data `MstTaskStatus`, bukan hardcoded ID.
- Jika nama status existing berbeda, service wajib menggunakan normalisasi `StatusName`.
- Setiap perubahan status wajib membuat record di `TrxTaskHistory` dengan `ActionType = "STATUS_CHANGE"`.

## 8. Endpoint API

Semua endpoint menggunakan `[Authorize]` dan mengambil user dari JWT claim.

| Method | Endpoint | Deskripsi |
| :----- | :------- | :-------- |
| `GET` | `/api/my-tasks` | Mengambil daftar task milik user login dengan filter dan sort. |
| `GET` | `/api/my-tasks/{taskId}` | Mengambil detail task milik user login termasuk history, comments, documents. |
| `PUT` | `/api/my-tasks/{taskId}/status` | Update status task sesuai role user pada task tersebut. |
| `POST` | `/api/my-tasks/{taskId}/comments` | Menambahkan komentar pada task milik user login. |

## 9. Acceptance Criteria

- User tidak dapat melihat task milik user lain.
- User tidak dapat update status task yang bukan miliknya.
- Assignee hanya bisa menjalankan transisi status yang menjadi tanggung jawab assignee.
- Reviewer hanya bisa menjalankan transisi status yang menjadi tanggung jawab reviewer.
- Setiap update status tercatat pada `TrxTaskHistory`.
- Setiap komentar tercatat pada `TrxTaskComment`.
- Response API konsisten memakai `ApiResponse<T>`.
- `dotnet build` harus sukses tanpa error.

## 10. GitHub Issues Breakdown

### Issue 1 - [Backend][MyTask] Define DTOs, Validators, and Service Contract

Scope:
- Tambahkan DTO request/response untuk MyTask bila diperlukan.
- Tambahkan validator untuk update status dan add comment.
- Tambahkan interface `IMyTaskService`.
- Pastikan naming konsisten dengan modul existing.

Acceptance Criteria:
- DTO immutable menggunakan `record`.
- Validator menolak status id invalid dan komentar kosong.
- Interface siap diimplementasikan tanpa coupling ke controller.

### Issue 2 - [Backend][MyTask] Implement Secure MyTask Query and Detail Logic

Scope:
- Implementasi `MyTaskService.GetMyTasksAsync`.
- Implementasi `MyTaskService.GetMyTaskByIdAsync`.
- Terapkan ownership filter berdasarkan `AssigneeUserId` atau `ReviewerUserId`.
- Include history, comments, documents, user names, dan status names untuk detail.

Acceptance Criteria:
- User hanya menerima task miliknya.
- Filter `statusId`, `search`, dan `sortBy` berjalan.
- Detail task mengembalikan relasi pendukung yang dibutuhkan frontend.

### Issue 3 - [Backend][MyTask] Implement Status Transition Workflow and Audit Trail

Scope:
- Implementasi `UpdateMyTaskStatusAsync`.
- Validasi transisi status berdasarkan actor assignee/reviewer.
- Buat audit trail di `TrxTaskHistory`.
- Hindari hardcoded status ID; gunakan `MstTaskStatus.StatusName`.

Acceptance Criteria:
- Transisi invalid ditolak dengan error yang jelas.
- Update status valid menyimpan `UpdatedDate`, `UpdatedBy`, dan history.
- Task yang sudah soft-deleted tidak bisa diubah.

### Issue 4 - [Backend][MyTask] Implement MyTask Comment Capability

Scope:
- Implementasi `AddMyTaskCommentAsync`.
- Validasi user terkait task sebelum insert komentar.
- Simpan komentar ke `TrxTaskComment`.
- Return response berisi reviewer/user name dan timestamp.

Acceptance Criteria:
- User terkait task dapat menambahkan komentar.
- User yang tidak terkait task ditolak.
- Komentar kosong ditolak validator.

### Issue 5 - [Backend][MyTask] Expose MyTaskController Endpoints and DI Registration

Scope:
- Buat `MyTaskController` route `/api/my-tasks`.
- Tambahkan endpoint list, detail, update status, dan add comment.
- Daftarkan `IMyTaskService` pada `Program.cs`.
- Pastikan semua endpoint memakai `ApiResponse<T>`.

Acceptance Criteria:
- Endpoint dilindungi `[Authorize]`.
- User ID diambil dari JWT claim, bukan request body.
- `dotnet build` sukses tanpa error.

---
*Generated by Senior System Analyst - Implementation Document for BE.*