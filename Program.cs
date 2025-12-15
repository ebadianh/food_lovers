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
app.MapDelete("/login", Login.Delete);

// CRUD examples (user resource)
app.MapGet("/users", Users.Get);
app.MapGet("/users/{id}", Users.GetById);
app.MapPost("/users", Users.Post); //As an anomymous user I want to create an account, so I that can become a registered user
app.MapPut("/users/{id}", Users.Put);
app.MapDelete("/users/{id}", Users.Delete);

// CRUD methods for bookings
app.MapGet("/bookings", Bookings.GetAll);
app.MapGet("/bookings/{bookingsId}/totalcost", Bookings.GetTotalCostByBooking);
app.MapPost("/bookings", Bookings.Post);
app.MapDelete("/bookings/{id:int}", Bookings.Delete);
app.MapPut("/bookings/{bookingId:int}", Bookings.Put);


// CRUD methods for searchings
app.MapGet("/searchings", Searchings.GetAllPackages);
app.MapGet("/searchings/user", Searchings.GetAllPackagesForUser);


// CRUD Methods for packages
app.MapGet("/searchings/SuggestedCountry", Searchings.GetSuggestedByCountry);


app.MapGet("/search/hotels", Searchings.GetAllHotelsByPreference);
app.MapGet("/search/hotels/filters", async (
    Config config,
    string country,
    DateTime checkin,
    DateTime checkout,
    int total_travelers,
    string? city,
    string? hotelName,
    int? minStars,
    double? maxDistanceToCenter,
    string? facilities) =>
{
    List<string>? facilitiesList = null;
    if (!string.IsNullOrWhiteSpace(facilities))
    {
        facilitiesList = facilities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(f => f.Trim())
                                   .ToList();
    }

    var filter = new Searchings.ApplyFiltersRequest(
        country,
        checkin,
        checkout,
        total_travelers,
        city,
        hotelName,
        facilitiesList,
        minStars,
        maxDistanceToCenter
    );
    
    var hotels = await Searchings.GetAllHotelsByFilters(config, filter);
    return Results.Ok(hotels);
});



app.MapGet("/searchingsbycountry", async (Config config, string? country) =>
{
    return await Searchings.GetAllPackagesByCountry(config, country);
});

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
            PRIMARY KEY (package_id, destination_id),
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
        -- COUNTRIES
        -- ===========================
        INSERT INTO countries (id, name, cuisine) VALUES
        (1, 'Italy', 'Italian'),
        (2, 'Japan', 'Japanese'),
        (3, 'Mexico', 'Mexican');

        -- ===========================
        -- DESTINATIONS
        -- ===========================
        INSERT INTO destinations (id, country_id, city, description) VALUES
        (1, 1, 'Rome', 'Historic capital with amazing pasta, pizza and wine.'),
        (2, 1, 'Florence', 'Art, architecture and Tuscan food.'),
        (3, 2, 'Tokyo', 'Lively metropolis, sushi, ramen and izakayas.'),
        (4, 3, 'Cancún', 'Beach destination with tacos, seafood and nightlife.');

        -- ===========================
        -- TRIP PACKAGES
        -- ===========================
        INSERT INTO trip_packages (id, name, description, price_per_person) VALUES
        (1, 'Taste of Italy - Rome & Florence', '7 nights focusing on classic Italian food and culture.', 1299.00),
        (2, 'Tokyo Street Food Adventure', '5 nights in Tokyo exploring ramen, sushi and izakayas.', 999.00),
        (3, 'Beach & Tacos in Cancún', '6 nights all-inclusive with Mexican food experiences.', 899.00);

        -- ===========================
        -- PACKAGE ITINERARIES
        -- ===========================
        INSERT INTO package_itineraries (package_id, destination_id, stop_order, nights) VALUES
        (1, 1, 1, 3),  -- Italy: Rome 3 nights
        (1, 2, 2, 4),  -- Italy: Florence 4 nights
        (2, 3, 1, 5),  -- Tokyo only
        (3, 4, 1, 6);  -- Cancún only

        -- ===========================
        -- HOTELS
        -- ===========================
        INSERT INTO hotels (id, destination_id, name, description, stars, distance_to_center) VALUES
        (1, 1, 'Hotel Roma Centro', 'Boutique hotel near the historic center.', 4, 0.5),
        (2, 2, 'Florence Riverside Hotel', 'Charming hotel by the river in Florence.', 4, 1.0),
        (3, 3, 'Tokyo Shibuya Stay', 'Modern hotel close to Shibuya Crossing.', 3, 0.3),
        (4, 4, 'Cancún Beach Resort', 'Resort directly on the beach with pool and bar.', 5, 2.0);

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

        (4, 2, 4, 1.2),
        (5, 2, 5, 0.7),

        (6, 3, 6, 0.2),
        (7, 3, 7, 3.0),
        (8, 3, 8, 5.5),

        (9, 4, 9, 0.0),
        (10, 4, 10, 5.0),
        (11, 4, 11, 18.0);

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
            (1, 101, 1),  -- Single
            (1, 102, 1),  -- Single
            (1, 103, 2),  -- Double
            (1, 104, 2),  -- Double
            (1, 105, 3),  -- Family
            (1, 106, 3),  -- Family
            (1, 107, 4),  -- Suite

            -- Hotel 2: Mostly family-oriented (4 rooms)
            (2, 201, 2),  -- Double
            (2, 202, 2),  -- Double
            (2, 203, 3),  -- Family
            (2, 204, 4),  -- Suite

            -- Hotel 3: Budget hotel (4 rooms)
            (3, 301, 1),  -- Single
            (3, 302, 1),  -- Single
            (3, 303, 2),  -- Double
            (3, 304, 2),  -- Double

            -- Hotel 4: Luxury resort (2 rooms)
            (4, 401, 3),  -- Family
            (4, 402, 4);  -- Suite


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
        (1, 1),
        (1, 2),
        (2, 1),
        (2, 2),
        (3, 1),
        (4, 1),
        (4, 2),
        (4, 3),
        (4, 4),
        (4, 5),
        (4, 6);

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
        (2, 3, 301, 110.00),  -- Booking 2: Hotel 3, Room 301
        (3, 4, 401, 200.00);  -- Booking 3: Hotel 4, Room 401

    
    """;

    await MySqlHelper.ExecuteNonQueryAsync(config.db, tables);
    await MySqlHelper.ExecuteNonQueryAsync(config.db, views);
    await MySqlHelper.ExecuteNonQueryAsync(config.db, seeds);
}



