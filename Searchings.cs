namespace server;
using MySql.Data.MySqlClient;

class Searchings
{
    public record GetAll_Data
    (
        string TripPackageName,
        string TripPackageDescription,
        string CountryName,
        string City,
        string CityDescription,
        string HotelName,
        int Stars,
        decimal DistanceToCenter,
        decimal PoiDistance,
        string PoiName
    );

    

    public static async Task<List<GetAll_Data>> GetAllPackages(Config config)
    {
        List<GetAll_Data> result = new();

        string query = """
            SELECT 
                tp.name AS trip_package_name,
                tp.description AS trip_package_description,
                c.name AS country_name,
                d.city AS city,
                d.description AS city_description,
                h.name AS hotel_name,
                h.stars,
                h.distance_to_center,
                hpd.distance AS poi_distance,
                pd.name AS poi_name
            FROM trip_packages AS tp
            INNER JOIN package_itineraries AS pi ON tp.id = pi.package_id
            INNER JOIN destinations AS d ON pi.destination_id = d.id
            INNER JOIN hotels AS h ON d.id = h.destination_id
            INNER JOIN countries AS c ON d.country_id = c.id
            INNER JOIN hotel_poi_distances AS hpd ON h.id = hpd.hotel_id
            INNER JOIN poi_distances AS pd ON hpd.poi_distance_id = pd.id
            ORDER BY tp.name ASC;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (await reader.ReadAsync())
            {
                // possible NULLs for description / distance_to_center / poi_distance needs. maybe needs fix
                string tripPackageName        = reader.GetString(0);
                string tripPackageDescription = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string countryName            = reader.GetString(2);
                string city                   = reader.GetString(3);
                string cityDescription        = reader.IsDBNull(4) ? "" : reader.GetString(4);
                string hotelName              = reader.GetString(5);
                int stars                     = reader.GetInt32(6);
                decimal distanceToCenter      = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7);
                decimal poiDistance           = reader.IsDBNull(8) ? 0m : reader.GetDecimal(8);
                string poiName                = reader.GetString(9);

                result.Add(new GetAll_Data(
                    tripPackageName,
                    tripPackageDescription,
                    countryName,
                    city,
                    cityDescription,
                    hotelName,
                    stars,
                    distanceToCenter,
                    poiDistance,
                    poiName
                ));
            }
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

    // base query
    string query = """
        SELECT 
            tp.name,
            tp.description,
            c.name,
            d.city,
            d.description,
            h.name,
            h.stars,
            h.distance_to_center,
            hpd.distance,
            pd.name
        FROM trip_packages AS tp
        INNER JOIN package_itineraries AS pi ON tp.id = pi.package_id
        INNER JOIN destinations AS d ON pi.destination_id = d.id
        INNER JOIN hotels AS h ON d.id = h.destination_id
        INNER JOIN countries AS c ON d.country_id = c.id
        INNER JOIN hotel_poi_distances AS hpd ON h.id = hpd.hotel_id
        INNER JOIN poi_distances AS pd ON hpd.poi_distance_id = pd.id
    """;

    // filtering by country
    if (!string.IsNullOrEmpty(country))
    {
        query += " WHERE c.name LIKE @country ";
    }

    query += " ORDER BY tp.name ASC; "; // order by travelpackage.name in ascending order

    using var connection = new MySqlConnection(config.db);
    await connection.OpenAsync();
    using var cmd = new MySqlCommand(query, connection);

    if (!string.IsNullOrEmpty(country))
        cmd.Parameters.AddWithValue("@country", $"%{country}%"); // adjust the endpoint to http://localhost:5240/searchings?country=italy as an example

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new GetAll_Data(
            reader.GetString(0),
            reader.IsDBNull(1) ? "" : reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? "" : reader.GetString(4),
            reader.GetString(5),
            reader.GetInt32(6),
            reader.IsDBNull(7) ? 0m : reader.GetDecimal(7),
            reader.IsDBNull(8) ? 0m : reader.GetDecimal(8),
            reader.GetString(9)
        ));
    }
    return result;
    }

    
}

