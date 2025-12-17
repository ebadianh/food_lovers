Dokumentation Foodlovers 

 

1. Översikt 

Systemet är en C# .NET Web API-applikation som använder: 
- MySQL som databas 
- Postman för testning av API-endpoints 
- Swagger för dokumentation och interaktiv testning 
- Sessions för autentisering (login/logout för användare och admin) 
- Systemet är byggt för att hantera resor, hotell, bokningar och användare i en mat- och reseplattform. 
- Arkitekturen är modulär: varje resurs (Users, Bookings, Searchings, Login) har sin egen klass med CRUD-metoder. 


2. Program.cs

Funktion:

- Konfigurerar databaskoppling (Config) 
- Aktiverar sessioner och cache 
- Registrerar Swagger 
- Definierar alla REST-endpoints 
- Viktiga endpoints 
- Login: /login, /login/admin, /logout 
- Users: /users, /users/{id} 
- Bookings: /bookings, /bookings/{id}, /bookings/user 
- Searchings: /packages, /hotels, /admin/trips, /admin/facilities 
- Reset DB: /db (återställer databasen till default med seeds) 


3. Databasstruktur 

Tabeller:
- users, admins – användare och administratörer 
- countries, destinations – länder och städer 
- trip_packages, stops – resepaket och stopp 
- hotels, rooms, room_types – hotell och rumstyper 
- facilities, accommodation_facilities – faciliteter
- bookings, booking_stops, booked_rooms – bokningar och kopplingar 
- poi_distances, hotel_poi_distances – avstånd till sevärdheter 

Views:
- receipt – sammanställer kostnad för en bokning (paket + rum) 

Seeds: 
- Exempeldata för användare, admins, länder, destinationer, hotell, faciliteter och en testbokning. 


4. Login.cs 

Hantera autentisering via sessions: 
- Login (User): validerar email + lösenord mot users
- Login (Admin): validerar mot admins 
- Get: kontrollerar om användare är inloggad 
- GetAdmin: kontrollerar om admin är inloggad 
- Delete: loggar ut (rensar session) 


5. Users.cs 

CRUD för användare: 
- GET: hämta alla användare (endast admin) 
- GetById: hämta användare via ID 
- POST: skapa ny användare 
- PUT: uppdatera användare (endast admin) 
- DELETE: ta bort användare 


6. Searchings.cs 

Hantera sökningar och filtrering: 
- GetPackages: hämta paket med filter (land, stad, pris, stjärnor) 
- GetSuggestedByCountry: rekommenderar paket baserat på land och matkultur 
- GetCustomizedPackage: skräddarsy paket med valda destinationer och hotell 
- GetFilters: filtrera hotell baserat på land, datum, faciliteter, stjärnor 
- AdminView: admin kan se hotell, trips och faciliteter 
- GetHotelByID / GetAllTripsByID / GetFacilityByID: detaljerad vy för admin 


7. Bookings.cs 

Hantera bokningar: 
- GetAll: hämta alla bokningar (endast admin) 
- Post: skapa ny bokning (måste vara inloggad som användare) 
- Delete: ta bort bokning (endast ägaren) 
- GetAllPackagesForUser: hämta alla paket bokade av en användare 
- GetTotalCostByBooking: beräkna total kostnad via receipt-view 
- Put: uppdatera bokning (endast ägaren) 
- GetDetails: detaljerad vy över en bokning (paket, land, stad, hotell) 


8. Säkerhet & Sessions 

- Users loggar in via /login → session lagrar user_id 
- Admins loggar in via /login/admin → session lagrar admin_id 
- Endpoints kontrollerar session innan åtkomst:  
- Admin-only: kräver admin_id 
- User-only: kräver user_id 


9. Testning 

- Swagger: körs via /swagger för att testa endpoints 
- Postman: används för att simulera requests med olika parametrar
- DB Reset: /db återställer databasen till seeds → viktigt för testmiljö 


10. Sammanfattning 

Systemet är uppbyggt för att: 
- Hantera autentisering (users/admins) 
- Tillhandahålla CRUD för användare, hotell, paket och faciliteter 
- Möjliggöra bokningar med stopp och rum 
- Ge kostnadsberäkning via en view 
- Vara testbart och dokumenterat via Swagger och Postman 

 