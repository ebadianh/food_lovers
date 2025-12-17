# Foodlovers Backend System

Foodlovers is a backend system built in C# .NET with MySQL. The application allows users to
register, log in, and book travel packages that include destinations, hotels, and rooms.
Administrators can manage users, hotels, facilities, and packages.
Bookings are stored with detailed information about stops, rooms, and costs, and the system automatically calculates the total price using a database view.
Below you’ll find database structure and API endpoints.

## Overview
Foodlovers is a C# .NET project that uses:
- MySQL as the database
- Swagger for API documentation and testing
- Postman for manual testing
- Sessions for authentication (users/admins)

The system manages users, bookings, hotels, packages and facilities in a travel and food platform.

---

## Technical Architecture
-- Language: C#
-- Framwork: .NET Minimal API
-- Database: MySQL
-- Testing: Swagger UI & Postman
-- Authentication: Session-based login/logout
-- Database Reset: Endpoint to restore the database to seed data

## Database Structure
-- users, admins - users and administrators
-- countries, destinations - countries and cities
-- trip_packages, stops - travel packages and stops
-- hotels, rooms, room_types - hotels and room types
-- facilities, accommodation_facilities - facilities
-- bookings, booking_stops, booked_rooms - bookings and related data
-- poi_distances, hotel_poi_distances - distance to points of interest

### Views
- receipt - compiles the total cost of a booking

---

## Authentication
The system uses sessions:
- User login -> session stores 'user_id'
- Admin login -> session stores 'admin_id'
- Endpoints check session before access:
    - Admin-only: requires 'admin_id'
    - User-only: requires 'user_id'

---

## API Endpoints

### Login
- 'GET /login' - check if users is logged in
- 'POST /login - log in user
- 'POST /login/admin' - log in admin
- 'DELETE /login' - log out

### Users
- 'GET /users' - get all users (admin)
- 'GET /users{id}' - get user by ID
- 'POST /user' - create new user
- 'PUT /users/{id}' - update user (admin)
- 'DELETE /users/{id}' - delete user

### Bookings
- 'GET /bookings' – get all bookings (admin)
- 'POST /bookings' – create new booking (user)
- 'DELETE /bookings/{id}' – delete booking (owner only)
- 'GET /bookings/user' – get all packages for a user
- 'GET /bookings/{id}/totalcost' – calculate total cost of a booking
- 'PUT /bookings/{id}' – update booking (owner only)
- 'GET /bookings/{id}/details' – detailed view of a booking

### Searchings
- 'GET /packages' - get packages with filters
- 'GET /searchings/SuggestedCountry' - recommended packages based on country
- 'GET /searchings/customizedPackage' - customize package
- 'GET /hotels' - filter hotels
- 'GET /admin/hotels' - admin view of hotels
- 'GET /admin/trips' - admin view of packages
- 'GET /admin/facilities' - admin view of facilities

### Special
- 'DELETE /db' - reset database to default seeds

---

## Testing
- Swagger UI: available at '/swagger'
- Postman: used to simulate requests with different parameters
- DB Reset - '/db' resets the database to seeds -> important for test environments

---

## Important Notes
- Sessions are used for authentication -> ensure cookies are handled correctly in Postman
- Admin endpoints require login via '/login/admin'
- The database can be reset via '/db' to restore seed data

---

## Summary
The system is designed to:
- Handle authentication
- Provide CRUD for users, hotels, packages and facilities
- Enable bookings with stops and rooms
- Provide cost calculation via a view
- Be testable and documented via Swagger and Postman
