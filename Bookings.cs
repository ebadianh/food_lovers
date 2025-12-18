using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;

namespace server;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

class Bookings
{

    // DTO FOR GET ALL BOOKINGS ENDPOINT
    public record GetAllData(
             int BookingId,
             DateTime TripStartDate,
             DateTime TripEndDate,
             int NumberOfTravelers,
             BookingStatus Status,
             int StopOrder,
             DateTime Checkin,
             DateTime Checkout,
             string City,
             string HotelName,
             int RoomNumber,
             string RoomType,
             decimal PricePerNight
     );
    // DTO FOR GET ALL PACKAGES FOR USER ENDPOINT
    public record Get_All_Packages_For_User(
        int BookingId,
        int UserId,
        int PackageId,
        int StopOrder,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        BookingStatus Status,
        string City,
        string HotelName
    );
    // DTO FOR POST BOOKINGS (doesn't take in id or userID, and auto sets status to pending)
    public record RoomSelection(
    int RoomNumber,
    decimal PricePerNight
    );

    public record StopSelection(
        int StopOrder,
        int HotelId,
        DateTime Checkin,
        DateTime Checkout,
        List<RoomSelection> Rooms
    );
    public record Post_Args(
        int PackageId,
        DateTime TripStart,
        DateTime TripEnd,
        int NumberOfTravelers,
        List<StopSelection> Stops
    );

    public record Receipt(
        int booking,
        string name,
        string package,
        int travelers,
        decimal price_per_person,
        int nights,
        decimal total
    );

    public record Put_Booking(
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        string Status);

    public record BookingDetails_Data(
        int Id,
        string Status,
        string PackageName,
        string Country,
        string City,
        string HotelName,
        decimal PricePerPerson
    );





