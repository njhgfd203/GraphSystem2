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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Data;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
//using GraphSistem2.Resources;
//using GraphSistem2.Resources;
namespace GraphSistem2
{
    public partial class MainWindow : Window
    {

        private MySqlConnection connection;
        private MySqlDataAdapter adapter;
        private DataTable dataTable;

        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Days { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            nameMachineCb.Visibility = Visibility.Visible;
            nameMachineCb.IsEnabled = true;
            nameMachineCb.Background = Brushes.White;
            typeMachineCb.SelectionChanged += typeMachineCb_SelectionChanged;
            nameMachineCb.SelectionChanged += nameMachineCb_SelectionChanged;
            SeriesCollection = new SeriesCollection();
            Days = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            YFormatter = value => value.ToString("N1") + " ч";
            InitializeDatabase();
            InitializeChart();
            DataContext = this;
            UpdateMachineTitle();
        }

        public class Message
        {
            public string Time { get; set; }
            public string Type { get; set; }
            public string Text { get; set; }
            public Brush TypeColor
            {
                get
                {
                    switch (Type)
                    {
                        case "Error": return Brushes.Red;
                        case "Warning": return Brushes.Orange;
                        default: return Brushes.Black;
                    }
                }
            }
        }
        public class ChartData
        {
            public ChartValues<double> TurningValues { get; set; } = new ChartValues<double>();
            public ChartValues<double> MillingValues { get; set; } = new ChartValues<double>();
            public List<string> Days { get; set; } = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
        }
        public Func<double, string> YFormatter { get; set; }
        private void InitializeChart()
        {
            // Инициализация данных графика
            SeriesCollection = new SeriesCollection();
            Days = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
        }

        private void InitializeDatabase()
        {
            string connectionString = "Server=localhost; Database=graph_system_interface; User ID=root; Password=Toyotaipsum1996!";
            connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();
                LoadMachineTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void LoadMachineTypes()
        {
            try
            {
                string query = "SELECT DISTINCT type FROM machine_tool_type ORDER BY type";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        typeMachineCb.Items.Clear();
                        while (reader.Read())
                        {
                            typeMachineCb.Items.Add(reader["type"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов станков: {ex.Message}");
            }
        }

        private void typeMachineCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (typeMachineCb.SelectedItem == null) return;

            string selectedType = typeMachineCb.SelectedItem.ToString();
            LoadMachineNames(selectedType);
            UpdateMachineTitle();
        }
        private void name_mt_DropDownClosed(object sender, EventArgs e)
        {
            // Ваш код обработки события
           
        }

        private void DebugLoadMachineNames(string machineType)
        {
            StringBuilder debugInfo = new StringBuilder();
            debugInfo.AppendLine($"Загружаем станки для типа: '{machineType}'");

            try
            {
                string query = @"SELECT 
                        t.id_mt as type_id,
                        t.type as type_name,
                        n.id_mtn as machine_id,
                        n.machine_tool_name as machine_name
                        FROM machine_tool_type t
                        LEFT JOIN machine_tool_name n ON t.id_mt = n.id_mt
                        WHERE t.type = @machineType";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@machineType", machineType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        bool hasAnyData = false;
                        while (reader.Read())
                        {
                            hasAnyData = true;
                            debugInfo.AppendLine($"Найдено: ТипID={reader["type_id"]}, " +
                                               $"Тип='{reader["type_name"]}', " +
                                               $"СтанокID={reader["machine_id"]}, " +
                                               $"Станок='{reader["machine_name"]}'");
                        }

                        if (!hasAnyData)
                        {
                            debugInfo.AppendLine("Нет данных в результате запроса!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Ошибка: {ex.Message}");
            }

            // Выводим всю информацию в MessageBox и консоль отладки
            MessageBox.Show(debugInfo.ToString(), "Отладка загрузки станков");
            Debug.WriteLine(debugInfo.ToString());
        }
        private void LoadMachineNames(string machineType)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                nameMachineCb.Items.Clear(); // Очищаем комбобокс

                string query = @"SELECT n.machine_tool_name 
                        FROM machine_tool_name n
                        INNER JOIN machine_tool_type t ON n.id_mt = t.id_mt
                        WHERE t.type = @machineType
                        ORDER BY n.machine_tool_name";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@machineType", machineType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            nameMachineCb.Items.Add(reader["machine_tool_name"].ToString());
                        }
                    }
                }

                // Если есть элементы, выберите первый
                if (nameMachineCb.Items.Count > 0)
                {
                    nameMachineCb.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                Debug.WriteLine(ex.ToString());
            }
        }
        /*private void nameMachineCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (nameMachineCb.SelectedItem == null) return;

            dynamic selectedMachine = nameMachineCb.SelectedItem;
            //machineTitle.Content = $"{typeMachineCb.SelectedItem} {selectedMachine.Name}";

            // Загрузка изображения
            try
            {
                string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                       "Resources",
                                                       selectedMachine.ImagePath);
                machineImage.Source = new BitmapImage(new Uri(imagePath));
            }
            catch
            {
                machineImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/{imageName}"));
            }

            LoadMachineData(selectedMachine.Name);
            UpdateMachineTitle();
        }*/

