using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace BAR_OF_CHANGE
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // проверка на наличие токена
            if (System.IO.File.Exists("session.txt"))
            {
                string sessionToken = System.IO.File.ReadLines("session.txt").First();
                string connectionString = "Host=localhost;Username=postgres;Password=71594199p;Database=postgres";


                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT user_id FROM users WHERE session_token = @sessionToken";

                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sessionToken", sessionToken);
                        NpgsqlDataReader reader = command.ExecuteReader();

                        if (reader.HasRows)
                        {

                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                        }
                        else
                        {

                            LoginWindow loginWindow = new LoginWindow();
                            loginWindow.Show();
                        }
                    }
                }
            }
            else
            {
                // Если файла токена нет, открыть окно входа
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
            }
        }


        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
    }

}
