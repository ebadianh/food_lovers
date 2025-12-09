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
    public record GetAll_Data(
        int Id,
        int UserId,
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        BookingStatus Status
    );

    public record Post_Args(
        int UserId,
        int PackageId,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        string Status // in this record, status is defined as string. this is to be able to handle it inside post
    );

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

    public static async Task<IResult> Post(Config config, Post_Args body) // iresult for custom status codes
    {
        // Try to parse the incoming status string to BookingStatus (case-insensitive)
        if (!Enum.TryParse<BookingStatus>(body.Status, ignoreCase: true, out var statusEnum))
        {
            return Results.BadRequest("Invalid status. Allowed values: pending, confirmed, cancelled, completed."); // exception handling for status. needs to be in certain formats.
        }

        string query = """
            INSERT INTO bookings (user_id, package_id, checkin, checkout, number_of_travelers, status)
            VALUES (@user_id, @package_id, @checkin, @checkout, @number_of_travelers, @status);
        """;

        var parameters = new MySqlParameter[]
        {
            new("@user_id", body.UserId),
            new("@package_id", body.PackageId),
            new("@checkin", body.Checkin),
            new("@checkout", body.Checkout),
            new("@number_of_travelers", body.NumberOfTravelers),
            // DB expects lowercase enum: 'pending', 'confirmed' etc
            new("@status", statusEnum.ToString().ToLower())
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

        // Get the newly created id
        string lastIdQuery = "SELECT LAST_INSERT_ID();";
        object? scalar = await MySqlHelper.ExecuteScalarAsync(config.db, lastIdQuery);
        int newId = Convert.ToInt32(scalar);

        return Results.Created($"/bookings/{newId}", new { id = newId });
    }
}