        private void nameMachineCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (nameMachineCb.SelectedItem == null) return;

            string selectedMachineName = nameMachineCb.SelectedItem.ToString();

           
            string imageName = $"{selectedMachineName.Replace(" ", "_").ToLower()}.png"; // Формируем имя файла
            LoadMessages(selectedMachineName);
            UpdateMachineTitle();
            // Пытаемся загрузить изображение из ресурсов
            try
            {
                var uri = new Uri($"pack://application:,,,/GraphSistem2;component/Resources/{imageName}", UriKind.Absolute);
                machineImage.Source = new BitmapImage(uri);
            }
            catch
            {
                // Если основное изображение не найдено, пробуем загрузить дефолтное
                try
                {
                    string imagePath = $"Resources/{imageName}";
                    machineImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                }
                catch
                {
                    // Если и дефолтное не загружается, очищаем изображение
                    machineImage.Source = null;
                }
            }

            LoadMachineData(selectedMachineName);
            UpdateMachineTitle();
        }
        private void LoadMachineData(string machineName)
        {
            try
            {
                string query = $@"
            SELECT
                CASE
                    WHEN id_operation = 1 THEN 'Токарная'
                    WHEN id_operation = 2 THEN 'Фрезерная'
                    ELSE 'Неизвестно'
                END AS operation_type,
                day,
                SUM(time_mtl) AS total_minutes
            FROM
                machine_tool_load
            WHERE
                id_mtn = (SELECT id_mtn FROM machine_tool_name WHERE machine_tool_name = @machineName)
                AND day IN ('пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'вс')
            GROUP BY
                operation_type,
                day
            ORDER BY
                CASE
                    WHEN day = 'пн' THEN 1
                    WHEN day = 'вт' THEN 2
                    WHEN day = 'ср' THEN 3
                    WHEN day = 'чт' THEN 4
                    WHEN day = 'пт' THEN 5
                    WHEN day = 'сб' THEN 6
                    WHEN day = 'вс' THEN 7
                    ELSE 8
                END;";
                adapter = new MySqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@machineName", machineName);

                dataTable = new DataTable();
                adapter.Fill(dataTable);
                LoadWeeklyChartData(machineName);
                //componentsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }
        private void LoadWeeklyChartData(string machineName)
        {
            if (SeriesCollection == null)
            {
                SeriesCollection = new SeriesCollection();
            }

            try
            {
                // Очищаем только если коллекция существует
                SeriesCollection.Clear();

                // Получаем ID станка
                int machineId = GetMachineIdByName(machineName);

                if (machineId == -1) return;

                // Создаем временные коллекции для данных
                var turningValues = new ChartValues<double>();
                var millingValues = new ChartValues<double>();

                // Заполняем данные для каждого дня недели
                foreach (var day in new[] { "пн", "вт", "ср", "чт", "пт", "сб", "вс" })
                {
                    turningValues.Add(GetOperationHours(machineId, 1, day) / 60.0);
                    millingValues.Add(GetOperationHours(machineId, 2, day) / 60.0);
                }

                // Добавляем серии
                SeriesCollection.Add(new ColumnSeries
                {
                    Title = "Токарные",
                    Values = turningValues,
                    Fill = Brushes.Green
                });

                SeriesCollection.Add(new ColumnSeries
                {
                    Title = "Фрезерные",
                    Values = millingValues,
                    Fill = Brushes.Blue
                });

                // Обновляем подписи дней
                Days = new List<string> { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private int GetMachineIdByName(string machineName)
        {
            string query = "SELECT id_mtn FROM machine_tool_name WHERE machine_tool_name = @machineName";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@machineName", machineName);
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
        }

        private double GetOperationHours(int machineId, int operationId, string day)
        {
            string query = @"SELECT COALESCE(SUM(time_mtl), 0) 
                   FROM machine_tool_load 
                   WHERE id_mtn = @machineId 
                   AND id_operation = @operationId 
                   AND day = @day";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@machineId", machineId);
                cmd.Parameters.AddWithValue("@operationId", operationId);
                cmd.Parameters.AddWithValue("@day", day);

                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToDouble(result) : 0;
            }
        }
        private void UpdateMachineTitle()
        {
            string type = typeMachineCb.SelectedItem?.ToString() ?? "Тип не выбран";
            string name = nameMachineCb.SelectedItem?.ToString() ?? "Название не выбрано";

            baseLabel.Content = $"{type} - {name}";
        }
        private void LoadMachineState(string machineName)
        {
            try
            {
                int machineId = GetMachineIdByName(machineName);

                string query = @"SELECT status, time_mtl, name_channel, value, description 
                       FROM machine_tool_state 
                       WHERE id_mtn = @machineId
                       ORDER BY time_mtl DESC
                       LIMIT 10";

                adapter = new MySqlDataAdapter(query, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@machineId", machineId);

                dataTable = new DataTable();
                adapter.Fill(dataTable);

                // Здесь можно привязать dataTable к DataGrid или другому элементу
                // componentsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки состояния станка: {ex.Message}");
            }
        }

        private void LoadMessages(string machineName)
        {
            try
            {
                var messages = new List<Message>
        {
            new Message
            {
                Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                Text = "Ошибка: Превышение температуры",
                Type = "Error"
            },
            new Message
            {
                Time = DateTime.Now.AddMinutes(-1).ToString("dd.MM.yyyy HH:mm:ss"),
                Text = "Предупреждение: Вибрация выше нормы",
                Type = "Warning"
            },
            new Message
            {
                Time = DateTime.Now.AddMinutes(-2).ToString("dd.MM.yyyy HH:mm:ss"),
                Text = "Информация: Цикл обработки завершен",
                Type = "Info"
            }
        };

                messagesGrid.ItemsSource = messages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }
        private void LoadMessagesFromDatabase(string machineName)
        {
            try
            {
                int machineId = GetMachineIdByName(machineName);
                if (machineId == -1) return;

                string query = @"SELECT 
                DATE_FORMAT(time_mtl, '%d.%m.%Y %H:%i:%s') as Time, 
                status as Type,
                description as Text
                FROM machine_tool_state
                WHERE id_mtn = @machineId
                ORDER BY time_mtl DESC
                LIMIT 20";

                var messages = new List<Message>();

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@machineId", machineId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new Message
                            {
                                Time = reader["Time"].ToString(),
                                Type = reader["Type"].ToString(),
                                Text = reader["Text"].ToString()
                            });
                        }
                    }
                }

                messagesGrid.ItemsSource = messages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
            }
        }
    }
}
