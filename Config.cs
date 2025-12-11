namespace server
{
    public class Config
    {
        public string db { get; set; }

        public Config(string connectionString)
        {
            db = connectionString;
        }
    }
}
