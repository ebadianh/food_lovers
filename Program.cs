using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using server;

var builder = WebApplication.CreateBuilder(args);
// 127.0.0.1:3306 (default port)

Config config = new(
    "server=127.0.0.1;uid=foodlovers;pwd=foodlovers;database=foodlovers"
);


builder.Services.AddSingleton<Config>(config);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// swagger added
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Use full type name (including declaring type) so GetAll_Data in Bookings/Searchings don't clash
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

var app = builder.Build();
app.UseSession();

app.UseSwagger();
app.UseSwaggerUI();

// REST routes
// session / login / logout examples (auth resource)
app.MapGet("/login", Login.Get);
app.MapPost("/login", Login.Post);
app.MapPost("/login/admin", Login.PostAdmin);
app.MapDelete("/login", Login.Delete);

// CRUD examples (user resource)
app.MapGet("/users", Users.Get); // get all users
app.MapGet("/users/{id}", Users.GetById); // get user by id
app.MapPost("/users", Users.Post); // create new user
app.MapPut("/users/{id}", Users.Put); // update user
app.MapDelete("/users/{id}", Users.Delete); // delete user

// CRUD methods for bookings
app.MapGet("/bookings", Bookings.GetAll);
app.MapGet("/bookings/{bookingsId}/totalcost", Bookings.GetTotalCostByBooking);
app.MapPost("/bookings", Bookings.Post);
app.MapDelete("/bookings/{id:int}", Bookings.Delete);
app.MapGet("/bookings/user", Bookings.GetAllPackagesForUser); // get all packages booked by a user




// CRUD Methods for packages
app.MapGet("/searchings/SuggestedCountry", Searchings.GetSuggestedByCountry);
app.MapPost("/searchings/customizedPackage", Searchings.GetCustomizedPackage);





app.MapGet("/packages", Searchings.GetPackages); // get all packages with optional filters
//  GET http://localhost:5240/packages?country=Italy
//  GET http://localhost:5240/packages?maxPrice=1000
//  GET http://localhost:5240/packages?search=street food
//  GET http://localhost:5240/packages?country=France&minStars=4&maxPrice=1500

app.MapGet("/hotels", Searchings.GetFilters);
// GET /hotels?country=Italy&minStars=4
// GET /hotels?country=Italy&checkin=2025-07-01T15:00:00&checkout=2025-07-08T10:00:00&total_travelers=2
// GET /hotels?city=Rome&facilities=Pool,Spa
// GET /hotels?country=Italy&checkin=2025-07-01T15:00:00&checkout=2025-07-08T10:00:00&total_travelers=2&city=Rome&minStars=4




// special, reset db
app.MapDelete("/db", db_reset_to_default);

app.Run();


