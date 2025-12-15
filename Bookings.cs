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
    public record GetAll_Data(
        int Id,
        int UserId,
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        BookingStatus Status
    );

        public record GetAllData(
        string user,
        int UserId,
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        BookingStatus Status
    );

    // DTO FOR POST BOOKINGS (doesn't take in id or userID)
    public record Post_Args(
     int PackageId,
     DateTime Checkin,
     DateTime Checkout,
     int NumberOfTravelers,
     string Status // in this record, status is defined as string. this is to be able to handle it inside post
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
            SELECT CONCAT(u.first_name, ' ', u.last_name) AS User, user_id, package_id, checkin, checkout, number_of_travelers, status
            FROM bookings AS b
            JOIN users AS u ON b.user_id = u.id;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                string user = reader.GetString(0);
                int userId = reader.GetInt32(1);
                int packageId = reader.GetInt32(2);
                DateTime checkin = reader.GetDateTime(3);
                DateTime checkout = reader.GetDateTime(4);
                int numberOfTravelers = reader.GetInt32(5);
                string statusString = reader.GetString(6);

                BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

                result.Add(new GetAllData(
                    user,
                    userId,
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

    // POST BOOKING 
    public static async Task<IResult> Post(Post_Args body, Config config, HttpContext ctx)
    {
        // 1. must be logged in
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        // 2. parse status string to enum
        if (!Enum.TryParse<BookingStatus>(body.Status, ignoreCase: true, out var statusEnum))
        {
            return Results.BadRequest(new { error = "Invalid booking status." });
        }

        // 3. Insert booking using session userId
        const string insertQuery = """
        INSERT INTO bookings (user_id, package_id, checkin, checkout, number_of_travelers, status)
        VALUES (@user_id, @package_id, @checkin, @checkout, @number_of_travelers, @status);
    """;

        var insertParams = new MySqlParameter[]
        {
        new("@user_id", userId.Value),
        new("@package_id", body.PackageId),
        new("@checkin", body.Checkin),
        new("@checkout", body.Checkout),
        new("@number_of_travelers", body.NumberOfTravelers),
        new("@status", statusEnum.ToString().ToLower())
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, insertQuery, insertParams);

        // 4. Retrieve last inserted ID
        string idQuery = "SELECT LAST_INSERT_ID();";
        object? scalar = await MySqlHelper.ExecuteScalarAsync(config.db, idQuery);

        if (scalar != null && scalar != DBNull.Value)
        {
            int newId = Convert.ToInt32(scalar);

            return Results.Ok(new
            {
                id = newId,
                message = "Booking created successfully."
            });
        }

        return Results.Problem("Could not retrieve booking ID after insert.");
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

    public record Receipt(
        int booking,
        string name,
        string package,
        int travelers,
        decimal price_per_person,
        int nights,
        decimal total
    );

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
    public record Put_Booking(      
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        string Status);

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

}
