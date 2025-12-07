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
app.MapPost("/users", Users.Post);
app.MapPut("/users/{id}", Users.Put);
app.MapDelete("/users/{id}", Users.Delete);

// CRUD methods for bookings
app.MapGet("/bookings", Bookings.GetAll);


// special, reset db
app.MapDelete("/db", db_reset_to_default);

app.Run();


async Task db_reset_to_default(Config config)
{

    // Drop all tables from database
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");

    // Create all tables
    string users_table = """
        CREATE TABLE users
        (
            id INT PRIMARY KEY AUTO_INCREMENT,
            email varchar(256) NOT NULL UNIQUE,
            password TEXT
        )
    """;
    await MySqlHelper.ExecuteNonQueryAsync(config.db, users_table);

    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, password) VALUES ('lukas@gmail.com', 'password123')");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, password) VALUES ('manni@gmail.com', 'password123')");
}