async Task db_reset_to_default(Config config)
{
    string tables = """
        SET FOREIGN_KEY_CHECKS = 0; -- turns of foreign key checks. allows us to drop tables in any order while ignoring constraints

        -- db tables dropped before inserted
        DROP TABLE IF EXISTS booked_rooms;
        DROP TABLE IF EXISTS bookings;
        DROP TABLE IF EXISTS accommodation_facilities;
        DROP TABLE IF EXISTS rooms;
        DROP TABLE IF EXISTS hotel_poi_distances;
        DROP TABLE IF EXISTS poi_distances;
        DROP TABLE IF EXISTS hotels;
        DROP TABLE IF EXISTS package_itineraries;
        DROP TABLE IF EXISTS trip_packages;
        DROP TABLE IF EXISTS destinations;
        DROP TABLE IF EXISTS facilities;
        DROP TABLE IF EXISTS countries;
        DROP TABLE IF EXISTS users;
        DROP TABLE IF EXISTS admins;
        DROP TABLE IF EXISTS room_types;


        SET FOREIGN_KEY_CHECKS = 1; -- control for database foreign key constraints. example: cant drop a parent table if a child table references it. = 1 enables it

        -- TABLES CREATED HERE

        -- USERS table
        CREATE TABLE users (
            id INT PRIMARY KEY AUTO_INCREMENT,
            first_name VARCHAR(256) NOT NULL,
            last_name VARCHAR(256) NOT NULL,
            email VARCHAR(256) NOT NULL UNIQUE,
            password VARCHAR(256) NOT NULL,
            CONSTRAINT chk_email_format CHECK (email LIKE '%_@_%._%')
        );

        -- ADMINS table
        CREATE TABLE admins (
            id INT PRIMARY KEY AUTO_INCREMENT,
            email VARCHAR(256) NOT NULL UNIQUE,
            password VARCHAR(256) NOT NULL,
            CONSTRAINT chk_a_email_format CHECK (email LIKE '%_@_%._%')
        );

        -- COUNTRIES table
        CREATE TABLE countries (
            id INT PRIMARY KEY AUTO_INCREMENT,
            name VARCHAR(100) NOT NULL,
            cuisine VARCHAR(100)
        );

        -- DESTINATIONS table
        CREATE TABLE destinations (
            id INT PRIMARY KEY AUTO_INCREMENT,
            country_id INT NOT NULL,
            city VARCHAR(100) NOT NULL,
            description TEXT,
            FOREIGN KEY (country_id) REFERENCES countries(id)
        );

        -- TRIP_PACKAGES table
        CREATE TABLE trip_packages (
            id INT PRIMARY KEY AUTO_INCREMENT,
            name VARCHAR(150) NOT NULL,
            description TEXT,
            price_per_person DECIMAL(10, 2) NOT NULL
        );

        -- PACKAGE_ITINERARIES table (junction table)
        CREATE TABLE package_itineraries (
            package_id INT NOT NULL,
            destination_id INT NOT NULL,
            stop_order INT NOT NULL,
            nights TINYINT NOT NULL,
            PRIMARY KEY (package_id, stop_order),
            FOREIGN KEY (package_id) REFERENCES trip_packages(id) ON DELETE CASCADE,
            FOREIGN KEY (destination_id) REFERENCES destinations(id) ON DELETE CASCADE
        );


        -- HOTELS table
        CREATE TABLE hotels (
            id INT PRIMARY KEY AUTO_INCREMENT,
            destination_id INT NOT NULL,
            name VARCHAR(150) NOT NULL,
            description TEXT,
            stars TINYINT CHECK (stars BETWEEN 1 AND 5),
            distance_to_center DECIMAL(5, 2),
            FOREIGN KEY (destination_id) REFERENCES destinations(id) ON DELETE CASCADE
        );

        -- POI_DISTANCES table
        CREATE TABLE poi_distances (
            id INT PRIMARY KEY AUTO_INCREMENT,
            name VARCHAR(150) NOT NULL
        );

        -- HOTEL_POI_DISTANCES table (junction table)
        CREATE TABLE hotel_poi_distances (
            id INT PRIMARY KEY AUTO_INCREMENT,
            hotel_id INT NOT NULL,
            poi_distance_id INT NOT NULL,
            distance DECIMAL(5, 2),
            FOREIGN KEY (hotel_id) REFERENCES hotels(id) ON DELETE CASCADE,
            FOREIGN KEY (poi_distance_id) REFERENCES poi_distances(id) ON DELETE CASCADE
        );
       
        -- ROOM_TYPE table (lookup table for room types)
        CREATE TABLE room_types (
        id INT PRIMARY KEY AUTO_INCREMENT,
        type_name VARCHAR(50) NOT NULL UNIQUE,
        capacity INT NOT NULL,  -- Fixed capacity
        CHECK (capacity > 0)
        );

        -- ROOMS table (each room type belongs to a hotel)
        CREATE TABLE rooms (
        hotel_id INT NOT NULL,
        room_number INT NOT NULL,
        roomtype_id INT NOT NULL,
        PRIMARY KEY (hotel_id, room_number),
        FOREIGN KEY (hotel_id) REFERENCES hotels(id) ON DELETE CASCADE,
        FOREIGN KEY (roomtype_id) REFERENCES room_types(id)
        );



        -- FACILITIES table
        CREATE TABLE facilities (
            id INT PRIMARY KEY AUTO_INCREMENT,
            name VARCHAR(100) NOT NULL
        );

        -- ACCOMMODATION_FACILITIES table (junction table)
        CREATE TABLE accommodation_facilities (
            hotel_id INT NOT NULL,
            facility_id INT NOT NULL,
            PRIMARY KEY (hotel_id, facility_id),
            FOREIGN KEY (hotel_id) REFERENCES hotels(id) ON DELETE CASCADE,
            FOREIGN KEY (facility_id) REFERENCES facilities(id) ON DELETE CASCADE
        );

        -- BOOKINGS table
        CREATE TABLE bookings (
            id INT PRIMARY KEY AUTO_INCREMENT,
            user_id INT NOT NULL,
            package_id INT NOT NULL,
            checkin DATETIME NOT NULL,
            checkout DATETIME NOT NULL,
            number_of_travelers INT NOT NULL,
            status ENUM('pending', 'confirmed', 'cancelled', 'completed') NOT NULL DEFAULT 'pending',
            FOREIGN KEY (user_id) REFERENCES users(id),
            FOREIGN KEY (package_id) REFERENCES trip_packages(id)
        );

        -- BOOKED_ROOMS table
        CREATE TABLE booked_rooms (
            booking_id INT NOT NULL,
            hotel_id INT NOT NULL,
            room_number INT NOT NULL,
            price_per_night DECIMAL(10, 2) NOT NULL,
            PRIMARY KEY (booking_id, hotel_id, room_number),
            FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE CASCADE,
            FOREIGN KEY (hotel_id, room_number) REFERENCES rooms(hotel_id, room_number) ON DELETE CASCADE
        );
        """;

    string views = """
        DROP VIEW IF EXISTS receipt;

        CREATE VIEW receipt AS (
        SELECT
        b.id AS booking,
        CONCAT(u.first_name, ' ', u.last_name) AS name,
        tp.name AS package,
        b.number_of_travelers AS travelers,
        tp.price_per_person AS price_per_person,
        DATEDIFF(b.checkout, b.checkin) AS nights,
        (tp.price_per_person * b.number_of_travelers) 
        + COALESCE(SUM(br.price_per_night * DATEDIFF(b.checkout, b.checkin)), 0) AS total
        FROM bookings b
        JOIN trip_packages tp ON b.package_id = tp.id
        LEFT JOIN booked_rooms br ON b.id = br.booking_id
        JOIN users AS u ON b.user_id = u.id
        WHERE b.id = 1
        GROUP BY b.id, u.first_name, u.last_name, tp.name, b.number_of_travelers, tp.price_per_person, b.checkin, b.checkout
        );
        """;



    string seeds = """

        SET FOREIGN_KEY_CHECKS = 0;
        TRUNCATE TABLE booked_rooms;
        TRUNCATE TABLE bookings;
        TRUNCATE TABLE accommodation_facilities;
        TRUNCATE TABLE rooms;
        TRUNCATE TABLE hotel_poi_distances;
        TRUNCATE TABLE poi_distances;
        TRUNCATE TABLE hotels;
        TRUNCATE TABLE package_itineraries;
        TRUNCATE TABLE trip_packages;
        TRUNCATE TABLE destinations;
        TRUNCATE TABLE facilities;
        TRUNCATE TABLE countries;
        TRUNCATE TABLE users;
        SET FOREIGN_KEY_CHECKS = 1;

        -- ===========================
        -- USERS
        -- ===========================
        INSERT INTO users (id, first_name, last_name, email, password) VALUES
        (1, 'Anna', 'Svensson', 'anna@example.com', 'password123'),
        (2, 'Johan', 'Larsson', 'johan@example.com', 'password123'),
        (3, 'Maria', 'Gonzalez', 'maria@example.com', 'password123');

        -- ===========================
        -- ADMINS
        -- ===========================
        INSERT INTO admins (id, email, password) VALUES 
        (1, 'christian@example.com', '123');


        -- ===========================
        -- COUNTRIES
        -- ===========================
        INSERT INTO countries (id, name, cuisine) VALUES
        (1, 'Italy', 'Italian'),
        (2, 'Japan', 'Japanese'),
        (3, 'Mexico', 'Mexican'),
        (4, 'France', 'French'),
        (5, 'Thailand', 'Thai'),
        (6, 'Spain', 'Spanish');

        -- ===========================
        -- DESTINATIONS
        -- ===========================
        INSERT INTO destinations (id, country_id, city, description) VALUES
        (1, 1, 'Rome', 'Historic capital with amazing pasta, pizza and wine.'),
        (2, 1, 'Florence', 'Art, architecture and Tuscan food.'),
        (3, 2, 'Tokyo', 'Lively metropolis, sushi, ramen and izakayas.'),
        (4, 3, 'Cancún', 'Beach destination with tacos, seafood and nightlife.'),
        (5, 4, 'Paris', 'City of lights with world-class pastries, wine and cheese.'),
        (6, 4, 'Lyon', 'Gastronomic capital of France with bouchons and markets.'),
        (7, 5, 'Bangkok', 'Street food paradise with pad thai, curries and night markets.'),
        (8, 6, 'Barcelona', 'Mediterranean cuisine with tapas, paella and seafood.');

        -- ===========================
        -- TRIP_PACKAGES
        -- ===========================
        INSERT INTO trip_packages (id, name, description, price_per_person) VALUES
        (1, 'Taste of Italy - Rome & Florence', '7 nights focusing on classic Italian food and culture.', 1299.00),
        (2, 'Tokyo Street Food Adventure', '5 nights in Tokyo exploring ramen, sushi and izakayas.', 999.00),
        (3, 'Beach & Tacos in Cancún', '6 nights all-inclusive with Mexican food experiences.', 899.00),
        (4, 'French Culinary Journey', '8 nights discovering Parisian patisseries and Lyonnaise cuisine.', 1499.00),
        (5, 'Bangkok Street Eats', '6 nights immersed in Thai street food and cooking classes.', 799.00),
        (6, 'Tapas Trail through Barcelona', '5 nights exploring Catalan tapas bars and seafood markets.', 1099.00);
       
        -- ===========================
        -- PACKAGE ITINERARIES
        -- ===========================
        INSERT INTO package_itineraries (package_id, destination_id, stop_order, nights) VALUES
        -- Package 1: Taste of Italy (Rome 3 nights → Florence 4 nights)
        (1, 1, 1, 3),
        (1, 2, 2, 4),

        -- Package 2: Tokyo Street Food (Tokyo only, 5 nights)
        (2, 3, 1, 5),

        -- Package 3: Beach & Tacos (Cancún only, 6 nights)
        (3, 4, 1, 6),

        -- Package 4: French Culinary Journey (Paris 4 nights → Lyon 4 nights)
        (4, 5, 1, 4),
        (4, 6, 2, 4),

        -- Package 5: Bangkok Street Eats (Bangkok only, 6 nights)
        (5, 7, 1, 6),

        -- Package 6: Tapas Trail (Barcelona only, 5 nights)
        (6, 8, 1, 5);


        -- ===========================
        -- HOTELS
        -- ===========================
        -- Rome (4 hotels)
        INSERT INTO hotels (id, destination_id, name, description, stars, distance_to_center) VALUES
        (1, 1, 'Hotel Roma Centro', 'Boutique hotel near the historic center.', 4, 0.5),
        (2, 1, 'Colosseum View Inn', 'Budget-friendly hotel with views of the Colosseum.', 3, 0.8),
        (3, 1, 'Vatican Luxury Suites', 'Upscale accommodation near Vatican City.', 5, 1.2),
        (4, 1, 'Trastevere Charming Stay', 'Cozy hotel in the trendy Trastevere district.', 3, 1.5),
        -- Florence (4 hotels)
        (5, 2, 'Florence Riverside Hotel', 'Charming hotel by the river in Florence.', 4, 1.0),
        (6, 2, 'Duomo Grand Hotel', 'Elegant hotel steps from the cathedral.', 5, 0.3),
        (7, 2, 'Tuscan Hills Retreat', 'Peaceful hotel with Tuscan countryside views.', 3, 3.5),
        (8, 2, 'Ponte Vecchio Boutique', 'Historic hotel overlooking the famous bridge.', 4, 0.6),
        -- Tokyo (4 hotels)
        (9, 3, 'Tokyo Shibuya Stay', 'Modern hotel close to Shibuya Crossing.', 3, 0.3),
        (10, 3, 'Shinjuku Skyscraper Hotel', 'High-rise hotel in bustling Shinjuku.', 4, 0.5),
        (11, 3, 'Asakusa Temple Inn', 'Traditional ryokan-style near Senso-ji Temple.', 3, 2.0),
        (12, 3, 'Ginza Luxury Tower', 'Premium hotel in upscale Ginza district.', 5, 1.0),
        -- Cancún (3 hotels)
        (13, 4, 'Cancún Beach Resort', 'Resort directly on the beach with pool and bar.', 5, 2.0),
        (14, 4, 'Hotel Zone Paradise', 'All-inclusive beachfront resort.', 4, 1.5),
        (15, 4, 'Downtown Cancún Budget Stay', 'Affordable hotel in city center.', 2, 0.5),
        -- Paris (4 hotels)
        (16, 5, 'Paris Marais Boutique', 'Elegant hotel in the heart of Le Marais district.', 4, 0.8),
        (17, 5, 'Eiffel Tower View Hotel', 'Romantic hotel with tower views.', 5, 1.5),
        (18, 5, 'Latin Quarter Hostel & Hotel', 'Budget option in student quarter.', 2, 0.6),
        (19, 5, 'Champs-Élysées Grand', 'Luxury hotel on famous avenue.', 5, 0.4),
        -- Lyon (3 hotels)
        (20, 6, 'Lyon Bouchon Inn', 'Traditional hotel near famous bouchon restaurants.', 3, 1.2),
        (21, 6, 'Presqu''île Business Hotel', 'Modern hotel in city center peninsula.', 4, 0.5),
        (22, 6, 'Vieux Lyon Historic Stay', 'Charming hotel in Old Lyon.', 3, 0.8),
        -- Bangkok (4 hotels)
        (23, 7, 'Bangkok Riverside Hotel', 'Modern hotel overlooking the Chao Phraya River.', 4, 0.6),
        (24, 7, 'Khao San Road Hostel', 'Budget backpacker accommodation.', 2, 1.0),
        (25, 7, 'Sukhumvit Sky Hotel', 'High-rise hotel in shopping district.', 4, 2.5),
        (26, 7, 'Grand Palace Luxury', 'Five-star hotel near major attractions.', 5, 0.8),
        -- Barcelona (4 hotels)
        (27, 8, 'Barcelona Gothic Stay', 'Charming hotel in the Gothic Quarter near La Rambla.', 4, 0.4),
        (28, 8, 'Sagrada Familia View Hotel', 'Modern hotel with basilica views.', 4, 1.0),
        (29, 8, 'Barceloneta Beach Hotel', 'Beachfront hotel in beach district.', 3, 2.0),
        (30, 8, 'Eixample Design Hotel', 'Contemporary hotel in modernist district.', 5, 0.7);

        -- ===========================
        -- POI_DISTANCES (reference POIs)
        -- ===========================
        INSERT INTO poi_distances (id, name) VALUES
        (1, 'Colosseum'),
        (2, 'Trevi Fountain'),
        (3, 'Main train station'),
        (4, 'Duomo di Firenze'),
        (5, 'Ponte Vecchio'),
        (6, 'Shibuya Crossing'),
        (7, 'Shinjuku nightlife area'),
        (8, 'Tokyo Tower'),
        (9, 'Beachfront'),
        (10, 'Cancún city center'),
        (11, 'Airport');

        -- ===========================
        -- HOTEL_POI_DISTANCES (junction table)
        -- ===========================
        INSERT INTO hotel_poi_distances (id, hotel_id, poi_distance_id, distance) VALUES
        (1, 1, 1, 0.8),
        (2, 1, 2, 0.6),
        (3, 1, 3, 1.5),
        (4, 5, 4, 1.2),
        (5, 5, 5, 0.7),
        (6, 9, 6, 0.2),
        (7, 9, 7, 3.0),
        (8, 9, 8, 5.5),
        (9, 13, 9, 0.0),
        (10, 13, 10, 5.0),
        (11, 13, 11, 18.0);


        -- ===========================
        -- ROOM TYPES (lookup table)
        -- ===========================
        INSERT INTO room_types (id, type_name, capacity) VALUES
            (1, 'Single room', 2),
            (2, 'Double room', 4),
            (3, 'Family room', 6),
            (4, 'Suite', 8);

        -- ===========================
        -- ROOMS (room types per hotel)
        -- ===========================

        INSERT INTO rooms (hotel_id, room_number, roomtype_id) VALUES
        -- Hotel 1: Mixed room types (7 rooms)
        (1, 101, 1), (1, 102, 1), (1, 103, 2), (1, 104, 2), (1, 105, 3), (1, 106, 3), (1, 107, 4),
        -- Hotel 2: Colosseum View Inn (6 rooms)
        (2, 201, 1), (2, 202, 1), (2, 203, 2), (2, 204, 2), (2, 205, 2), (2, 206, 3),
        -- Hotel 3: Vatican Luxury Suites (4 rooms)
        (3, 301, 3), (3, 302, 3), (3, 303, 4), (3, 304, 4),
        -- Hotel 4: Trastevere Charming Stay (5 rooms)
        (4, 401, 1), (4, 402, 1), (4, 403, 2), (4, 404, 2), (4, 405, 3),
        -- Hotel 5: Florence Riverside Hotel (4 rooms)
        (5, 501, 2), (5, 502, 2), (5, 503, 3), (5, 504, 4),
        -- Hotel 6: Duomo Grand Hotel (3 rooms)
        (6, 601, 3), (6, 602, 4), (6, 603, 4),
        -- Hotel 7: Tuscan Hills Retreat (4 rooms)
        (7, 701, 2), (7, 702, 2), (7, 703, 3), (7, 704, 3),
        -- Hotel 8: Ponte Vecchio Boutique (4 rooms)
        (8, 801, 2), (8, 802, 2), (8, 803, 3), (8, 804, 4),
        -- Hotel 9: Tokyo Shibuya Stay (4 rooms)
        (9, 901, 1), (9, 902, 1), (9, 903, 2), (9, 904, 2),
        -- Hotel 10: Shinjuku Skyscraper (8 rooms)
        (10, 1001, 1), (10, 1002, 1), (10, 1003, 2), (10, 1004, 2), (10, 1005, 2), (10, 1006, 2), (10, 1007, 3), (10, 1008, 4),
        -- Hotel 11: Asakusa Temple Inn (6 rooms)
        (11, 1101, 1), (11, 1102, 1), (11, 1103, 2), (11, 1104, 2), (11, 1105, 3), (11, 1106, 3),
        -- Hotel 12: Ginza Luxury Tower (5 rooms)
        (12, 1201, 3), (12, 1202, 3), (12, 1203, 4), (12, 1204, 4), (12, 1205, 4),
        -- Hotel 13: Cancún Beach Resort (2 rooms)
        (13, 1301, 3), (13, 1302, 4),
        -- Hotel 14: Hotel Zone Paradise (6 rooms)
        (14, 1401, 2), (14, 1402, 2), (14, 1403, 3), (14, 1404, 3), (14, 1405, 4), (14, 1406, 4),
        -- Hotel 15: Downtown Cancún Budget (8 rooms)
        (15, 1501, 1), (15, 1502, 1), (15, 1503, 1), (15, 1504, 1), (15, 1505, 2), (15, 1506, 2), (15, 1507, 2), (15, 1508, 2),
        -- Hotel 16: Paris Marais Boutique (4 rooms)
        (16, 1601, 2), (16, 1602, 2), (16, 1603, 3), (16, 1604, 4),
        -- Hotel 17: Eiffel Tower View Hotel (4 rooms)
        (17, 1701, 3), (17, 1702, 3), (17, 1703, 4), (17, 1704, 4),
        -- Hotel 18: Latin Quarter Hostel (5 rooms)
        (18, 1801, 1), (18, 1802, 1), (18, 1803, 1), (18, 1804, 2), (18, 1805, 2),
        -- Hotel 19: Champs-Élysées Grand (3 rooms)
        (19, 1901, 4), (19, 1902, 4), (19, 1903, 4),
        -- Hotel 20: Lyon Bouchon Inn (3 rooms)
        (20, 2001, 2), (20, 2002, 2), (20, 2003, 3),
        -- Hotel 21: Presqu'île Business Hotel (4 rooms)
        (21, 2101, 1), (21, 2102, 2), (21, 2103, 2), (21, 2104, 3),
        -- Hotel 22: Vieux Lyon Historic Stay (3 rooms)
        (22, 2201, 2), (22, 2202, 2), (22, 2203, 3),
        -- Hotel 23: Bangkok Riverside Hotel (4 rooms)
        (23, 2301, 2), (23, 2302, 2), (23, 2303, 3), (23, 2304, 4),
        -- Hotel 24: Khao San Road Hostel (4 rooms)
        (24, 2401, 1), (24, 2402, 1), (24, 2403, 1), (24, 2404, 2),
        -- Hotel 25: Sukhumvit Sky Hotel (4 rooms)
        (25, 2501, 2), (25, 2502, 2), (25, 2503, 3), (25, 2504, 4),
        -- Hotel 26: Grand Palace Luxury (4 rooms)
        (26, 2601, 3), (26, 2602, 3), (26, 2603, 4), (26, 2604, 4),
        -- Hotel 27: Barcelona Gothic Stay (4 rooms)
        (27, 2701, 2), (27, 2702, 2), (27, 2703, 3), (27, 2704, 4),
        -- Hotel 28: Sagrada Familia View Hotel (4 rooms)
        (28, 2801, 2), (28, 2802, 3), (28, 2803, 3), (28, 2804, 4),
        -- Hotel 29: Barceloneta Beach Hotel (4 rooms)
        (29, 2901, 1), (29, 2902, 2), (29, 2903, 2), (29, 2904, 3),
        -- Hotel 30: Eixample Design Hotel (4 rooms)
        (30, 3001, 3), (30, 3002, 3), (30, 3003, 4), (30, 3004, 4);

        -- ===========================
        -- FACILITIES
        -- ===========================
        INSERT INTO facilities (id, name) VALUES
        (1, 'Free Wi-Fi'),
        (2, 'Breakfast included'),
        (3, 'Swimming pool'),
        (4, 'Spa'),
        (5, 'Beach access'),
        (6, 'Airport shuttle');

        -- ===========================
        -- ACCOMMODATION FACILITIES
        -- ===========================
        INSERT INTO accommodation_facilities (hotel_id, facility_id) VALUES
        (1, 1), (1, 2),
        (2, 1),
        (3, 1), (3, 2), (3, 3), (3, 4), (3, 6),
        (4, 1), (4, 2),
        (5, 1), (5, 2),
        (6, 1), (6, 2), (6, 3), (6, 4),
        (7, 1), (7, 2),
        (8, 1), (8, 2), (8, 4),
        (9, 1),
        (10, 1), (10, 2), (10, 3),
        (11, 1), (11, 2),
        (12, 1), (12, 2), (12, 3), (12, 4), (12, 6),
        (13, 1), (13, 2), (13, 3), (13, 4), (13, 5), (13, 6),
        (14, 1), (14, 2), (14, 3), (14, 4), (14, 5), (14, 6),
        (15, 1),
        (16, 1), (16, 2), (16, 4),
        (17, 1), (17, 2), (17, 3), (17, 4),
        (18, 1),
        (19, 1), (19, 2), (19, 3), (19, 4), (19, 6),
        (20, 1), (20, 2),
        (21, 1), (21, 2), (21, 3),
        (22, 1), (22, 2),
        (23, 1), (23, 2), (23, 3), (23, 4),
        (24, 1),
        (25, 1), (25, 2), (25, 3),
        (26, 1), (26, 2), (26, 3), (26, 4), (26, 6),
        (27, 1), (27, 2), (27, 4),
        (28, 1), (28, 2), (28, 3),
        (29, 1), (29, 2), (29, 5),
        (30, 1), (30, 2), (30, 3), (30, 4);

        -- ===========================
        -- BOOKINGS
        -- ===========================
        INSERT INTO bookings (id, user_id, package_id, checkin, checkout, number_of_travelers, status) VALUES
        (1, 1, 1, '2025-07-01 15:00:00', '2025-07-08 10:00:00', 2, 'confirmed'),
        (2, 2, 2, '2025-09-10 12:00:00', '2025-09-15 09:00:00', 1, 'pending'),
        (3, 3, 3, '2025-11-05 18:00:00', '2025-11-11 08:00:00', 4, 'confirmed');


        -- ===========================
        -- BOOKED ROOMS
        -- ===========================
        INSERT INTO booked_rooms (booking_id, hotel_id, room_number, price_per_night) VALUES
        (1, 1, 101, 150.00),  -- Booking 1: Hotel 1, Room 101
        (1, 2, 201, 150.00),  -- Booking 1: Hotel 2, Room 201
        (2, 3, 301, 110.00),  -- Booking 2: Hotel 3, Room 301
        (3, 4, 401, 200.00);  -- Booking 3: Hotel 4, Room 401

    
    """;

    await MySqlHelper.ExecuteNonQueryAsync(config.db, tables);
    await MySqlHelper.ExecuteNonQueryAsync(config.db, views);
    await MySqlHelper.ExecuteNonQueryAsync(config.db, seeds);
}



