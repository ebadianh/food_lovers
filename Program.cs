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

var app = builder.Build();
app.UseSession();

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

// CRUD methods for searchings
app.MapGet("/searchings", Searchings.GetAllPackages);

// special, reset db
app.MapDelete("/db", db_reset_to_default);

app.Run();


async Task db_reset_to_default(Config config)
{

    // Drop all tables from database
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");

    // Create all tables
    string tables = """

        -- USERS table
        CREATE TABLE users
        (
            id INT PRIMARY KEY AUTO_INCREMENT,
            firstname varchar(256) NOT NULL,
            lastname varchar(256) NOT NULL,
            email varchar(256) NOT NULL UNIQUE,
            password varchar(256) NOT NULL,
            
            CONSTRAINT chk_email_format
                CHECK (email LIKE '%_@_%._%'),

            CONSTRAINT chk_password_strength
                CHECK (LENGTH(password) >= 8
                AND password REGEXP '.*[A-Z].*'
                AND password REGEXP '.*[a-z].*'
                AND password REGEXP '.*[0-9].*'
            )
        );

        -- COUNTRY table
        CREATE TABLE COUNTRY (
            CountryID INT PRIMARY KEY AUTO_INCREMENT,
            Name VARCHAR(100) NOT NULL,
            Cuisine VARCHAR(100)
        );

        -- DESTINATIONS table
        CREATE TABLE DESTINATIONS (
            DestinationID INT PRIMARY KEY AUTO_INCREMENT,
            CountryID INT NOT NULL,
            City VARCHAR(100) NOT NULL,
            Description TEXT,
            FOREIGN KEY (CountryID) REFERENCES COUNTRY(CountryID)
        );

        -- TRIPPACKAGES table
        CREATE TABLE TRIPPACKAGES (
            PackageID INT PRIMARY KEY AUTO_INCREMENT,
            PackageName VARCHAR(150) NOT NULL,
            Description TEXT,
            PricePerPerson DECIMAL(10, 2) NOT NULL
        );

        -- PACKAGEITINERARY table (junction table)
        CREATE TABLE PACKAGEITINERARY (
            PackageID INT NOT NULL,
            DestinationID INT NOT NULL,
            StopOrder INT NOT NULL,
            Nights TINYINT NOT NULL,
            PRIMARY KEY (PackageID, DestinationID),
            FOREIGN KEY (PackageID) REFERENCES TRIPPACKAGES(PackageID),
            FOREIGN KEY (DestinationID) REFERENCES DESTINATIONS(DestinationID)
        );

        -- HOTELS table
        CREATE TABLE HOTELS (
            HotelID INT PRIMARY KEY AUTO_INCREMENT,
            DestinationID INT NOT NULL,
            HotelName VARCHAR(150) NOT NULL,
            Description TEXT,
            Stars TINYINT CHECK (Stars BETWEEN 1 AND 5),
            DistanceToCenter DECIMAL(5, 2),
            FOREIGN KEY (DestinationID) REFERENCES DESTINATIONS(DestinationID)
        );

        -- POI_DISTANCES table
        CREATE TABLE POI_DISTANCES (
            POI_DistancesID INT PRIMARY KEY AUTO_INCREMENT,
            Name VARCHAR(150) NOT NULL
        );

        -- HOTELS_to_POI_DISTANCES table (junction table)
        CREATE TABLE HOTELS_to_POI_DISTANCES (
            HOTELS_to_POI_DISTANCESID INT PRIMARY KEY AUTO_INCREMENT,
            HotelID INT NOT NULL,
            POI_DISTANCESID INT NOT NULL,
            Distance DECIMAL(5, 2),
            FOREIGN KEY (HotelID) REFERENCES HOTELS(HotelID),
            FOREIGN KEY (POI_DISTANCESID) REFERENCES POI_DISTANCES(POI_DistancesID)
        );

        -- ROOMS table
        CREATE TABLE ROOMS (
            RoomID INT PRIMARY KEY AUTO_INCREMENT,
            HotelID INT NOT NULL,
            Capacity INT NOT NULL,
            FOREIGN KEY (HotelID) REFERENCES HOTELS(HotelID)
        );

        -- FACILITIES table
        CREATE TABLE FACILITIES (
            FacilityID INT PRIMARY KEY AUTO_INCREMENT,
            FacilityName VARCHAR(100) NOT NULL
        );

        -- ACCOMMODATIONFACILITIES table (junction table)
        CREATE TABLE ACCOMMODATIONFACILITIES (
            HotelID INT NOT NULL,
            FacilityID INT NOT NULL,
            PRIMARY KEY (HotelID, FacilityID),
            FOREIGN KEY (HotelID) REFERENCES HOTELS(HotelID),
            FOREIGN KEY (FacilityID) REFERENCES FACILITIES(FacilityID)
        );

        -- BOOKINGS table
        CREATE TABLE BOOKINGS (
            BookingID INT PRIMARY KEY AUTO_INCREMENT,
            UserID INT NOT NULL,
            PackageID INT NOT NULL,
            Checkin DATE NOT NULL,
            Checkout DATE NOT NULL,
            NumberOfTravelers INT NOT NULL,
            Status ENUM('pending', 'confirmed', 'cancelled', 'completed') NOT NULL DEFAULT 'pending',
            FOREIGN KEY (UserID) REFERENCES USERS(id),
            FOREIGN KEY (PackageID) REFERENCES TRIPPACKAGES(PackageID)
        );

        -- BOOKEDROOMS table
        CREATE TABLE BOOKEDROOMS (
            BookedRoomID INT PRIMARY KEY AUTO_INCREMENT,
            BookingID INT NOT NULL,
            RoomID INT NOT NULL,
            Quantity INT NOT NULL,
            PricePerNight DECIMAL(10, 2) NOT NULL,
            FOREIGN KEY (BookingID) REFERENCES BOOKINGS(BookingID),
            FOREIGN KEY (RoomID) REFERENCES ROOMS(RoomID)
        );

    """;
    await MySqlHelper.ExecuteNonQueryAsync(config.db, tables);
    
}