    // GET ALL BOOKINGS
    public static async Task<IResult> GetAll(Config config, HttpContext ctx)
    {
        int? adminId = ctx.Session.GetInt32("admin_id");
        if (adminId is null)
        {
            return Results.Unauthorized();
        }


        List<GetAllData> result = new();

        string query = """
            SELECT 
                b.id as booking_id,
                b.trip_start_date,
                b.trip_end_date,
                b.number_of_travelers,
                b.status,
                bs.stop_order,
                bs.checkin,
                bs.checkout,
                d.city,
                h.name as hotel_name,
                br.room_number,
                rt.type_name as room_type,
                br.price_per_night
            FROM bookings b
            JOIN booking_stops bs ON b.id = bs.booking_id
            JOIN hotels h ON bs.hotel_id = h.id
            JOIN destinations d ON h.destination_id = d.id
            JOIN booked_rooms br ON b.id = br.booking_id AND bs.stop_order = br.stop_order
            JOIN rooms r ON br.hotel_id = r.hotel_id AND br.room_number = r.room_number
            JOIN room_types rt ON r.roomtype_id = rt.id
            ORDER BY bs.stop_order, br.room_number;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                int bookingId = reader.GetInt32(0);
                DateTime tripStartDate = reader.GetDateTime(1);
                DateTime tripEndDate = reader.GetDateTime(2);
                int travelers = reader.GetInt32(3);
                string statusString = reader.GetString(4);
                int stopOrder = reader.GetInt32(5);
                DateTime checkin = reader.GetDateTime(6);
                DateTime checkout = reader.GetDateTime(7);
                string city = reader.GetString(8);
                string hotelName = reader.GetString(9);
                int roomNumber = reader.GetInt32(10);
                string roomType = reader.GetString(11);
                decimal pricePerNight = reader.GetDecimal(12);


                BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

                result.Add(new GetAllData(
                    bookingId,
                    tripStartDate,
                    tripEndDate,
                    travelers,
                    status,
                    stopOrder,
                    checkin,
                    checkout,
                    city,
                    hotelName,
                    roomNumber,
                    roomType,
                    pricePerNight
                ));
            }
        }

        return Results.Ok(result);
    }

    // POST BOOKING 
    public static async Task<IResult> Post(Post_Args body, Config config, HttpContext ctx)
    {
        // 1. must be logged in
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        using var conn = new MySqlConnection(config.db);
        await conn.OpenAsync();

        using var bookingTransaction = await conn.BeginTransactionAsync();
        try
        {
            const string insertBooking = """
            INSERT INTO bookings (user_id, package_id, trip_start_date, trip_end_date, number_of_travelers, status)
            VALUES (@user_id, @package_id, @trip_start, @trip_end, @num_travelers, 'pending');
            """;

            var bookingCmd = new MySqlCommand(insertBooking, conn, (MySqlTransaction)bookingTransaction);
            bookingCmd.Parameters.AddRange(new[]
            {
            new MySqlParameter("@user_id", userId.Value),
            new MySqlParameter("@package_id", body.PackageId),
            new MySqlParameter("@trip_start", body.TripStart),
            new MySqlParameter("@trip_end", body.TripEnd),
            new MySqlParameter("@num_travelers", body.NumberOfTravelers)
            });

            await bookingCmd.ExecuteNonQueryAsync();
            long bookingId = bookingCmd.LastInsertedId;

            string insertStop = """
            INSERT INTO booking_stops (booking_id, stop_order, hotel_id, checkin, checkout)
            VALUES (@booking_id, @stop_order, @hotel_id, @checkin, @checkout);
            """;
            string insertRoom = """
            INSERT INTO booked_rooms (booking_id, stop_order, hotel_id, room_number, price_per_night)
            VALUES (@booking_id, @stop_order, @hotel_id, @room_number, @price);
            """;

            foreach (var stop in body.Stops)
            {
                // Insert stop
                var stopCmd = new MySqlCommand(insertStop, conn, (MySqlTransaction)bookingTransaction);
                stopCmd.Parameters.AddRange(new[]
                {
                    new MySqlParameter("@booking_id", bookingId),
                    new MySqlParameter("@stop_order", stop.StopOrder),
                    new MySqlParameter("@hotel_id", stop.HotelId),
                    new MySqlParameter("@checkin", stop.Checkin),
                    new MySqlParameter("@checkout", stop.Checkout)
                });
                await stopCmd.ExecuteNonQueryAsync();

                foreach (var r in stop.Rooms)
                {
                    var roomCmd = new MySqlCommand(insertRoom, conn, (MySqlTransaction)bookingTransaction);
                    roomCmd.Parameters.AddRange(new[]
                    {
                        new MySqlParameter("@booking_id", bookingId),
                        new MySqlParameter("@stop_order", stop.StopOrder),
                        new MySqlParameter("@hotel_id", stop.HotelId),
                        new MySqlParameter("@room_number", r.RoomNumber),
                        new MySqlParameter("@price", r.PricePerNight)
                    });
                    await roomCmd.ExecuteNonQueryAsync();
                }
            }
            await bookingTransaction.CommitAsync();
            return Results.Ok(new { bookingId });
        }
        catch
        {
            await bookingTransaction.RollbackAsync();
            throw;
        }
    }

    public static async Task<IResult> Delete(int id, HttpContext ctx, Config config)
    {
        // 1. must be logged in
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        // 2. check if booking exists AND belongs to user
        string checkQuery = "SELECT user_id FROM bookings WHERE id = @id;";
        var checkParams = new MySqlParameter[] { new("@id", id) };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, checkQuery, checkParams))
        {
            if (!reader.Read())
            {
                return Results.NotFound(new { error = "Booking not found." });
            }

            int ownerId = reader.GetInt32(0);

            if (ownerId != userId.Value)
            {
                return Results.Forbid();
            }
        }

        // 3. DELETE booking
        string deleteQuery = "DELETE FROM bookings WHERE id = @id;";
        var deleteParams = new MySqlParameter[] { new("@id", id) };

        int affected = await MySqlHelper.ExecuteNonQueryAsync(config.db, deleteQuery, deleteParams);

        if (affected == 0)
        {
            return Results.NotFound(new { error = "Booking could not be deleted." });
        }

        // 4. return SUCCESS MESSAGE
        return Results.Ok(new { message = "Booking deleted successfully." });
    }
    public static async Task<IResult> GetAllPackagesForUser(Config config, HttpContext ctx)
    {
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
            return Results.Unauthorized();

        List<Get_All_Packages_For_User> result = new();

        const string query = """
                SELECT 
                    b.id,
                    b.user_id,
                    b.package_id,
                    bs.stop_order,
                    bs.checkin,
                    bs.checkout,
                    b.number_of_travelers,
                    b.status,
                    d.city,
                    h.name as hotel_name
                FROM bookings b
                JOIN booking_stops bs ON b.id = bs.booking_id
                JOIN hotels h ON bs.hotel_id = h.id
                JOIN destinations d ON h.destination_id = d.id
                WHERE b.user_id = @user_id
                ORDER BY b.id, bs.stop_order;
            """;

        var parameters = new MySqlParameter[]
            {
                    new("@user_id", userId.Value)
            };

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
        while (await reader.ReadAsync())
        {
            int bookingId = reader.GetInt32(0);
            int user_Id = reader.GetInt32(1);
            int packageId = reader.GetInt32(2);
            int stopOrder = reader.GetInt32(3);
            DateTime checkin = reader.GetDateTime(4);
            DateTime checkout = reader.GetDateTime(5);
            int numberOfTravelers = reader.GetInt32(6);
            string statusString = reader.GetString(7);
            string city = reader.GetString(8);
            string hotelName = reader.GetString(9);
            BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

            result.Add(new Get_All_Packages_For_User(
                bookingId,
                user_Id,
                packageId,
                stopOrder,
                checkin,
                checkout,
                numberOfTravelers,
                status,
                city,
                hotelName
            ));
        }

        return Results.Ok(result);
    }

    public static async Task<List<Receipt>?> GetTotalCostByBooking(Config config, HttpContext ctx, int bookingsId)
    {

        List<Receipt> result = new();

        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
        {
            return null;
        }

        string query = """
            SELECT
            booking,
            name,
            package,
            travelers,
            price_per_person,
            nights,
            total
            FROM receipt
            WHERE booking = @bookingsid
        """;

        var parameters = new MySqlParameter[]
        {
            new("@bookingsid", bookingsId)
        };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            while (reader.Read())
            {

                int booking = reader.GetInt32(0);
                string name = reader.GetString(1);
                string package = reader.GetString(2);
                int travelers = reader.GetInt32(3);
                decimal price_per_person = reader.GetDecimal(4);
                int nights = reader.GetInt32(5);
                decimal total = reader.GetDecimal(6);

                result.Add(new Receipt(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetDecimal(4),
                    reader.GetInt32(5),
                    reader.GetDecimal(6)
                ));
            }
        }

        return result;
    }

    public static async Task<IResult> Put(int bookingId, Put_Booking body, Config config, HttpContext ctx)
    {

        int? userId = ctx.Session.GetInt32("user_id");
        if (userId == null)
        {
            return Results.Unauthorized();
        }

        string query = """
        UPDATE bookings 
        SET package_id = @package_id, 
        checkin = @checkin, 
        checkout = @checkout, 
        number_of_travelers = 
        @number_of_travelers, 
        status = @status 
        WHERE id = @id AND user_id = @user_id
     """;

        var parameters = new MySqlParameter[]
        {
        new("@package_id", body.PackageId),
        new("@checkin", body.Checkin),
        new("@checkout", body.Checkout),
        new("@number_of_travelers", body.NumberOfTravelers),
        new("@status", body.Status),
        new("@id", bookingId),
        new("@user_id", userId.Value)
        };

        int rows = await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

        if (rows == 0)
        {
            return Results.NotFound("Booking not found or not owned by user");
        }

        return Results.NoContent();
    }




    // definerar datan som ska visas till user

    public static async Task<IResult> GetDetails(int id, Config config)
    {
        // förbered en lista för att hålla resultat 
        var details = new List<BookingDetails_Data>();

        using var conn = new MySqlConnection(config.db);
        await conn.OpenAsync();

        string query = """
                SELECT 
                b.id, 
                b.status,
                tp.name, 
                c.name, 
                d.city, 
                h.name, 
                tp.price_per_person
                FROM bookings b 
                JOIN trip_packages tp ON b.package_id = tp.id
                JOIN stops s ON tp.id = s.package_id
                JOIN destinations d ON s.destination_id = d.id  
                JOIN countries c ON d.country_id = c.id
                JOIN hotels h ON d.id = h.destination_id
                WHERE b.id = @id
                """;

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();

        while (reader.Read())
        {
            details.Add(new BookingDetails_Data(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetDecimal(6)
            ));
        }

        // if sats om listan är tom 
        if (details.Count == 0)
        {
            return Results.NotFound(new { error = "Booking not found." });
        }

        return Results.Ok(details);
    }

}

