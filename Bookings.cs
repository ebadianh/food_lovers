using MySql.Data.MySqlClient;

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

        // DTO FOR POST BOOKINGS (doesn't take in id or userID)
       public record Post_Args(
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        string Status // in this record, status is defined as string. this is to be able to handle it inside post
    );

    // GET ALL BOOKINGS
    public static async Task<List<GetAll_Data>> GetAll(Config config)
    {

        List<GetAll_Data> result = new();

        string query = """
            SELECT id, user_id, package_id, checkin, checkout, number_of_travelers, status
            FROM bookings;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                int userId = reader.GetInt32(1);
                int packageId = reader.GetInt32(2);
                DateTime checkin = reader.GetDateTime(3);
                DateTime checkout = reader.GetDateTime(4);
                int numberOfTravelers = reader.GetInt32(5);
                string statusString = reader.GetString(6);

                BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

                result.Add(new GetAll_Data(
                    id,
                    userId,
                    packageId,
                    checkin,
                    checkout,
                    numberOfTravelers,
                    status
                ));
            }
        }

        return result;
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




   

}
