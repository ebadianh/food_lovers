using MySql.Data.MySqlClient;

namespace server
{
    public static class Searchings
    {
        // Simple package summary
        public record GetAll_Data(
            string TripPackage,
            string Country,
            string City,
            string HotelName,
            int RoomCapacity,
            int Stars,
            decimal PackagePrice
        );

        public record Get_All_Packages_For_User(
            int Id,
            int UserId,
            int PackageId,
            DateTime Checkin,
            DateTime Checkout,
            int NumberOfTravelers,
            BookingStatus Status
        );


        // More detailed package data (description + distances + POI)
        public record PackageDetails(
            string TripPackage,
            string TripPackageDescription,
            string Country,
            string City,
            string CityDescription,
            string HotelName,
            int Stars,
            decimal DistanceToCenter,
            decimal PoiDistance,
            string PoiName
        );

        /// <summary>
        /// Get all packages with optional filters.
        /// </summary>
        public static async Task<List<GetAll_Data>> GetAllPackages(
            Config config,
            string? search = null,
            string? country = null,
            string? city = null,
            int? minStars = null,
            decimal? maxPrice = null)
        {
            var results = new List<GetAll_Data>();

            await using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();

            var query = @"
                SELECT 
                    tp.name, 
                    c.name, 
                    d.city, 
                    h.name, 
                    r.capacity, 
                    h.stars, 
                    tp.price_per_person
                FROM trip_packages AS tp
                JOIN package_itineraries AS pi ON tp.id = pi.package_id
                JOIN destinations AS d ON pi.destination_id = d.id
                JOIN hotels AS h ON d.id = h.destination_id
                JOIN countries AS c ON c.id = d.country_id
                JOIN rooms AS r ON h.id = r.hotel_id
                WHERE 1 = 1
            ";

            await using var cmd = new MySqlCommand();
            cmd.Connection = conn;

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Search by package name / city / country
                query += " AND (tp.name LIKE @search OR d.city LIKE @search OR c.name LIKE @search)";
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
                cmd.Parameters.AddWithValue("@minStars", minStars.Value);
            }

            if (maxPrice.HasValue)
            {
                query += " AND tp.price_per_person <= @maxPrice";
                cmd.Parameters.AddWithValue("@maxPrice", maxPrice.Value);
            }

