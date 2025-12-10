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

    public static async Task<List<GetAll_Data>> GetAllPackages(
        Config config,
        string? search = null,     // <-- Added search parameter
        string? country = null,
        string? city = null,
        int? minStars = null,
        decimal? maxPrice = null)
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