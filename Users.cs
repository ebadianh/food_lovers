using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
namespace server;

// SELECT, reader
// INSERT, parameters
class Users

{
    static List<User> users = new();
    public record Get_Data(int id, string Email, string Password);
    //List<Get_Data> -> asynd Task<List<Get_Data>> Get()
    public static async Task<IResult> Get(Get_Data body, Config config, HttpContext ctx)
    {
        int? adminId = ctx.Session.GetInt32("admin_id");
         if (adminId is null)
    {
        return Results.Unauthorized();
    }

        List<Get_Data> result = new();

        string query = "SELECT id, email, password FROM users";

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }
        }
        return Results.Ok(result);
    }
    public record GetById_Data(string Email);
    public static async Task<GetById_Data?> GetById(int id, Config config) //En del av vår path
    {
        GetById_Data? result = null;
        string query = "SELECT email FROM users WHERE id = @id";
        var parameters = new MySqlParameter[]
        {
            new("@id", id)
        };
        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            if (reader.Read())
            {
                result = new(reader.GetString(0));
            }
        }
        return result;
    }

    public record Post_Args(string Firstname, string Lastname, string Email, string Password); // har vi en void så blir det async task
    public static async Task Post(Post_Args body, Config config)
    {
        string query = "INSERT INTO users(first_name, last_name, email, password) VALUES(@firstname, @lastname, @email, @password)";

        var parameters = new MySqlParameter[]
    {
        new("@firstname", body.Firstname),
        new("@lastname", body.Lastname),
        new("@email", body.Email),
        new("@password", body.Password)
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }

    public static async Task Delete(int id, Config config)
    {
        string query = "DELETE FROM users WHERE id = @id";
        var parameters = new MySqlParameter[] { new("@id", id) };
        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }
    public record Put_Args(int Id, string Email, string Password);

    public static async Task Put(Put_Args user, Config config)
    {
        string query = """
        UPDATE users 
        SET email = @email, password = @password 
        WHERE id = @id
    """;

        var parameters = new MySqlParameter[]
        {
        new("@id", user.Id),
        new("@email", user.Email),
        new("@password", user.Password),
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }

}
record User(string Firstname, string Lastname, string Email, string Password);