            query += " ORDER BY tp.name ASC;";
            cmd.CommandText = query;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new GetAll_Data(
                    reader.GetString(0), // TripPackage
                    reader.GetString(1), // Country
                    reader.GetString(2), // City
                    reader.GetString(3), // HotelName
                    reader.GetInt32(4),  // RoomCapacity
                    reader.GetInt32(5),  // Stars
                    reader.GetDecimal(6) // PackagePrice
                ));
            }

            return results;
        }
 public static async Task<IResult> GetAllPackagesForUser(Config config, HttpContext ctx)
    {
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
        {
        // if not logged in, not allowed to retrieve data
            return Results.Unauthorized();
        }

        List<Get_All_Packages_For_User> result = new();

        string query = """
                SELECT id, user_id, package_id, checkin, checkout, number_of_travelers, status
                FROM bookings
                WHERE user_id = @user_id;
        """;

         var parameters = new MySqlParameter[]
    {
        new("@user_id", userId.Value)
    };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                int user_Id = reader.GetInt32(1);
                int packageId = reader.GetInt32(2);
                DateTime checkin = reader.GetDateTime(3);
                DateTime checkout = reader.GetDateTime(4);
                int numberOfTravelers = reader.GetInt32(5);
                string statusString = reader.GetString(6);

                BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

                result.Add(new Get_All_Packages_For_User(
                    id,
                    user_Id,
                    packageId,
                    checkin,
                    checkout,
                    numberOfTravelers,
                    status
                ));
            }
        }

        return Results.Ok(result);

    }


        /// <summary>
        /// Detailed packages by country (includes descriptions, distances, POI).
        /// </summary>
        public static async Task<List<PackageDetails>> GetAllPackagesByCountry(
            Config config,
            string? country = null)
        {
            List<PackageDetails> result = new();

            string query = @"
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
            ";

            using var connection = new MySqlConnection(config.db);
            await connection.OpenAsync();
            using var cmd = new MySqlCommand();
            cmd.Connection = connection;

            if (!string.IsNullOrEmpty(country))
            {
                query += " WHERE c.name LIKE @country ";
                cmd.Parameters.AddWithValue("@country", $"%{country}%");
            }

            query += " ORDER BY tp.name ASC; ";
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new PackageDetails(
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

        public record GetHotelByF(
            string HotelName,
            string City,
            string Country,
            string Facilities
        );

        public static async Task<List<GetHotelByF>> GetHotelByFacilities(Config config)
        {
            List<GetHotelByF> result = new();

            string query = @"
                SELECT
                    h.name AS HotelName,
                    d.city AS City,
                    c.name AS Country,
                    GROUP_CONCAT(DISTINCT f.name ORDER BY f.name SEPARATOR ', ') AS Facilities
                FROM hotels AS h
                INNER JOIN destinations d ON h.destination_id = d.id
                INNER JOIN countries c ON d.country_id = c.id
                INNER JOIN accommodation_facilities af ON h.id = af.hotel_id
                INNER JOIN facilities f ON af.facility_id = f.id
                WHERE f.name IN ('Swimming pool', 'Spa')
                GROUP BY h.name, d.city, c.name
                HAVING COUNT(DISTINCT f.name) = 2;
            ";

            using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
            {
                while (await reader.ReadAsync())
                {
                    string hotelName = reader.GetString(0);
                    string city = reader.GetString(1);
                    string country = reader.GetString(2);
                    string facilities = reader.GetString(3);

                    result.Add(new GetHotelByF(
                        hotelName,
                        city,
                        country,
                        facilities
                    ));
                }
            }

            return result;
        }

        public record GetHotelByW(
            string Country,
            string City,
            string HotelName,
            string Facility
        );

        public static async Task<List<GetHotelByW>> GetHotelByWiFi(Config config)
        {
            List<GetHotelByW> result = new();

            // Fixed JOINs – previous version joined facilities to countries etc.
            string query = @"
                SELECT
                    c.name AS Country,
                    d.city AS City,
                    h.name AS HotelName,
                    f.name AS Facility
                FROM hotels AS h
                INNER JOIN destinations AS d ON h.destination_id = d.id
                INNER JOIN countries AS c ON d.country_id = c.id
                INNER JOIN accommodation_facilities AS af ON h.id = af.hotel_id
                INNER JOIN facilities AS f ON af.facility_id = f.id
                WHERE f.name = 'Free Wi-Fi';
            ";

            using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
            {
                while (await reader.ReadAsync())
                {
                    string country = reader.GetString(0);
                    string city = reader.GetString(1);
                    string hotelName = reader.GetString(2);
                    string facility = reader.GetString(3);

                    result.Add(new GetHotelByW(
                        country,
                        city,
                        hotelName,
                        facility
                    ));
                }
            }

            return result;
        }

        public record HotelSearchResult
        (
            int HotelId,
            string HotelName,
            string Country,
            string City,
            int TotalAvailableCapacity,
            int TotalAvailableRooms
        );

        public static async Task<List<HotelSearchResult>> GetAllHotelsByPreference(
            Config config,
            string country,
            DateOnly checkin,
            DateOnly checkout,
            int total_travelers)
        {
            List<HotelSearchResult> result = new();

            string query = @"
                SELECT
                    h.id AS hotel_id,
                    h.name AS hotel_name,
                    c.name AS country,
                    d.city,
                    SUM(r.capacity) AS total_hotel_capacity,
                    COUNT(*) AS total_available_rooms
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
                WHERE b.id IS NULL -- filter out the rooms that are booked
                  AND LOWER(c.name) = LOWER(@country)
                GROUP BY h.id, h.name, c.name, d.city
                HAVING SUM(r.capacity) >= @total_travelers; -- filter out hotels that dont have the capacity
            ";

            using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(query, conn);

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

        public record GetHotelByS
        (
            string Country,
            string City,
            string HotelName,
            int Stars
        );

        public static async Task<List<GetHotelByS>> GetHotelByStars(Config config)
        {
            List<GetHotelByS> result = new();

            string query = @"
                SELECT
                    c.name AS Country,
                    d.city AS City,
                    h.name AS HotelName,
                    h.stars AS Stars
                FROM hotels h
                INNER JOIN destinations AS d ON h.destination_id = d.id
                INNER JOIN countries AS c ON d.country_id = c.id
                WHERE h.stars >= 4
                ORDER BY h.stars DESC;
            ";

            using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
            {
                while (await reader.ReadAsync())
                {
                    string country = reader.GetString(0);
                    string city = reader.GetString(1);
                    string hotelName = reader.GetString(2);
                    int stars = reader.GetInt32(3);

                    result.Add(new GetHotelByS(
                        country,
                        city,
                        hotelName,
                        stars
                    ));
                }
            }

            return result;
        }

        public record GetHotelByD
        (
            string Country,
            string City,
            string HotelName,
            double DistanceToCenter
        );

        public static async Task<List<GetHotelByD>> GetHotelByDistanceToC(Config config)
        {
            List<GetHotelByD> result = new();

            string query = @"
                SELECT
                    c.name AS Country,
                    d.city AS City,
                    h.name AS HotelName,
                    h.distance_to_center AS DistanceToCenter
                FROM hotels h
                INNER JOIN destinations AS d ON h.destination_id = d.id
                INNER JOIN countries AS c ON d.country_id = c.id
                WHERE h.distance_to_center <= 1
                ORDER BY h.distance_to_center DESC;
            ";

            using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
            {
                while (await reader.ReadAsync())
                {
                    string country = reader.GetString(0);
                    string city = reader.GetString(1);
                    string hotelName = reader.GetString(2);
                    double distanceToCenter = reader.GetDouble(3);

                    result.Add(new GetHotelByD(
                        country,
                        city,
                        hotelName,
                        distanceToCenter
                    ));
                }
            }

            return result;
        }
    }
}
