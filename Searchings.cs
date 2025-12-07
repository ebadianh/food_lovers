namespace server;
using MySql.Data.MySqlClient;

class Searchings
{
    public record GetAll_Data
    (
        string TripPackage,
        string Country,
        string City,
        string HotelName,
        int RoomCapacity,
        int Stars,
        decimal PackagePrice
 
    );
    public static async Task<List<GetAll_Data>> GetAllPackages(Config config)
    {
        List<GetAll_Data> result = new();

        string query = """
            SELECT tp.PackageName AS TripPackage, c.Country, d.City, h.HotelName, r.Capacity AS RoomCapacity, h.Stars, tp.PricePerPerson AS PackagePrice
            FROM TRIPPACKAGES AS tp
            
            JOIN PACKAGEITINERARY AS pi ON tp.PackageID = pi.PackageID
            JOIN DESTINATIONS AS d On pi.DestinationID = d.DestinationID
            JOIN HOTELS AS h ON d.DestinationID = h.DestinationID
            JOIN COUNTRY AS c ON c.CountryID = d.CountryID
            JOIN ROOMS AS r ON h.HotelID = r.HotelID
            
            ORDER BY tp.PackageName ASC 

            ;
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                

                result.Add(new GetAll_Data(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetDecimal(6)
                ));
            }
        }

        return result;
    }

}