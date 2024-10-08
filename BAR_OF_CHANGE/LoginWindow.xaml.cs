using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BAR_OF_CHANGE
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }


        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика для регистрации
            string first_name = FirstNameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            using (NpgsqlConnection conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=71594199p;Database=postgres"))
            {
                conn.Open();

                // Проверка уникальности имени
                string checkQuery = "SELECT COUNT(*) FROM users WHERE first_name = @first_name";
                using (NpgsqlCommand checkCmd = new NpgsqlCommand(checkQuery, conn))
                {

                    checkCmd.Parameters.AddWithValue("first_name", first_name);

                    // Приведение результата к long
                    long userCount = (long)checkCmd.ExecuteScalar();

                    if (userCount > 0) // Если хотя бы один пользователь с таким именем есть
                    {
                        // Имя уже существует
                        MessageBox.Show("Пользователь с таким именем уже существует.");
                        return;
                    }
                }

                // Если имя уникальное, продолжаем регистрацию
                string query = "INSERT INTO users (first_name, email, password) VALUES (@first_name, @email, @password)";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("first_name", first_name);
                    cmd.Parameters.AddWithValue("email", email);
                    cmd.Parameters.AddWithValue("password", password);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Пользователь успешно зарегистрирован.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string first_name = FirstNameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            string connectionString = "Host=localhost;Username=postgres;Password=71594199p;Database=postgres";

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT user_id FROM users WHERE first_name = @first_name AND email = @email AND password = @password";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@first_name", first_name);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);

                    NpgsqlDataReader reader = cmd.ExecuteReader();


                    if (reader.HasRows)
                    {
                        reader.Close();

                        // Генерация уникального токена
                        string sessionToken = Guid.NewGuid().ToString();

                        // Сохранение токена в базу данных
                        string updateTokenQuery = "UPDATE users SET session_token = @sessionToken WHERE first_name = @first_name AND email = @email";
                        using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateTokenQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("@sessionToken", sessionToken);
                            updateCommand.Parameters.AddWithValue("@first_name", first_name);
                            updateCommand.Parameters.AddWithValue("@email", email);
                            updateCommand.ExecuteNonQuery();
                        }

                        string queryid = "SELECT user_id FROM users WHERE first_name = @first_name";
                        using (NpgsqlCommand cmdid = new NpgsqlCommand(queryid, connection))
                        {
                            cmdid.Parameters.AddWithValue("@first_name", first_name);
                            object result = cmdid.ExecuteScalar();
                            int userId = Convert.ToInt32(result);
                            System.IO.File.WriteAllText("session.txt",$"{sessionToken}\n{userId}");
                        }


                        

                        // Открытие главного окна
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Неверные данные");
                    }
                }
            }
        }

    }
}
