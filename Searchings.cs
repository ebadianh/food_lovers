using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

namespace server
{
    public static class Searchings
    {


        public record PackageSearchResult(
            int PackageId,
            string PackageName,
            string Description,
            decimal PricePerPerson,
            string Route,  // "Rome → Florence" or "Tokyo"
            string Countries  // "Italy" or "France"
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
        public record HotelFilterRequest(
            string Country,
            DateTime Checkin,
            DateTime Checkout,
            int TotalTravelers,
            string? City = null,
            string? HotelName = null,
            List<string>? Facilities = null,
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
        public record PackageFilterRequest(
            string? Search = null,
            string? Country = null,
            string? City = null,
            int? MinStars = null,
            decimal? MaxPrice = null
        );

        public record AdminHotelView(
            int Id,
            int DestinastionId,
            string Name,
            string Description,
            int Stars,
            decimal DistanceToCenter
        );

        public record HotelByID(
          string Country,
          string City,
          int Id,
          string HotelName,
          int Stars,
          decimal DistanceToCenter,
          string Description,
          int TotalRooms,
          string RoomTypes
      );

        public record AdminTrips(
        int Id,
        string Name,
        string Description,
        decimal PricePerPerson
        );

        public record TripByID(
        string Country,
        string City,
        int Id,
        string Name,
        string Description,
        decimal PricePerPerson,
        int Nights
        );



        public record AdminFacilities(
            int Id,
            string Name

        );
        public record FacilityByID(
            int FacilityId,
            string FacilityName,
            int HotelId,
            string HotelName,
            int Stars,
            decimal DistanceToCenter,
            string City,
            string Country
            );

        public static async Task<List<HotelFilterResult>> GetAllHotelsFiltered(
            Config config,
            HotelFilterRequest filter)
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
        public record CustomizedPackage(
            int PackageId,
            string PackageName,
            string Description,
            decimal PricePerPerson,
            List<int> SelectedDestinations,
            List<int> SelectedHotels
        );

        /// <summary>
        /// Get all packages with optional filters.
        /// </summary>

        public static async Task<List<PackageSearchResult>> GetAllPackagesFiltered(
            Config config,
            PackageFilterRequest filter)
        {
            List<PackageSearchResult> result = new();

            string query = """
                SELECT 
                    tp.id,
                    tp.name,
                    tp.description,
                    tp.price_per_person,
                    GROUP_CONCAT(DISTINCT d.city ORDER BY pi.stop_order SEPARATOR ' → ') AS Route,
                    GROUP_CONCAT(DISTINCT c.name SEPARATOR ', ') AS Countries
                FROM trip_packages AS tp
                JOIN package_itineraries AS pi ON tp.id = pi.package_id
                JOIN destinations AS d ON pi.destination_id = d.id
                JOIN countries AS c ON c.id = d.country_id
                JOIN hotels AS h ON d.id = h.destination_id
                WHERE 1 = 1
            """;

            using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand();
            cmd.Connection = conn;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                query += " AND (tp.name LIKE @search OR tp.description LIKE @search OR d.city LIKE @search OR c.name LIKE @search)";
                cmd.Parameters.AddWithValue("@search", "%" + filter.Search + "%");
            }

            if (!string.IsNullOrWhiteSpace(filter.Country))
            {
                query += " AND c.name LIKE @country";
                cmd.Parameters.AddWithValue("@country", "%" + filter.Country + "%");
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query += " AND d.city LIKE @city";
                cmd.Parameters.AddWithValue("@city", "%" + filter.City + "%");
            }

            if (filter.MinStars.HasValue)
            {
                query += " AND h.stars >= @minStars";
                cmd.Parameters.AddWithValue("@minStars", filter.MinStars.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query += " AND tp.price_per_person <= @maxPrice";
                cmd.Parameters.AddWithValue("@maxPrice", filter.MaxPrice.Value);
            }

            query += " GROUP BY tp.id, tp.name, tp.description, tp.price_per_person";
            query += " ORDER BY MIN(c.name) ASC;";
            cmd.CommandText = query;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new PackageSearchResult(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.GetDecimal(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));
            }

            return result;
        }

        public static async Task<IResult> GetPackages(
            Config config,
            string? search = null,
            string? country = null,
            string? city = null,
            int? minStars = null,
            decimal? maxPrice = null)
        {
            var filter = new PackageFilterRequest(search, country, city, minStars, maxPrice);
            return Results.Ok(await GetAllPackagesFiltered(config, filter));
        }

        public static async Task<IResult> GetSuggestedByCountry(Config config, string country)
        {
            await using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();

            const string sql = """
                   SELECT  	
                    h.name AS hotel_name,
                    tp.id,
                    tp.name,
                    tp.description,
                    tp.price_per_person,
                    c.cuisine
                FROM trip_packages tp
                JOIN package_itineraries pi ON tp.id = pi.package_id
                JOIN destinations d ON pi.destination_id = d.id
                JOIN countries c ON d.country_id = c.id
                JOIN hotels AS h ON d.id = h.destination_id
                WHERE c.name = @country;
                """;

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@country", country);

            await using var reader = await cmd.ExecuteReaderAsync();

            var result = new List<object>();

            while (await reader.ReadAsync())
            {
                var hotelname = reader["hotel_name"]?.ToString() ?? "";
                var id = Convert.ToInt32(reader["id"]);
                var name = reader["name"]?.ToString() ?? "";
                var description = reader["description"]?.ToString() ?? "";
                var price = Convert.ToDecimal(reader["price_per_person"]);
                var cuisine = reader["cuisine"]?.ToString() ?? "";

                result.Add(new
                {
                    HotelName = hotelname,
                    PackageId = id,
                    Name = name,
                    Description = description,
                    Price = price,
                    Cuisine = cuisine,
                    SuggestionReason = "Matches selected country and cuisine"
                });
            }

            return Results.Ok(result);
        }


        public static async Task<CustomizedPackage> GetCustomizedPackage(
            Config config,
            int packageId,
            string? destinationIds = null,
            string? hotelIds = null)
        {
            var destinationsList = string.IsNullOrWhiteSpace(destinationIds)
            ? new List<int>()
            : destinationIds.Split(',').Select(int.Parse).ToList();

            var hotelsList = string.IsNullOrWhiteSpace(hotelIds)
            ? new List<int>()
            : hotelIds.Split(',').Select(int.Parse).ToList();


            const string query = """
                SELECT id, name, description, price_per_person
                FROM trip_packages
                WHERE id = @PackageId;
                """;

            await using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();

            await using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PackageId", packageId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new Exception("Package not found");

            return new CustomizedPackage(
                reader.GetInt32(0), // ID
                reader.GetString(1), // Name
                reader.IsDBNull(2) ? "" : reader.GetString(2), // description
                reader.GetDecimal(3), // Price_Per_Person
                destinationsList,
                hotelsList
            );
        }


        public static async Task<IResult> GetFilters(
            Config config,
            string country,
            DateTime checkin,
            DateTime checkout,
            int total_travelers,
            string? city = null,
            string? hotelName = null,
            int? minStars = null,
            double? maxDistanceToCenter = null,
            string? facilities = null)
        {
            List<string>? facilitiesList = null;
            if (!string.IsNullOrWhiteSpace(facilities))
            {
                facilitiesList = facilities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(f => f.Trim())
                                        .ToList();
            }

            var filter = new HotelFilterRequest(
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

            var hotels = await GetAllHotelsFiltered(config, filter);
            return Results.Ok(hotels);
        }


        public static async Task<List<AdminHotelView>> GetAdminView(Config config, HttpContext ctx)
        {
            var result = new List<AdminHotelView>();

            using var conn = new MySqlConnection(config.db);
            await conn.OpenAsync();

            string query = """
                SELECT 
                    id,
                    destination_id,
                    name,
                    description,
                    stars,
                    distance_to_center
                FROM hotels 
            """;

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AdminHotelView(
                    reader.GetInt32(0), // Id
                    reader.GetInt32(1), // DestinationId
                    reader.GetString(2), // Name
                    reader.IsDBNull(3) ? "" : reader.GetString(3), // Description
                    reader.GetInt32(4), // Stars
                    reader.GetDecimal(5) // DistanceToCenter
                ));

            }

            return result;
        }

