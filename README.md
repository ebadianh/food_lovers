# Food Lovers API

A lightweight **.NET Minimal API** with **MySQL**, using raw SQL (no Entity Framework). Provides:

- User CRUD
- Session-based login/logout
- Basic booking data retrieval

---

## 🔧 Setup

### 1. Create database + user

```sql
CREATE DATABASE foodlovers;

CREATE USER 'foodlovers'@'localhost' IDENTIFIED BY 'foodlovers';

GRANT ALL PRIVILEGES ON foodlovers.* TO 'foodlovers'@'localhost';
FLUSH PRIVILEGES;

### 2. Build commands

dotnet build 
dotnet run