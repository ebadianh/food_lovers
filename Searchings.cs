namespace server;
using MySql.Data.MySqlClient;

class Searchings
{
    public record GetAll_Data(
        string TripPackage,
        string Country,
        string City,
        string HotelName,
        int RoomCapacity,
        int Stars,
        decimal PackagePrice
    );

    

    public static async Task<List<GetAll_Data>> GetAllPackages(Config config)
    {
        var results = new List<GetAll_Data>();

        using var conn = new MySqlConnection(config.db);
        await conn.OpenAsync();

        var query = """
            SELECT tp.name, c.name, d.city, h.name, r.capacity, h.stars, tp.price_per_person
            FROM trip_packages AS tp
            JOIN package_itineraries AS pi ON tp.id = pi.package_id
            JOIN destinations AS d On pi.destination_id = d.id
            JOIN hotels AS h ON d.id = h.destination_id
            JOIN countries AS c ON c.id = d.country_id
            JOIN rooms AS r ON h.id = r.hotel_id
            WHERE 1=1
        """;

        using var cmd = new MySqlCommand();
        cmd.Connection = conn;

        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += " AND c.name LIKE @search";
            cmd.Parameters.AddWithValue("@search", "%" + search + "%");
        }

        
        if (!string.IsNullOrEmpty(country))
        {
            query += " AND c.name LIKE @country";
            cmd.Parameters.AddWithValue("@country", "%" + country + "%");
        }
        return result;
    }

    public record HotelSearchResult
    (
        int hotelId,
        string hotelName,
        string country,
        string city,
        int totalAvailableCapacity,
        int totalAvailableRooms
    );
    
    public static async Task<List<HotelSearchResult>> GetAllHotelsByPreference(Config config, string country, DateOnly checkin, DateOnly checkout, int total_travelers)
    {
    List<HotelSearchResult> result = new();

    // base query
    string query = """
        SELECT
        h.id AS hotel_id,
        h.name AS hotel_name,
        c.name AS country,
        d.city,
        SUM(r.capacity) AS total_hotel_capacity,
        SUM(r.hotel_id) AS total_available_rooms
        FROM hotels h
            JOIN destinations AS d ON d.id = h.destination_id
            JOIN countries AS c ON c.id = d.country_id
            JOIN rooms AS r On r.hotel_id = h.id
            LEFT JOIN booked_rooms AS br 
                ON br.hotel_id = r.hotel_id 
                AND br.room_number = r.room_number
            LEFT JOIN bookings AS b -- filter out rooms that are booked during this timespan
                ON b.id = br.booking_id 
                AND b.checkin < @checkout 
                AND b.checkout > @checkin
        WHERE br.booking_id IS NULL -- filter out the rooms that are booked
        AND LOWER(c.name) = LOWER(@country)
        GROUP BY h.id, h.name, c.name, d.city
        HAVING SUM(r.capacity) >= @total_travelers; -- filter out hotels that dont have the capacity

        """;

    using var connection = new MySqlConnection(config.db);
    await connection.OpenAsync();
    using var cmd = new MySqlCommand(query, connection);

    

    cmd.Parameters.AddWithValue("@country", country); 
    cmd.Parameters.AddWithValue("@checkin", checkin);
    cmd.Parameters.AddWithValue("@checkout", checkout);
    cmd.Parameters.AddWithValue("@total_travelers", total_travelers);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new HotelSearchResult(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5)
            ));
    }
    return result;
    }

    public static async Task<List<GetAll_Data>> GetAllPackagesByCountry(Config config, string? country = null)
    {
    List<GetAll_Data> result = new();

        
        if (!string.IsNullOrEmpty(city))
        {
            query += " AND d.city LIKE @city";
            cmd.Parameters.AddWithValue("@city", "%" + city + "%");
        }

        
        if (minStars.HasValue)
        {
            query += " AND h.stars >= @minStars";
            cmd.Parameters.AddWithValue("@minStars", minStars);
        }

        
        if (maxPrice.HasValue)
        {
            query += " AND tp.price_per_person <= @maxPrice";
            cmd.Parameters.AddWithValue("@maxPrice", maxPrice);
        }

        query += " ORDER BY tp.name ASC";
        cmd.CommandText = query;

        using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            results.Add(new GetAll_Data(
                reader.GetString(0), // TripPackage Name
                reader.GetString(1), // Country
                reader.GetString(2), // City
                reader.GetString(3), // Hotel
                reader.GetInt32(4),  // Capacity
                reader.GetInt32(5),  // Stars
                reader.GetDecimal(6) // Price
            ));
        }

        return results;
    }
}
