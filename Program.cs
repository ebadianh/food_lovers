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

        CREATE TABLE Users (
        id INT PRIMARY KEY,
        firstname VARCHAR(255),
        lastname VARCHAR(255),
        email VARCHAR(255),
        password VARCHAR(255)
    );

    CREATE TABLE Admins (
        id INT PRIMARY KEY,
        username VARCHAR(255),
        password VARCHAR(255)
    );

    CREATE TABLE Countries (
        id INT PRIMARY KEY,
        name VARCHAR(255),
        cuisine VARCHAR(255)
    );

    CREATE TABLE Destinations (
        id INT PRIMARY KEY,
        countryid INT,
        city VARCHAR(255),
        description TEXT,
        FOREIGN KEY (countryid) REFERENCES Countries(id)
    );

    CREATE TABLE Trippackages (
        id INT PRIMARY KEY,
        name VARCHAR(255),
        desciption TEXT,
        price DECIMAL(10,2)
    );

    CREATE TABLE PackageItinerary (
        packageid INT,
        destinationid INT,
        stopOrder INT,
        nights TINYINT,
        PRIMARY KEY (packageid, destinationid),
        FOREIGN KEY (packageid) REFERENCES TRIPPACKAGES(id),
        FOREIGN KEY (destinationid) REFERENCES DESTINATIONS(id)
    );

    CREATE TABLE Hotels (
        id INT PRIMARY KEY,
        destinationid INT,
        name VARCHAR(255),
        distance DECIMAL(10,2),
        description TEXT,
        stars TINYINT,
        FOREIGN KEY (destinationid) REFERENCES Destinations(id)
    );

    CREATE TABLE Rooms (
        id INT PRIMARY KEY,
        hotelid INT,
        number INT,
        capacity INT,
        FOREIGN KEY (hotelid) REFERENCES HOTELS(id)
    );

    CREATE TABLE PoI (
        id INT PRIMARY KEY,
        name VARCHAR(255),
        distance DECIMAL(10,2)
    );

    CREATE TABLE Hotels_to_PoI (
        hotelid INT,
        poiid INT,
        PRIMARY KEY (hotelid, poiid),
        FOREIGN KEY (hotelid) REFERENCES HOTELS(id),
        FOREIGN KEY (poiid) REFERENCES POI(id)
    );

    CREATE TABLE Facilities (
        id INT PRIMARY KEY,
        name VARCHAR(255)
    );

    CREATE TABLE Accomodations_by_Facilities (
        hotel_id INT,
        facility_id INT,
        PRIMARY KEY (hotelid, facilityid),
        FOREIGN KEY (hotelid) REFERENCES HOTELS(id),
        FOREIGN KEY (facilityid) REFERENCES FACILITIES(id)
    );

    CREATE TABLE Bookings (
        id INT PRIMARY KEY,
        userid INT,
        packageid INT,
        checkin DATE,
        checkout DATE,
        numoftravellers INT,
        status ENUM('pending','confirmed','cancelled'),
        FOREIGN KEY (userid) REFERENCES users(id),
        FOREIGN KEY (packageid) REFERENCES Trippackages(id)
    );

    CREATE TABLE Rooms_by_Bookings (
        bookingid INT,
        roomid INT,
        quantity INT,
        price DECIMAL(10,2),
        PRIMARY KEY (bookingid, roomid),
        FOREIGN KEY (bookingid) REFERENCES Bookings(id),
        FOREIGN KEY (roomid) REFERENCES Rooms(id)
    );


        )
    """;
    await MySqlHelper.ExecuteNonQueryAsync(config.db, tables);
    
}


