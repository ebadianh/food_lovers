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
        public record GetAllHotels
        (
            int HotelId,
            string HotelName,
            string Country,
            string City,
            int TotalAvailableCapacity,
            int TotalAvailableRooms
        );
        public record ApplyFiltersRequest(
            string Country,
            DateTime Checkin,
            DateTime Checkout,
            int TotalTravelers,
            string? City = null,
            string? HotelName = null,
            List<string>? Facilities = null,  // 7th parameter
            int? MinStars = null,
            double? MaxDistanceToCenter = null
        );

        public record HotelFilterResult(
            string Country,
            string City,
            string HotelName,
            int Stars,
            double DistanceToCenter,
            string Facilities
        );


        public record GetHotelByF(
            string HotelName,
            string City,
            string Country,
            string Facilities
        );
        public record GetHotelByW(
            string Country,
            string City,
            string HotelName,
            string Facility
        );
        public record GetHotelByS
        (
            string Country,
            string City,
            string HotelName,
            int Stars
        );
        public record GetHotelByD
        (
            string Country,
            string City,
            string HotelName,
            double DistanceToCenter
        );

        public static async Task<List<HotelFilterResult>> GetAllHotelsByFilters(
            Config config,
            ApplyFiltersRequest filter)
        {
            List<HotelFilterResult> result = new();

            string query = """
                    SELECT
                    c.name AS Country,
                    d.city AS City,
                    h.name AS HotelName,
                    h.stars AS Stars,
                    h.distance_to_center AS DistanceToCenter,
                    GROUP_CONCAT(DISTINCT f.name ORDER BY f.name SEPARATOR ', ') AS Facilities
                FROM hotels h
                INNER JOIN destinations AS d ON h.destination_id = d.id
                INNER JOIN countries AS c ON d.country_id = c.id
                LEFT JOIN accommodation_facilities AS af ON h.id = af.hotel_id
                LEFT JOIN facilities AS f ON af.facility_id = f.id
                JOIN rooms AS r ON r.hotel_id = h.id
                JOIN room_types AS rt ON rt.id = r.roomtype_id
                LEFT JOIN booked_rooms AS br 
                    ON br.hotel_id = r.hotel_id 
                    AND br.room_number = r.room_number
                LEFT JOIN bookings AS b
                    ON b.id = br.booking_id 
                    AND b.checkin < @checkout 
                    AND b.checkout > @checkin
                WHERE br.booking_id IS NULL
                AND LOWER(c.name) = LOWER(@country)
            """;

            using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand();
            cmd.Connection = conn;

            // Takes in parameters from the initial search
            cmd.Parameters.AddWithValue("@country", filter.Country);
            cmd.Parameters.AddWithValue("@checkin", filter.Checkin);
            cmd.Parameters.AddWithValue("@checkout", filter.Checkout);
            cmd.Parameters.AddWithValue("@total_travelers", filter.TotalTravelers);

            // Optional filters
            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query += " AND d.city LIKE @city";
                cmd.Parameters.AddWithValue("@city", "%" + filter.City + "%");
            }

            if (!string.IsNullOrWhiteSpace(filter.HotelName))
            {
                query += " AND h.name LIKE @hotelName";
                cmd.Parameters.AddWithValue("@hotelName", "%" + filter.HotelName + "%");
            }

            if (filter.MinStars.HasValue)
            {
                query += " AND h.stars >= @minStars";
                cmd.Parameters.AddWithValue("@minStars", filter.MinStars.Value);
            }

            if (filter.MaxDistanceToCenter.HasValue)
            {
                query += " AND h.distance_to_center <= @maxDistance";
                cmd.Parameters.AddWithValue("@maxDistance", filter.MaxDistanceToCenter.Value);
            }

            query += " GROUP BY h.id, h.name, c.name, d.city, h.stars, h.distance_to_center";

            if (filter.Facilities?.Count > 0)
            {
                query += " HAVING COUNT(DISTINCT CASE WHEN f.name IN (";
                
                for (int i = 0; i < filter.Facilities.Count; i++)
                {
                    query += i > 0 ? ", " : "";
                    query += $"@facility{i}";
                    cmd.Parameters.AddWithValue($"@facility{i}", filter.Facilities[i]);
                }
                
                query += $") THEN f.name END) = {filter.Facilities.Count}";
                query += " AND SUM(rt.capacity) >= @total_travelers";
            }
            else
            {
                query += " HAVING SUM(rt.capacity) >= @total_travelers";
            }

            query += " ORDER BY h.name ASC;";
            cmd.CommandText = query;


            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new HotelFilterResult(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetDouble(4),
                    reader.IsDBNull(5) ? "No facilities" : reader.GetString(5)
                ));
            }
            return result;
        }

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

        public static async Task<List<GetAllHotels>> GetAllHotelsByPreference(
            Config config,
            string country,
            DateTime checkin,
            DateTime checkout,
            int total_travelers)
        {
            List<GetAllHotels> result = new();

            string query = @"
                SELECT
                    h.id AS hotel_id,
                    h.name AS hotel_name,
                    c.name AS country,
                    d.city,
                    SUM(rt.capacity) AS total_hotel_capacity,
                    COUNT(r.hotel_id) AS total_available_rooms
                FROM hotels h
                JOIN destinations AS d ON d.id = h.destination_id
                JOIN countries AS c ON c.id = d.country_id
                JOIN rooms AS r ON r.hotel_id = h.id
                JOIN room_types AS rt ON rt.id = r.roomtype_id
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
                HAVING SUM(rt.capacity) >= @total_travelers; -- filter out hotels that dont have the capacity
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
                result.Add(new GetAllHotels(
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
    }
}