        public static async Task<IResult> GetHotelByID(Config config, HttpContext ctx, int id)
        {
            int? adminId = ctx.Session.GetInt32("admin_id");
            if (adminId is null)

                return Results.Unauthorized();

            string query = """
            SELECT
            c.name AS Country,
            d.city,
            h.id,
            h.name AS HotelName,
            h.stars,
            h.distance_to_center,
            h.description,
            COUNT(r.room_number) AS TotalRooms,
            GROUP_CONCAT(DISTINCT rt.type_name) AS RoomTypes
            FROM hotels h
            JOIN destinations d ON h.destination_id = d.id
            JOIN countries c ON d.country_id = c.id
            LEFT JOIN rooms r ON r.hotel_id = h.id
            LEFT JOIN room_types rt ON rt.id = r.roomtype_id
            WHERE h.id = @id
            GROUP BY
            c.name, d.city, h.id, h.name, h.stars, h.distance_to_center, h.description;
            """;

            var parameters = new MySqlParameter[]
            {
                new("@id", id)
            };


            var result = new List<HotelByID>();

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
            while (await reader.ReadAsync())
            {
                var hotel = new HotelByID(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetDecimal(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetString(8)
            );

                result.Add(hotel);
            }

            return Results.Ok(result);

        }

        public static async Task<IResult> GetAllTrips(Config config, HttpContext ctx)
        {
            int? adminId = ctx.Session.GetInt32("admin_id");
            if (adminId is null)
            {
                return Results.Unauthorized();
            }

            List<AdminTrips> result = new();

            string query = """
            SELECT
            id,
            name,
            description,
            price_per_person
            FROM
            trip_packages
            """;

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);
            while (await reader.ReadAsync())
            {
                result.Add(new AdminTrips(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3)
                ));
            }

