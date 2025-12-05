namespace server;

using MySql.Data.MySqlClient;

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
        int BookingID,
        int UserID,
        int PackageID,
        DateTime Checkin,
        DateTime Checkout,
        int NumberOfTravelers,
        BookingStatus Status
    );

    public static async Task<List<GetAll_Data>> GetAll(Config config)
    {
        List<GetAll_Data> result = new();

        string query = """
            SELECT BookingID, UserID, PackageID, Checkin, Checkout, NumberOfTravelers, Status
            FROM bookings;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                int bookingId = reader.GetInt32(0);
                int userId = reader.GetInt32(1);
                int packageId = reader.GetInt32(2);
                DateTime checkin = reader.GetDateTime(3);
                DateTime checkout = reader.GetDateTime(4);
                int numberOfTravelers = reader.GetInt32(5);

                // MySQL ENUM comes back as string
                string statusString = reader.GetString(6);

                // Parse to enum bookingstatus (enum defined at the top)
                BookingStatus status = Enum.Parse<BookingStatus>(statusString, ignoreCase: true);

                result.Add(new GetAll_Data(
                    bookingId,
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
}