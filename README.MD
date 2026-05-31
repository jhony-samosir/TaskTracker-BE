# TaskTracker Auth & API System

TaskTracker adalah sebuah sistem manajemen tugas berbasis Web API dengan .NET 10. Repositori ini berisi implementasi sistem Autentikasi dan Otorisasi yang aman, modular, dan mengikuti best-practice arsitektur modern menggunakan PostgreSQL sebagai database engine dan Entity Framework Core sebagai ORM.

## Arsitektur & Teknologi

- **Framework**: .NET 10 Web API
- **ORM**: Entity Framework Core 10
- **Database**: PostgreSQL (Npgsql)
- **Autentikasi**: JWT Bearer Authentication (Access Token & Refresh Token)
- **Otorisasi**: Role-Based Authorization
- **Security**: ASP.NET Core `PasswordHasher` untuk enkripsi password (argon2id/PBKDF2 secara internal)
- **Validation**: Request Validation menggunakan `FluentValidation`
- **Arsitektur**: N-Layer Flat Architecture (Controllers → Services → Data/Models), tanpa redundansi Repository pattern.

---

## Struktur Folder Project

Struktur project disusun dengan struktur yang datar (flat) selevel dengan controller agar memudahkan navigasi tanpa _over-engineering_ layer:

```
TaskTracker.API/
│
├── Controllers/                  # API Endpoint Controllers (e.g., AuthController)
├── Services/                     # Business Logic & JWT Token Logic (AuthService, JwtTokenService)
├── Interfaces/                   # Abstraksi dan Kontrak Service (IAuthService, IJwtTokenService)
├── DTOs/                         # Request & Response Data Transfer Objects (AuthDtos)
├── Validators/                   # FluentValidation rules (AuthValidators)
├── Models/                       # Database Entities (MstUser, RefreshToken, dll.)
├── Data/                         # EF Core DbContext (AppDbContext)
├── Middleware/                   # Custom exception handling & validation errors handler
└── InfrastructureConfigurations/ # Konfigurasi Infrastruktur (JwtOptions)
```

---

## API Endpoints (Authentication)

Semua endpoint menghasilkan respons terstandarisasi dengan bentuk:

```json
{
  "succeeded": true,
  "message": "Success message",
  "data": { ... },
  "errors": null
}
```

| Method | Endpoint                    | Auth       | Deskripsi                                                              |
| :----- | :-------------------------- | :--------- | :--------------------------------------------------------------------- |
| `POST` | `/api/auth/register`        | Anonymous  | Mendaftarkan user baru (Otomatis mendapatkan default Role)             |
| `POST` | `/api/auth/login`           | Anonymous  | Login dengan email & password untuk mendapatkan Access & Refresh Token |
| `POST` | `/api/auth/refresh-token`   | Anonymous  | Memperbarui Access Token yang expired menggunakan Refresh Token aktif  |
| `POST` | `/api/auth/logout`          | Authorized | Mencabut hak akses (revoke) refresh token user saat ini                |
| `GET`  | `/api/auth/me`              | Authorized | Mengambil data profile pengguna yang saat ini sedang login             |
| `POST` | `/api/auth/change-password` | Authorized | Mengganti password pengguna yang sedang aktif                          |

---

## Petunjuk Penggunaan

### Run secara Lokal (Development)

1. Pastikan PostgreSQL local Anda berjalan dan database `tasktracker` sudah terbuat.
2. Jalankan query SQL di atas pada database Anda.
3. Edit target koneksi dan JWT Key di `appsettings.Development.json` sesuai dengan environment Anda.
4. Jalankan aplikasi menggunakan CLI:
   ```bash
   dotnet run --project TaskTracker.API
   ```
5. Akses Swagger/OpenAPI UI di browser melalui `http://localhost:5133/openapi/v1.json` atau URL HTTPS yang tertera pada output terminal.

### Run menggunakan Docker

Aplikasi ini sudah mendukung Docker container yang dijalankan dengan mode `Development` agar membaca konfigurasi local secara konsisten.

1. Build image Docker:
   ```bash
   docker build -t tasktracker-api -f TaskTracker.API/Dockerfile .
   ```
2. Jalankan Container:
   ```bash
   docker run -d -p 8080:8080 -p 8081:8081 --name tasktracker-app tasktracker-api
   ```