            return Results.Ok(result);
        }


        public static async Task<IResult> GetAllTripsByID(Config config, HttpContext ctx, int id)
        {
            int? adminId = ctx.Session.GetInt32("admin_id");
            if (adminId is null)
            {
                return Results.Unauthorized();
            }

            string query = """
            SELECT
            c.name,
            d.city,
            tp.id,
            tp.name,
            tp.description,
            tp.price_per_person,
            pi.nights
            FROM trip_packages AS tp
            JOIN package_itineraries AS pi ON tp.id = pi.package_id
            JOIN destinations AS d ON pi.destination_id = d.id
            JOIN countries AS c ON d.country_id = c.id
            WHERE tp.id = @id;
            """;

            var parameters = new MySqlParameter[]
            {
                new("@id", id)
            };

            var result = new List<TripByID>();


            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
            while (await reader.ReadAsync())
            {
                result.Add(new TripByID(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDecimal(5),
                    reader.GetInt32(6)
                ));
            }

            return Results.Ok(result);
        }

    }

    

        public static async Task<IResult> GetAllFacilities(Config config, HttpContext ctx)
        {

            int? adminId = ctx.Session.GetInt32("admin_id");
            if (adminId is null)
            {
                return Results.Unauthorized();
            }

            List<AdminFacilities> result = new();

            string query = """
                SELECT
                    id,
                    name
                FROM facilities;
            """;

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);
            while (await reader.ReadAsync())
            {
                result.Add(new AdminFacilities(
                    reader.GetInt32(0), //id
                    reader.GetString(1) // name
                ));
            }

            return Results.Ok(result);
        }
               public static async Task<IResult> GetFacilityByID(Config config, HttpContext ctx, int id)
        {
            int? adminId = ctx.Session.GetInt32("admin_id");
            if (adminId is null) return Results.Unauthorized();
 
            string query = """
            SELECT
            f.id AS FacilityId,
            f.name AS FacilityName,
            h.id AS HotelId,
            h.name AS HotelName,
            h.stars,
            h.distance_to_center,
            d.city,
            c.name AS Country
            FROM facilities f
            LEFT JOIN accommodation_facilities af ON f.id = af.facility_id
            LEFT JOIN hotels h ON af.hotel_id = h.id
            LEFT JOIN destinations d ON h.destination_id = d.id
            LEFT JOIN countries c ON d.country_id = c.id
            WHERE f.id = @id
            ORDER BY h.id;
            """;
 
            var parameters = new MySqlParameter[] { new("@id", id) };
            var result = new List<FacilityByID>();
 
            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
            while (await reader.ReadAsync())
            {
                result.Add(new FacilityByID(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetDecimal(5),
                    reader.GetString(6),
                    reader.GetString(7)
                ));
            }
 
            return Results.Ok(result);
        }
 
 
    }
       
}