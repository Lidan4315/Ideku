# ⚙️ Petunjuk Setup Proyek

Dokumen ini menjelaskan langkah-langkah yang diperlukan untuk mengkonfigurasi dan menjalankan proyek ini di lingkungan development Anda.

---

## Langkah-langkah Konfigurasi

Ikuti langkah-langkah di bawah ini secara berurutan.

### 1. Buat File `appsettings.Development.json`

Di dalam direktori utama proyek, buat sebuah file baru dan beri nama `appsettings.Development.json`. File ini akan berisi semua konfigurasi khusus untuk lingkungan development, seperti koneksi database dan pengaturan email.

### 2. Isi File Konfigurasi

Salin dan tempel (copy-paste) kode JSON di bawah ini ke dalam file `appsettings.Development.json` yang baru saja Anda buat.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server={YourServerName};Database={YourDatabaseName};Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "{your-email}",
    "SenderPassword": "{your-app-password}",
    "SenderName": "Ideku System",
    "BaseUrl": "http://localhost:5035"
  },
  "ApprovalTokenSettings": {
    "EncryptionKey": "{your enctyption code}"
  },
  "FileUploadSettings": {
    "MaxFileSizeMB": {fill as needed},
    "MaxTotalFileSizeMB": {fill as needed},
    "AllowedExtensions": [
      ".pdf",
      ".doc",
      ".docx",
      ".xls",
      ".xlsx",
      ".ppt",
      ".pptx",
      ".jpg",
      ".jpeg",
      ".png"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
````
**Penting:** Anda perlu mengubah beberapa nilai agar sesuai dengan pengaturan lokal Anda:

- `YourServerName`: Ganti dengan nama server SQL Server Anda (contoh: localhost, (localdb)\mssqllocaldb, atau .\SQLEXPRESS).
- `YourDatabaseName`: Ganti dengan nama database yang ingin Anda gunakan untuk proyek ini.
- `your-email@gmail.com`: Ganti dengan alamat email valid Anda yang akan digunakan untuk mengirim email dari aplikasi.
- `your-app-password`: Ganti dengan App Password yang di-generate dari akun Google Anda, bukan kata sandi login email biasa.

### 3. Migrasi

Lakukan migrasi dengan menjalankan perintah di bawah ini:

````
dotnet ef database update
````

### 4. Jalankan Program

Untuk menjalankan program sekaligus melakukan seeding data yang di perlukan anda bisa menjalankan perintah di bawah:

````
dotnet watch run
````

