namespace server;

using MySql.Data.MySqlClient;

class Login
{
    public static bool
    Get(HttpContext ctx)
    {
        bool result = false;
        if (ctx.Session.IsAvailable)
        {
            result = ctx.Session.Keys.Contains("user_id");
        }
        return result;
    }

    public static bool
    GetAdmin(HttpContext ctx)
    {
        bool result = false;
        if (ctx.Session.IsAvailable)
        {
            result = ctx.Session.Keys.Contains("admin_id");
        }
        return result;
    }
    

public record Post_Data(string Email, string Password);

    public static async Task<bool> Post(Post_Data credentials, Config config, HttpContext ctx)
    {
        bool result = false;

        string query = "SELECT id FROM users WHERE email = @email AND password = @password";

        var parameters = new MySqlParameter[]
        {
            new("@email", credentials.Email),
            new("@password", credentials.Password),
        };

        object? query_result = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);

        // small change: convert to int
        if (query_result != null && query_result != DBNull.Value)
        {
            int id = Convert.ToInt32(query_result); //  can handle int, long etc
            ctx.Session.SetInt32("user_id", id);
            result = true;
        }

        return result;
    }

public record Post_A_Data(string Email, string Password);

        public static async Task<bool> PostAdmin(Post_A_Data credentials, Config config, HttpContext ctx)
    {
        bool result = false;

        string query = "SELECT id FROM admins WHERE email = @email AND password = @password";

        var parameters = new MySqlParameter[]
        {
            new("@email", credentials.Email),
            new("@password", credentials.Password),
        };

        object? query_result = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);

        // small change: convert to int
        if (query_result != null && query_result != DBNull.Value)
        {
            int id = Convert.ToInt32(query_result); //  can handle int, long etc
            ctx.Session.SetInt32("admin_id", id);
            result = true;
        }

        return result;
    }


    public static void Delete(HttpContext ctx)
    {
        ctx.Session.Clear();
    }

}