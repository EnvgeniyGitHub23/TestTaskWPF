using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;


namespace TestTaskWPF {
    /// <summary>
    /// Логика для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private static readonly string URL_DEVICES = "https://2392bb8b-2589-4515-a05d-bff3882c6c75.mock.pstmn.io/devices";
        private static readonly HttpClient client = new HttpClient();

        // Мапа со ссылками на информацию об устройствах
        Dictionary<string, string> linksMap = new Dictionary<string, string>();

        // Язык по умолчанию
        String currLang = "ru-RU";

        public MainWindow() {
            InitializeComponent();
            ChangeLanguage(currLang); // Устанавка начального языка
            Loaded += MainWindow_Loaded; // запрос данных при запуске программы
        }

        // получение списка устройств
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            string dataJson = await LoadDataAsync(URL_DEVICES);
            if (dataJson == null) {
                return;
            }
            DevicesParse(dataJson); // парсинг устройств в выпадающий список


            if (comboBox.Items.Count == 0) {
                string errorMessage = GetLocalizedString("ErrorGetUrl");
                throw new InvalidOperationException(errorMessage);
            }
            comboBox.SelectedIndex = 0;

            // заполняем мапу с адресами
            linksMap.Add("pumps", "https://2392bb8b-2589-4515-a05d-bff3882c6c75.mock.pstmn.io/pumps");
            linksMap.Add("cylinders", "https://2392bb8b-2589-4515-a05d-bff3882c6c75.mock.pstmn.io/cylinders");
            linksMap.Add("valves", "https://2392bb8b-2589-4515-a05d-bff3882c6c75.mock.pstmn.io/valves");

            loadButton.IsEnabled = true; // после подгрузки разрещаем нажимать кнопку
        }

        // парсинг устройств в выпадающий список
        private void DevicesParse(string data) {
            var devices = JsonConvert.DeserializeObject<List<Device>>(data);
            comboBox.ItemsSource = devices;
            comboBox.DisplayMemberPath = "Description";  // для отображения
            comboBox.SelectedValuePath = "Name";         // для значения
        }

        /// <summary>
        /// Асинхронно загружает данные с указанного URL
        /// </summary>
        /// <param name="url">URL для загрузки данных</param>
        /// <returns>string (JSON)</returns>
        /// <exception cref="HttpRequestException">Выбрасывается, если возникает ошибка при выполнении запроса</exception>
        private static async Task<string> LoadDataAsync(string url) {
            try {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) {
                    string errorMessage = GetLocalizedString("ErrorGetUrl");
                    MessageBox.Show(errorMessage + ": " + url + ", Status Code: " + response.StatusCode);
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync();
                if (content == null || content.Contains("error")) {
                    string errorMessage = GetLocalizedString("ErrorContent");
                    MessageBox.Show(errorMessage);
                    return null;
                }

                return content;
            } catch (HttpRequestException ex) {
                string errorMessage = GetLocalizedString("ErrorGetUrl");
                MessageBox.Show(errorMessage + ": " + url);
                throw;
            }
        }
        private static JArray jsonArray = [];


        // клик по кнопке Загрузить
        private async void Button_Load_Click(object sender, RoutedEventArgs e) {
            string selectedValue = (string)comboBox.SelectedValue;

            if (linksMap.ContainsKey(selectedValue)) { // если есть URL для выбранного типа устройств
                string response = await LoadDataAsync(linksMap[selectedValue]);

                jsonArray = JArray.Parse(response);
                List<DeviceItem> itemsList = new List<DeviceItem>();
                foreach (var item in jsonArray) {
                    itemsList.Add(new DeviceItem {
                        Id = item["Id"].ToString(),
                        Name = item["Name"].ToString(),
                        Code = item["Code"].ToString()
                    });
                }

                DataContext = itemsList;                

                // привязываем к dataGrid
                var data = new List<dynamic>();
                foreach (var item in jsonArray) {
                    data.Add(item);
                }

                dataGrid.ItemsSource = data;

            } else {
                string errorMessage = GetLocalizedString("ErrorNoUrl");
                MessageBox.Show(errorMessage);
            }
        }


        /// <summary>
        /// Выводит информацию по клику мыши по таблице
        /// </summary>
        private void DataGridMain_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedItem = dataGrid.SelectedItem;
            if (selectedItem != null) {
                JObject selectedObject = JObject.FromObject(selectedItem);
                detailsTextBox.Clear();
                foreach (var property in selectedObject.Properties()) {
                    if (property.Name != "Id" && property.Name != "Name" && property.Name != "Code") {
                        detailsTextBox.AppendText($"{property.Name}: {property.Value}\n");
                    }
                }
            } else {
                detailsTextBox.Clear();
            }
        }


        // устройства в выпадающем списке
        public class Device {
            public required string Name { get; set; }
            public required string Description { get; set; }
        }

        // основная информация об устройствах в таблице
        public class DeviceItem {
            public required string Id { get; set; }
            public required string Code { get; set; }
            public required string Name { get; set; }
        }


        // циклическая смена языка интерфейса по кнопке
        private void OnChangeLanguageClick(object sender, RoutedEventArgs e) {
            
            if (currLang == "ru-RU") {
                ChangeLanguage("en-US");
            } else {
                ChangeLanguage("ru-RU");
            }
        }

        // изменение языка интерфейса
        private void ChangeLanguage(string culture) {
            var dict = new ResourceDictionary();
            switch (culture) {
                case "ru-RU":
                    dict.Source = new Uri("Resources/Strings.ru.xaml", UriKind.Relative);
                    currLang = "ru-RU";
                    break;
                default:
                    dict.Source = new Uri("Resources/Strings.en.xaml", UriKind.Relative);
                    currLang = "en-US";
                    break;
            }

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);

            UpdateBindings();
        }

        // обновление языка элементов интерфейса
        private void UpdateBindings() {
            // кнопки
            langButton.Content = Application.Current.FindResource("Lang") as string;
            loadButton.Content = Application.Current.FindResource("Load") as string;

            // название окна
            this.Title = Application.Current.FindResource("ProgramName") as string;
            
            // столбцы
            dataGrid.Columns[0].Header = Application.Current.FindResource("DataGridColumnId") as string;
            dataGrid.Columns[1].Header = Application.Current.FindResource("DataGridColumnName") as string;
            dataGrid.Columns[2].Header = Application.Current.FindResource("DataGridColumnCode") as string;
        }

        // изменение языка сообщений
        private static string? GetLocalizedString(string key) {
            return Application.Current.TryFindResource(key) as string;
        }
    }
}

