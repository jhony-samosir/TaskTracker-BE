# Task Management Backend Implementation Plan

Dokumen ini mendefinisikan langkah-langkah implementasi, rancangan teknis, serta panduan pengerjaan untuk fitur **Task Management** pada sistem backend TaskTracker.

## 1. Arsitektur Modul Task Management

Modul Task Management akan mengikuti struktur N-Layer Flat Architecture dari proyek ini:
- **Data/Models**: Menggunakan entitas hasil *scaffolding* Entity Framework Core (`TrxTask`, `TrxTaskHistory`, `TrxTaskComment`, `TrxTaskDocument`, `MstTaskStatus`).
- **DTOs**: Data Transfer Objects khusus untuk modul tugas.
- **Validators**: FluentValidation rules untuk *request body*.
- **Interfaces**: Abstraksi `ITaskService`.
- **Services**: Kelas `TaskService` berisi *business logic*, filter pencarian, paginasi, pencatatan histori (audit trail), dan update status.
- **Controllers**: `TaskController` mengekspos REST API dan melindungi endpoints dengan `[Authorize]` serta memfilter data berdasar *Roles*.

## 2. Definisi DTO (Data Transfer Objects)

Berikut adalah *request/response* objects yang perlu dibuat di dalam folder `DTOs/TaskDtos.cs`:

### Request DTOs
- `CreateTaskRequest`: `TaskTitle`, `TaskDescription`, `AssigneeUserId`, `ReviewerUserId`, `DeadlineDate`, `TaskStatusId`
- `UpdateTaskRequest`: `TaskTitle`, `TaskDescription`, `AssigneeUserId`, `ReviewerUserId`, `DeadlineDate`, `TaskStatusId`
- `UpdateTaskStatusRequest`: `TaskStatusId`, `Remarks`
- `AddCommentRequest`: `CommentText`

### Response DTOs
- `TaskResponse`: Representasi lengkap data tugas beserta relasi tabel referensinya (Assignee Name, Reviewer Name, Status Name).
- `TaskHistoryResponse`: Untuk log aktivitas task.
- `TaskCommentResponse`: Untuk riwayat diskusi / komentar.

## 3. Daftar Service yang akan Diimplementasi (`ITaskService`)

```csharp
public interface ITaskService
{
    // Mengambil daftar tugas (Querying)
    Task<IEnumerable<TaskResponse>> GetTasksAsync(int? assigneeId, int? statusId, string? search, CancellationToken cancellationToken);
    Task<TaskResponse> GetTaskByIdAsync(int taskId, CancellationToken cancellationToken);

    // Mutasi Data Task
    Task<TaskResponse> CreateTaskAsync(int currentUserId, CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskResponse> UpdateTaskAsync(int currentUserId, int taskId, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteTaskAsync(int currentUserId, int taskId, CancellationToken cancellationToken);

    // Status & Histori
    Task<bool> UpdateTaskStatusAsync(int currentUserId, int taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);
    
    // Komentar
    Task<TaskCommentResponse> AddCommentAsync(int currentUserId, int taskId, AddCommentRequest request, CancellationToken cancellationToken);
}
```

## 4. Endpoints yang Harus Dibuat pada `TaskController`

Semua endpoints terbungkus dalam model standard aplikasi (`ApiResponse<T>`):

| Method | Endpoint                        | Auth/Role            | Deskripsi                                                        |
| :----- | :------------------------------ | :------------------- | :--------------------------------------------------------------- |
| `GET`  | `/api/tasks`                    | Admin / Employee     | Mendapatkan daftar tugas, mendukung filter dan *search*.           |
| `GET`  | `/api/tasks/{taskId}`           | Admin / Employee     | Detail tugas spesifik termasuk histori dan komentar.               |
| `POST` | `/api/tasks`                    | Admin                | Membuat tugas baru.                                                |
| `PUT`  | `/api/tasks/{taskId}`           | Admin                | Memperbarui rincian tugas.                                         |
| `PUT`  | `/api/tasks/{taskId}/status`    | Admin / Employee     | Memperbarui status tugas sesuai dengan alur sistem.                |
| `POST` | `/api/tasks/{taskId}/comments`  | Admin / Employee     | Menambahkan komentar/feedback pada sebuah tugas.                   |
| `DELETE`|`/api/tasks/{taskId}`           | Admin                | Melakukan *soft-delete* pada tugas.                                |

## 5. Rencana Pekerjaan (GitHub Issues Breakdown)

Implementasi akan dibagi menjadi beberapa *Issue* di GitHub agar pengerjaan bisa dilakukan secara modular dan bertahap:

1. **[Backend] Issue 1: Define Task Management DTOs & Validation Rules**
   - Buat file `DTOs/TaskDtos.cs`.
   - Buat *record class* untuk Create/Update/Status Requests dan Task Responses.
   - Buat *FluentValidation* rules pada folder `Validators/TaskValidators.cs` untuk memastikan field (title, assignee, status, dll) ter-validasi dengan baik sebelum diproses.

2. **[Backend] Issue 2: Implement ITaskService and Setup Query Logic**
   - Buat *interface* `ITaskService`.
   - Implementasikan *read operations* (`GetTasksAsync` & `GetTaskByIdAsync`) pada `TaskService`.
   - Pastikan *Employee* hanya dapat melihat tugasnya sendiri, sedangkan *Admin* bisa melihat semua.

3. **[Backend] Issue 3: Implement Task Lifecycle & Status Updates (Mutations)**
   - Implementasikan operasi CRUD (`CreateTaskAsync`, `UpdateTaskAsync`, `DeleteTaskAsync`).
   - Implementasikan `UpdateTaskStatusAsync` disertai validasi transisi status (misal: tidak bisa mengubah *Cleared* kembali menjadi *Need Review* secara asal).
   - Pastikan setiap *create* dan *status update* mencatat *audit trail* ke dalam tabel `TrxTaskHistory`.

4. **[Backend] Issue 4: Implement Comment & Document Attachments Logic**
   - Implementasikan fitur untuk menambahkan komentar ke `TrxTaskComment`.
   - Implementasikan relasi *History* dan *Comments* saat query detail.
   
5. **[Backend] Issue 5: Implement TaskController Endpoints**
   - Buat `TaskController`.
   - Eksekusi hasil implementasi service.
   - Set *Role-Based Authorization* (Misal: hanya role tertentu yang bisa POST `/api/tasks`).
   - Sediakan dan format respons dengan `ApiResponse<T>`.

---
*Generated by Senior System Analyst - Implementation Document for BE.*