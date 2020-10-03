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

using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Geared;

using System.Linq;

using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

using System.IO;

namespace TStat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public SeriesCollection SeriesCollection = new SeriesCollection
        //{
        //    new LineSeries
        //    {
        //        Values = new ChartValues<double> { 3, 5, 7, 4 }
        //    },
        //    new ColumnSeries
        //    {
        //        Values = new ChartValues<decimal> { 5, 6, 2, 7 }
        //    }
        //};

        SeriesCollection sc = new SeriesCollection();

        void DrawData(string filter, Dictionary<string, int[]> data)
        {
            axisX.LabelFormatter = value => /*new System.DateTime((long)(value * TimeSpan.FromDays(1).Ticks)).ToString("t")*/ myBigData.GetDateFromIndex(value).ToString("d");

            series.DisableAnimations = true;
            sc.Clear();

            foreach (var item in data)
                if (filter == null || item.Key.Contains(filter))
            {
                LineSeries lineSeries = new LineSeries();
                lineSeries.Name = myBigData.OnlyLetters(item.Key, '_');
                lineSeries.Title = lineSeries.Name;
                //lineSeries.Values = new ChartValues<int>(item.Value);

                var cv = item.Value.AsGearedValues();
                lineSeries.Values = cv;
                    //lineSeries.Values = lineSeries.Values.AsGearedValues().WithQuality(Quality.High);

                lineSeries.PointGeometry = null;
                sc.Add(lineSeries);
            }
        }

        public Func<double, string> Formatter { get; set; }

        public void UpdateNameById()
        {
            string myName = myBigData.FindMyName();
            idLabel.Content = config.MyId;
            nameText.Text = myName;
        }

        public void UpdateNameClick(object sender, EventArgs e)
        {
            string myName = myBigData.FindMyId(nameText.Text);
            idLabel.Content = config.MyId;
            nameText.Text = myName;
        }

        public void RunClick(object sender, EventArgs e)
        {
            //switch (e.Key)
            {
                //     case Key.F1:
                int d;
                if (!int.TryParse(days.Text, out d))
                { d = 1; days.Text = "1"; }


                StatMode statMode;
                OnlyMy onlyMy;

                switch (mode.Text)
                {
                    case "разделять всё":
                        statMode = StatMode.SplitAll; break;
                    case "не разделять по диалогам":
                        statMode = StatMode.SumPeople; break;
                    case "не разделять по словам":
                        statMode = StatMode.SumWords; break;
                    default:
                        MessageBox.Show($"Опция \"{mode.Text}\" не распознана", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }

                switch (my.Text)
                {
                    case "все сообщения":
                        onlyMy = OnlyMy.Any; break;
                    case "мои сообщения":
                        onlyMy = OnlyMy.My; break;
                    case "чужие сообщения":
                        onlyMy = OnlyMy.NotMy; break;
                    case "все (раздельно)":
                        onlyMy = OnlyMy.AnySplit; break;
                    default:
                        MessageBox.Show($"Опция \"{mode.Text}\" не распознана", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }


                DrawData(null, myBigData.RunWordsCounter(dialog.Text, 1000, onlyMy, word.Text, statMode, startDate.DisplayDate, endDate.DisplayDate, d));



          //          break;

                    //case Key.F1:
                    //    DrawData(null, myBigData.RunMetric(filter.Text, myBigData.AllLength));
                    //    break;
                    //case Key.F2:
                    //    DrawData(null, myBigData.RunMetric(filter.Text, myBigData.ToMeLength));
                    //    break;
                    //case Key.F3:
                    //    DrawData(null, myBigData.RunMetric(filter.Text, myBigData.ToMeStarts));
                    //    break;
                    //case Key.F4:
                    //    DrawData(null, myBigData.RunMetric(filter.Text, myBigData.StartsDiff));
                    //    break;
                    //case Key.F5:
                    //    DrawData(null, myBigData.RunMetric(filter.Text, myBigData.ToMeStartsPercentSmart));
                    //    break;
                    //case Key.F12:
                    //    DrawData(filter.Text, myBigData.RunWordsCounter("") );
                    //    break;
            }

        }

        MyBigData myBigData;
        Config config;

        public void LoadConfig()
        {
            try
            {
                using (StreamReader sr = new StreamReader("config.json", Encoding.UTF8))
                {
                    string text = sr.ReadToEnd();
                    config = System.Text.Json.JsonSerializer.Deserialize<Config>(text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть файл конфигурации. Конфигурация создана с нуля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                config = new Config();
                SaveConfig();
            }
        }

        public void SaveConfig()
        {
            using (StreamWriter sw = new StreamWriter("config.json", false, Encoding.UTF8))
            {
                string text = System.Text.Json.JsonSerializer.Serialize<Config>(config);
                sw.Write(text);
                //config = System.Text.Json.JsonSerializer.Deserialize<Config>(text);
            }

        }

        public MainWindow()
        {
            InitializeComponent();

            LoadConfig();

            //IronPython.

            //ScriptEngine engine = Python.CreateEngine();
            //engine.Execute("print('hello, world')");

            myBigData = new MyBigData(config);

            endDate.DisplayDate = DateTime.Now;
            startDate.DisplayDate = DateTime.Now.Subtract(new TimeSpan(365 * 4, 0, 0, 0, 0));
            endDate.SelectedDate = endDate.DisplayDate;
            startDate.SelectedDate = startDate.DisplayDate;

            if (config.MyId == 0)
            {
                MessageBox.Show("id не найден. Установите ваш id!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else UpdateNameById();

            //var words = myBigData.RunWordsCounter("Эве");

            //myBigData

            this.WindowState = System.Windows.WindowState.Maximized;


            // series.ChartLegend = new DefaultLegend();
            //series.LoadLegend();
            //series.ShowLegend(new LiveCharts.Dtos.CorePoint(10, 10));


            //axisY.DataContext = this;
            //sc.se

            series.Series = sc;

            Closing += (s, e) => { if (config.configChanged) SaveConfig(); };
        }
    }
}
