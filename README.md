# 🛒 Pearline – E-Commerce RESTful API

A production-grade E-Commerce backend built with **ASP.NET Core (.NET 8)**, designed with **Clean Architecture** to ensure scalability, maintainability, and separation of concerns.

---

## 🏗️ Architecture

This project follows **Clean Architecture** with 4 distinct layers:

```
Pearline.Domain          → Entities, Enums (no dependencies)
Pearline.Application     → DTOs, Interfaces, Services
Pearline.Infrastructure  → EF Core, Migrations, Identity, Helpers
Pearline.API             → Controllers, Program.cs
```

Dependency flow: `API → Application → Domain ← Infrastructure`

---

## 🚀 Features

### 🔐 Authentication & Authorization
- JWT Bearer authentication
- ASP.NET Identity with role management
- OTP-based forgot password flow via email
- Profile & account management

### 🛒 E-Commerce Core
- Product & category management with bulk JSON import
- Cart system with unit/case logic and auto price calculations
- Quote submission and item snapshot system
- User quote history

### 🛠️ Admin Panel
- Manage users & assign/revoke admin roles
- Review and update quote statuses
- Manage contact messages
- Full admin CRUD operations

### 📑 API Documentation
- Full Swagger (OpenAPI 3.0) documentation
- JWT authorization support in Swagger UI
- Clean endpoint grouping by feature

---

## 🧱 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 8) |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Auth | ASP.NET Identity + JWT |
| Mapping | AutoMapper |
| Docs | Swagger / Swashbuckle |
| Email | SMTP (Gmail) |

---

## 📂 Project Structure

```
Pearline-Ecommerce-API/
├── Pearline.API/
│   ├── Controllers/
│   │   ├── Admin/
│   │   ├── Auth/
│   │   ├── Cart/
│   │   ├── Contact/
│   │   ├── Products/
│   │   └── Profile/
│   └── Program.cs
├── Pearline.Application/
│   ├── DTOs/
│   │   ├── Admin/
│   │   ├── Auth/
│   │   ├── Cart/
│   │   ├── Quote/
│   │   └── User/
│   ├── Interfaces/
│   └── Services/
├── Pearline.Domain/
│   ├── Entities/
│   └── Enums/
└── Pearline.Infrastructure/
    ├── Data/
    ├── Identity/
    ├── Helpers/
    └── Migrations/
```

---

## ▶️ How to Run

### 1. Clone the repository
```bash
git clone https://github.com/mohamedfaresss/Pearline-Ecommerce-API
cd Pearline-Ecommerce-API
```

### 2. Configure settings
Copy `appsettings.json` and fill in your values:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY",
    "Issuer": "http://localhost:7225/",
    "Audience": "http://localhost:4200/"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "YOUR_EMAIL",
    "Password": "YOUR_APP_PASSWORD"
  }
}
```

### 3. Apply migrations
```bash
dotnet ef database update --project Pearline.Infrastructure --startup-project Pearline.API
```

### 4. Run the API
```bash
dotnet run --project Pearline.API
```

Swagger UI: `https://localhost:7225/swagger`

---

## 📘 API Summary

| Group | Endpoints |
|---|---|
| Auth | Register, Login, Forgot Password, OTP, Reset Password |
| Products | List, Filter, CRUD, Bulk Import |
| Cart | Add, Remove, Clear, Auto Pricing |
| Quote | Submit, History, Admin Review |
| Profile | View, Update, Change Email/Password |
| Admin | Users, Roles, Quotes, Messages |
| Contact | Send Message, Admin View |

---

## 👤 Developer

**Mohamed Gamal** – Backend .NET Developer  
[GitHub](https://github.com/mohamedfaresss)
