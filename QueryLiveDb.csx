using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "Server=db41223.public.databaseasp.net;Database=db41223;User Id=db41223;Password=C!r5%8ZwE@q4;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string sql = "SELECT TOP 10 Action, Description, Timestamp FROM AuditLogs WHERE Action LIKE '%Webhook%' ORDER BY Timestamp DESC";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    bool found = false;
                    while (reader.Read())
                    {
                        found = true;
                        Console.WriteLine($"\n[{reader.GetDateTime(2).ToString("O")}] {reader.GetString(0)}");
                        Console.WriteLine(reader.GetString(1));
                        Console.WriteLine("--------------------------------------------------");
                    }
                    if (!found)
                    {
                        Console.WriteLine("No webhook logs found in the last entries.");
                    }
                }
            }
        }
    }
}
