# Food Lovers API

A lightweight **.NET Minimal API** with **MySQL**, using raw SQL (no Entity Framework). Provides:

- User CRUD
- Session-based login/logout
- Basic booking data retrieval

---

## 🔧 Setup

### 1. Create database + user

```sql
CREATE DATABASE food_lovers;

DROP USER IF EXISTS 'api_user'@'localhost';
CREATE USER 'api_user'@'localhost' IDENTIFIED BY '123!';

GRANT ALL PRIVILEGES ON food_lovers.* TO 'api_user'@'localhost';
FLUSH PRIVILEGES;

### 2. Build commands

dotnet build <br>
dotnet run