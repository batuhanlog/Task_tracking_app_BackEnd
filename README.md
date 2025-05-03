![Ekran görüntüsü 2025-05-03 084533](https://github.com/user-attachments/assets/8c075a4f-abb3-4d3c-a0ad-8777ca1e57dd)
![Ekran görüntüsü 2025-05-03 085718](https://github.com/user-attachments/assets/4f9b1559-50cc-40e8-8fc6-1bc8d42f193e)
![Ekran görüntüsü 2025-05-03 085708](https://github.com/user-attachments/assets/e8bb361b-bc51-493e-bf45-6c901c10a35a)
![Ekran görüntüsü 2025-05-03 085648](https://github.com/user-attachments/assets/9dea45d5-0aab-43bb-98a9-a91cecef1142)
![Ekran görüntüsü 2025-05-03 085638](https://github.com/user-attachments/assets/52d02847-bcc8-49d9-bf52-e22114ef068f)
![Ekran görüntüsü 2025-05-03 085612](https://github.com/user-attachments/assets/fc22c069-47bc-43a1-bad3-aa5fee598779)
# 🛠️ Task Tracking App - Backend API ⚙️

🔌 Bu repository, **Task Tracking App** uygulamasının backend (arka uç) API kodlarını içerir. .NET 7 ve ASP.NET Core kullanılarak geliştirilen bu API, uygulamanın iş mantığını yürütmek, veritabanı işlemlerini yönetmek (Entity Framework Core ile) ve frontend uygulamasına veri sağlamaktan sorumludur.

---

## ✨ Temel Teknolojiler & Kütüphaneler

Bu projenin backend'i aşağıdaki teknolojiler ve kütüphaneler üzerine kurulmuştur:

*   **🚀 .NET 7:** Uygulamanın çalıştığı ana platform.
*   **🌐 ASP.NET Core:** Web API oluşturmak için kullanılan framework.
*   **💾 Entity Framework Core:** Veritabanı işlemleri için Object-Relational Mapper (ORM).
*   **🐘 PostgreSQL:** Veritabanı yönetim sistemi (EF Core ile kullanıldı).
*   **🗺️ AutoMapper:** Nesneler arası DTO (Data Transfer Object) dönüşümlerini kolaylaştırmak için.
*   **🔑 JWT (JSON Web Tokens):** API güvenliği ve kimlik doğrulama için (ASP.NET Core Identity ile birlikte veya bağımsız).
*   **📄 Swagger (OpenAPI):** API endpoint'lerini belgelemek ve test etmek için.

---

## 📋 Gereksinimler

Projeyi yerel makinenizde çalıştırmadan önce aşağıdaki araçların kurulu olduğundan emin olun:

*   **.NET 7 SDK:** [https://dotnet.microsoft.com/download/dotnet/7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
*   **PostgreSQL:** [https://www.postgresql.org/download/](https://www.postgresql.org/download/) (ve pgAdmin gibi bir yönetim aracı önerilir).

---

## ⚙️ Kurulum ve Çalıştırma

1.  **Repository'yi Klonlayın:**
    ```bash
    git clone https://github.com/batuhanlog/Task_tracking_app_BackEnd.git
    cd Task_tracking_app_BackEnd
    ```

2.  **Bağımlılıkları Yükleyin:**
    ```bash
    dotnet restore TaskTitan.Api # Proje dosyasının olduğu klasördeyse sadece dotnet restore yeterli olabilir
    ```

3.  **Veritabanı Bağlantısını Yapılandırın (ÇOK ÖNEMLİ):**
    *   **Yöntem 1: User Secrets (Tavsiye Edilen):**
        *   `TaskTitan.Api` klasöründeyken terminalde aşağıdaki komutu çalıştırın. Değerleri kendi PostgreSQL kurulumunuza göre değiştirin:
        ```bash
        dotnet user-secrets init # Daha önce yapılmadıysa
        dotnet user-secrets set "ConnectionStrings:TaskTitanDb" "Server=localhost;Port=5432;Database=TaskTitanDb;User Id=postgres;Password=SENIN_POSTGRES_SIFREN;"
        ```
    *   **Yöntem 2: `appsettings.Development.json`:**
        *   `TaskTitan.Api` klasörü içinde `appsettings.Development.json` adında bir dosya oluşturun.
        *   Aşağıdaki içeriği kendi bilgilerinizle doldurup içine yapıştırın:
        ```json
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "ConnectionStrings": {
            "TaskTitanDb": "Server=localhost;Port=5432;Database=TaskTitanDb;User Id=postgres;Password=SENIN_POSTGRES_SIFREN;"
          }
          // Gerekliyse diğer geliştirme ayarları...
        }
        ```
        *(Not: `appsettings.Development.json` dosyası `.gitignore` içinde olmalıdır!)*

4.  **Veritabanını Oluşturun ve Başlangıç Verilerini Yükleyin:**
    *   `TaskTitan.Api` klasöründeyken terminalde aşağıdaki komutu çalıştırın:
    ```bash
    dotnet ef database update
    ```
    *   Bu komut, veritabanını oluşturacak (eğer yoksa), tabloları EF Core Migrations'a göre yapılandıracak ve `DbContext`'teki `HasData` ile tanımlanan başlangıç verilerini (müşteriler, kullanıcılar, projeler vb.) ekleyecektir.

5.  **Uygulamayı Çalıştırın:**
    *   Terminal üzerinden:
        ```bash
        dotnet run --project TaskTitan.Api
        ```
    *   Veya Visual Studio / Rider gibi bir IDE üzerinden projeyi başlatın.

6.  **API'ye Erişin:**
    *   Uygulama varsayılan olarak `https://localhost:7141` (veya farklı bir port) adresinde çalışacaktır.
    *   API belgelerine ve test arayüzüne genellikle `https://localhost:7141/swagger` adresinden erişebilirsiniz.

---

**(Gerekirse lisans, katkıda bulunma yönergeleri gibi ek bölümler eklenebilir.)**
