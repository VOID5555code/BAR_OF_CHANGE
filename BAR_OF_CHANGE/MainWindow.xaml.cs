using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;



namespace BAR_OF_CHANGE
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string connectionString = "Host=localhost;Username=postgres;Password=71594199p;Database=postgres";
        public MainWindow()
        {
            InitializeComponent();
            UserDataLoad();   
        }


        private void IncreaseProgress(object sender, RoutedEventArgs e)
        {
            if (progressBarMain.Value < progressBarMain.Maximum)
            {
                progressBarMain.Value += 5;
                UpdateColor();

                // Обновляем значение в базе данных
                UpdateProgressInDatabase((int)progressBarMain.Value);
            }
        }

        private void UpdateProgressInDatabase(int progressValue)
        {
            string connectionString = "Host=localhost;Username=postgres;Password=71594199p;Database=postgres";
            var lines = System.IO.File.ReadAllLines("session.txt");
            string userId = lines[1];

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE users SET Bar = @progress WHERE user_id = @user_id";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@progress", progressValue);
                    cmd.Parameters.AddWithValue("@user_id", Convert.ToInt32(userId));

                    try
                    {
                        cmd.ExecuteNonQuery();
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating progress: {ex.Message}");
                    }
                }
            }
        }


        private void DecreaseProgress(object sender, RoutedEventArgs e)
        {
            if (progressBarMain.Value < progressBarMain.Maximum)
            {
                progressBarMain.Value -= 5;
                UpdateColor();

                // Обновляем значение в базе данных
                UpdateProgressInDatabase((int)progressBarMain.Value);
            }
        }

        private void UpdateColor()
        {
            // Пастельный красный (228, 113, 122)
            byte redR = 255;
            byte redG = 30;
            byte redB = 30;

            // Синий (127, 199, 255)
            byte blueR = 127;
            byte blueG = 199;
            byte blueB = 255;

            // Пастельный зеленый (119, 221, 119)
            byte greenR = 119;
            byte greenG = 221;
            byte greenB = 119;

            double percentage = progressBarMain.Value / progressBarMain.Maximum;

            byte r, g, b;

            if (percentage <= 0.5)
            {
                double scale = percentage / 0.5;
                r = (byte)((blueR - redR) * scale + redR);
                g = (byte)((blueG - redG) * scale + redG);
                b = (byte)((blueB - redB) * scale + redB);
            }
            else
            {
                double scale = (percentage - 0.5) / 0.5;
                r = (byte)((greenR - blueR) * scale + blueR);
                g = (byte)((greenG - blueG) * scale + blueG);
                b = (byte)((greenB - blueB) * scale + blueB);
            }

            progressBarMain.Foreground = new SolidColorBrush(Color.FromRgb(r, g, b));
        }


        private void CreatePinNote(string title, string text_note, int IdNote, bool task)
        {
            TextBlock newNotePinned = new TextBlock();
            Run boldText = new Run(title) { FontWeight = FontWeights.Bold };

            newNotePinned.Inlines.Add(boldText);

            newNotePinned.Inlines.Add(new LineBreak());

            Run normalText = new Run(text_note);
            newNotePinned.Inlines.Add(normalText);




            newNotePinned.Style = (Style)FindResource("NoteTextBlockStyle");


            Image ImageFix = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/image/FixNote.png"))
            };


            Button ButtonUnFix = new Button
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(0),
                Content = ImageFix,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            ButtonUnFix.Click += (s, ea) =>
            {
                UnFixed(s, ea);
            };

            Grid noteGrid = new Grid();
            noteGrid.ColumnDefinitions.Add(new ColumnDefinition());

            Grid.SetColumn(newNotePinned, 0);
            Grid.SetColumn(ButtonUnFix, 0);

            noteGrid.Children.Add(newNotePinned);
            noteGrid.Children.Add(ButtonUnFix);

            if (task == true)
            {
                CheckBox completeTaskCheckBox = new CheckBox
                {
                    Style = (Style)FindResource("CheckBoxTask"),
                    Margin = new Thickness(0,0,60,0)
                    
                };

                completeTaskCheckBox.Checked += (s, e) =>
                {
                    noteGrid.Opacity = 0.5;
                };

                completeTaskCheckBox.Unchecked += (s, e) =>
                {
                    noteGrid.Opacity = 1.0;
                };

                Grid.SetColumn(completeTaskCheckBox, 0);
                noteGrid.Children.Add(completeTaskCheckBox);
            }


            Border noteBorder = new Border
            {
                Tag = IdNote,
                Child = noteGrid
            };
            noteBorder.Style = (Style)FindResource("PinnedNote");


            PinnedPanel.Children.Add(noteBorder);
        }
        private void AddPinNote_Click(object sender, RoutedEventArgs e)
        {
            AddPinNote_Click(sender, e, true, false);
        }
        private void AddPinNote_Click(object sender, RoutedEventArgs e, bool pinned, bool task)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                string sessionFile = "session.txt";
                string[] sessionData = System.IO.File.ReadAllLines(sessionFile);
                string userId = sessionData[1];
                conn.Open();
                string title = Note_Title.Text;
                string noteText = Note_Box.Text;
                string query = "INSERT INTO notes (user_id, title, note_text, pin, task) VALUES (@user_id, @title, @note_text, @pinned, @task) RETURNING id_note";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("user_id", int.Parse(userId));
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("note_text", noteText);
                    cmd.Parameters.AddWithValue("pinned", pinned);
                    cmd.Parameters.AddWithValue("task", task);

                    try
                    {
                        int idNote = (int)cmd.ExecuteScalar();
                        CreateNote(title, noteText, idNote);
                        CreatePinNote(title, noteText, idNote,task);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении заметки: {ex.Message}");
                    }
                }
            }
            
            
        }

        private void UnFixed(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid noteGrid = button.Parent as Grid;
            Border noteBorder = noteGrid.Parent as Border;

            int noteId = (int)noteBorder.Tag;

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "UPDATE notes SET pin = false WHERE id_note = @noteId";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@noteId", noteId);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при обновлении заметки: {ex.Message}");
                    }
                }
            }

            PinnedPanel.Children.Remove(noteBorder);
        }

        private void NotesPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;

            if (scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 100)
            {
                LoadMoreNotes(); 
            }
        }

        private void LoadMoreNotes()
        {
            
            for (int i = 0; i < 10; i++) 
            {
                CreateNote($"Заметка {i + 1}", "Текст заметки", i);
            }
        }


        private void SaveTask(object sender, RoutedEventArgs e)
        {
            SaveNote(sender, e, false, true);
        }

        private void SaveNote(object sender, RoutedEventArgs e)
        {
            SaveNote(sender, e, false, false);
        }

        private void SaveNote(object sender, RoutedEventArgs e, bool pinned, bool isTask)
        {
            string title = Note_Title.Text;
            string noteText = Note_Box.Text;
            DateTime date = TaskDueDatePicker.DisplayDate;
            string sessionFile = "session.txt";
            string[] sessionData = System.IO.File.ReadAllLines(sessionFile);
            string userId = sessionData[1];

            if (string.IsNullOrEmpty(userId))
            {
                MessageBox.Show("Ошибка: не удалось получить идентификатор пользователя.");
                return;
            }

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO notes (user_id, title, note_text, pin, task) VALUES (@user_id, @title, @note_text, @pinned, @task) RETURNING id_note";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("user_id", int.Parse(userId));
                    cmd.Parameters.AddWithValue("title", title);
                    cmd.Parameters.AddWithValue("note_text", noteText);
                    cmd.Parameters.AddWithValue("pinned", pinned);
                    cmd.Parameters.AddWithValue("task", isTask);

                    try
                    {
                        int idNote = (int)cmd.ExecuteScalar();
                        if(isTask == true)
                        {
                            CreateTask(title, noteText, idNote, date);
                        }
                        else
                        {
                            CreateNote(title, noteText, idNote);
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении заметки: {ex.Message}");
                    }
                }
            }
        }


        private void DeleteNote(object sender, RoutedEventArgs e)
        {
            Button deleteButton = sender as Button;
            Grid noteGrid = deleteButton.Parent as Grid;
            Button noteButton = noteGrid.Children.OfType<Button>().FirstOrDefault(btn => Grid.GetColumn(btn) == 0);
            int noteId = (int)noteButton.Tag;


            string connectionString = "Host=localhost;Username=postgres;Password=71594199p;Database=postgres";
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM notes WHERE id_note = @noteId";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@noteId", noteId);

                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении заметки: {ex.Message}");
                    }
                }
            }

            // Удаление заметки с интерфейса
            NotesPanel.Children.Remove(noteGrid);
        }

        private void CreateTask(string title, string taskText, int idTask, DateTime? date = null)
        {

            TextBlock taskTitle = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 30
            };


            TextBlock taskBody = new TextBlock
            {
                Text = taskText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 18
            };

            StackPanel taskPanel = new StackPanel();
            taskPanel.Children.Add(taskTitle);
            taskPanel.Children.Add(taskBody);

            Grid taskGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });


            Button taskButton = new Button
            {
                Content = taskPanel,
                Style = (Style)FindResource("Note"),
                Tag = idTask
            };

            taskButton.Click += (s, e) =>
            {
                OpenTaskForEditing(title, taskText, idTask, date);
            };

            Image ImageBin = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/image/bin.png"))
            };

            Button ButtonDelete = new Button
            {
                Width = 100,
                Height = 100,
                Content = ImageBin,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            ButtonDelete.Click += (s, e) =>
            {
                DeleteNote(s, e);
            };

            Grid.SetColumn(taskButton, 0);
            Grid.SetColumn(ButtonDelete, 1);
            taskGrid.Children.Add(taskButton);
            taskGrid.Children.Add(ButtonDelete);


            CheckBox completeTaskCheckBox = new CheckBox
            {
                Style = (Style)FindResource("CheckBoxTask")
            };

            completeTaskCheckBox.Checked += (s, e) =>
            {
                taskGrid.Opacity = 0.5;  
            };

            completeTaskCheckBox.Unchecked += (s, e) =>
            {
                taskGrid.Opacity = 1.0;  
            };

            taskGrid.Children.Add(completeTaskCheckBox);

            DatePicker dueDatePicker = new DatePicker
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5),
                SelectedDate = date  
            };

            dueDatePicker.SelectedDateChanged += (s, e) =>
            {
                if (dueDatePicker.SelectedDate.HasValue)
                {
                    DateTime selectedDate = dueDatePicker.SelectedDate.Value;
                    // Логика сохранения выбранной даты
                    Console.WriteLine($"Дата выполнения задачи: {selectedDate.ToShortDateString()}");
                }
            };

            taskPanel.Children.Add(dueDatePicker);

            TasksPanel.Children.Add(taskGrid);
        }


        private void OpenTaskForEditing(string title, string taskText, int idTask, DateTime? dueDate)
        {
            TasksPanel.Children.Clear();

            TextBox editTitle = new TextBox
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 30,
                Margin = new Thickness(10)
            };

            TextBox editBody = new TextBox
            {
                Text = taskText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 18,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };

            DatePicker editDueDatePicker = new DatePicker
            {
                SelectedDate = dueDate,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            CheckBox pinbox = new CheckBox
            {
                Content = "ЗАКРЕПИТЬ",
                Margin = new Thickness(10)
            };


            pinbox.Checked += (s, e) =>
            {
                CreatePinNote(title, taskText, idTask, true);
            };

            pinbox.Unchecked += (s, e) =>
            {
                UnFixed(s,e);
            };


            Button saveButton = new Button
            {
                Content = "СОХРАНИТЬ",
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100
            };

            saveButton.Click += (s, e) =>
            {
                SaveEditedTask(idTask, editTitle.Text, editBody.Text, editDueDatePicker.SelectedDate, (bool)pinbox.IsChecked);
                ReturnToTasksPanel();
            };

            StackPanel editPanel = new StackPanel();
            editPanel.Children.Add(editTitle);
            editPanel.Children.Add(editBody);
            editPanel.Children.Add(editDueDatePicker);
            editPanel.Children.Add(pinbox);
            editPanel.Children.Add(saveButton);

            TasksPanel.Children.Add(editPanel);
        }





        private void SaveEditedTask(int idTask, string newTitle, string newBody, DateTime? dueDate, bool pin)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE notes SET title = @newTitle, note_text = @newBody, date = @dueDate,pin = @pin  WHERE id_note = @idTask";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@newTitle", newTitle);
                    cmd.Parameters.AddWithValue("@newBody", newBody);
                    cmd.Parameters.AddWithValue("@idTask", idTask);
                    cmd.Parameters.AddWithValue("@pin", pin);


                    if (dueDate.HasValue)
                        cmd.Parameters.AddWithValue("@dueDate", dueDate.Value);
                    else
                        cmd.Parameters.AddWithValue("@dueDate", DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        private void ReturnToTasksPanel()
        {
            var lines = System.IO.File.ReadAllLines("session.txt");
            string userId = lines[1];
            TasksPanel.Children.Clear();
            TaskLoad(userId);
        }





        private void CreateNote(string title, string noteText, int idNote)
        {

            TextBlock noteTitle = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 30
            };


            TextBlock noteBody = new TextBlock
            {
                Text = noteText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 18
            };


            StackPanel notePanel = new StackPanel();
            notePanel.Children.Add(noteTitle);
            notePanel.Children.Add(noteBody);


            Grid noteGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
            noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); 


            Button noteButton = new Button
            {
                Content = notePanel,  
                Style = (Style)FindResource("Note"),
                Tag = idNote
            };


            noteButton.Click += (s, e) =>
            {
                OpenNoteForEditing(title, noteText, idNote);
            };


            Grid.SetColumn(noteButton, 0);
            noteGrid.Children.Add(noteButton);


            Image ImageBin = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/image/bin.png"))
            };

            Button ButtonDelete = new Button
            {
                Width = 100,
                Height = 100,
                Content = ImageBin,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };


            ButtonDelete.Click += (s, e) =>
            {
                DeleteNote(s,e);  
            };

            Grid.SetColumn(ButtonDelete, 1);
            noteGrid.Children.Add(ButtonDelete);


            NotesPanel.Children.Add(noteGrid);
        }


        private void OpenNoteForEditing(string title, string noteText, int idNote)
        {

            NotesPanel.Children.Clear();

            TextBox editTitle = new TextBox
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 30,
                Margin = new Thickness(10)
            };

            TextBox editBody = new TextBox
            {
                Text = noteText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 18,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };

            Button saveButton = new Button
            {
                Content = "СОХРАНИТЬ",
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100
            };

            saveButton.Click += (s, e) =>
            {
                SaveEditedNote(idNote, editTitle.Text, editBody.Text);
                ReturnToNotesPanel();
            };

            StackPanel editPanel = new StackPanel();
            editPanel.Children.Add(editTitle);
            editPanel.Children.Add(editBody);
            editPanel.Children.Add(saveButton);

            NotesPanel.Children.Add(editPanel);
        }

        private void SaveEditedNote(int idNote, string newTitle, string newBody)
        {

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE notes SET title = @newTitle, note_text = @newBody WHERE id_note = @idNote";

                using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@newTitle", newTitle);
                    cmd.Parameters.AddWithValue("@newBody", newBody);
                    cmd.Parameters.AddWithValue("@idNote", idNote);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ReturnToNotesPanel()
        {
            var lines = System.IO.File.ReadAllLines("session.txt");
            string userId = lines[1];
            NotesPanel.Children.Clear();
            NotesLoad(userId);
        }

        private void PinnedNotesLoad(string userId)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string queryid = "SELECT title, note_text, id_note, task FROM notes WHERE user_id = @user_id AND pin = true";

                using (NpgsqlCommand cmd = new NpgsqlCommand(queryid, connection))
                {
                    cmd.Parameters.AddWithValue("user_id", int.Parse(userId));

                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string title = reader.GetString(0);
                                string noteText = reader.GetString(1);
                                int idNote = reader.GetInt32(2);
                                bool task = reader.GetBoolean(3);
                                
                                CreatePinNote(title, noteText, idNote, task);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при извлечении закрепленных заметок: {ex.Message}");
                    }
                }

            }
        }


        private void UserDataLoad()
        {
            var lines = System.IO.File.ReadAllLines("session.txt");
            string sessionToken = lines[0]; // Первая строка — sessionToken
            string userId = lines[1];
            UserIdTextBlock.Text = userId;
            PinnedNotesLoad(userId);
            UpdateColor();

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string queryid = "SELECT first_name, Bar FROM users WHERE user_id = @user_id";

                using (NpgsqlCommand cmdid = new NpgsqlCommand(queryid, connection))
                {
                    cmdid.Parameters.AddWithValue("@user_id", Convert.ToInt32(userId));

                    using (NpgsqlDataReader reader = cmdid.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader["first_name"].ToString();
                            UserNameTextBlock.Text = name;


                            int barValue = Convert.ToInt32(reader["Bar"]);
                            progressBarMain.Value = barValue;
                            UpdateColor();
                        }
                        else
                        {
                            UserNameTextBlock.Text = "Пользователь не найден";
                        }
                    }
                }

            }
            NotesLoad(userId);
            TaskLoad(userId);
        }


        private void NotesLoad(string userId)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT title, note_text, id_note FROM notes WHERE user_id = @user_id AND task = false";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("user_id", int.Parse(userId));

                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string title = reader.GetString(0);
                                string noteText = reader.GetString(1);
                                int idNote = reader.GetInt32(2);

                                CreateNote(title, noteText, idNote);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при извлечении заметок: {ex.Message}");
                    }
                }
            }
        }

        private void TaskLoad(string userId)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT title, note_text, id_note, date FROM notes WHERE user_id = @user_id AND task = true";
                using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("user_id", int.Parse(userId));

                    try
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string title = reader.GetString(0);
                                string noteText = reader.GetString(1);
                                int idNote = reader.GetInt32(2);

                                // Проверка, есть ли дата (4-е поле)
                                if (!reader.IsDBNull(3))
                                {
                                    DateTime date = reader.GetDateTime(3);
                                    CreateTask(title, noteText, idNote, date); // Если дата есть
                                }
                                else
                                {
                                    CreateTask(title, noteText, idNote); // Если даты нет
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при извлечении заметок: {ex.Message}");
                    }
                }
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

            if (System.IO.File.Exists("session.txt"))
            {
                System.IO.File.Delete("session.txt");
            }


            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            Window.GetWindow(this)?.Close();
        }


    }
}
