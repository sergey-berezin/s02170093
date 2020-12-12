namespace RecognitionApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using Newtonsoft.Json;
    using Contracts;

    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string url = "https://localhost:44353/rec";
        private static CancellationTokenSource cts = new CancellationTokenSource();

        public static RoutedCommand Start = new RoutedCommand("Start", typeof(MainWindow));
        public static RoutedCommand Stop = new RoutedCommand("Stop", typeof(MainWindow));
        public static RoutedCommand ClearDb = new RoutedCommand("ClearDb", typeof(MainWindow));
        public static RoutedCommand GetStats = new RoutedCommand("GetStats", typeof(MainWindow));

        private bool isDirPathChosen = false;
        private bool isProcessing = false;
        private bool isClearingDb = false;
        private bool isGettingStats = false;

        private ObservableCollection<ImgInf> ImgCollection;
        private ObservableCollection<LabelInf> LabelsCollection;
        private ObservableCollection<OneLabel> OneLabelCollection;

        private string DirPath { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            ImgCollection = new ObservableCollection<ImgInf>();
            Binding BndImgColl = new Binding();
            BndImgColl.Source = ImgCollection;
            LbOIC.SetBinding(ItemsControl.ItemsSourceProperty, BndImgColl);

            LabelsCollection = new ObservableCollection<LabelInf>();
            Binding BndLabelsColl = new Binding();
            BndLabelsColl.Source = LabelsCollection;
            LbOLC.SetBinding(ItemsControl.ItemsSourceProperty, BndLabelsColl);
            for (int i = 0; i < 10; i++)
            {
                LabelsCollection.Add(new LabelInf() { Label = i, Count = 0 });
            }

            OneLabelCollection = new ObservableCollection<OneLabel>();
            Binding BndOneLabelColl = new Binding();
            BndOneLabelColl.Source = OneLabelCollection;
            LbOneLabel.SetBinding(ItemsControl.ItemsSourceProperty, BndOneLabelColl);

            TbStats.Text = "Statistics:";
        }

        private void OpenCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                DirPath = dialog.SelectedPath;
                isDirPathChosen = true;
            }
        }

        private void CanStartCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isDirPathChosen && !isProcessing && !isClearingDb && !isGettingStats;
        }

        private void StartCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            isProcessing = true;
            TbStats.Text = "Statistics:";
            ImgCollection.Clear();
            foreach (LabelInf i in LabelsCollection)
            {
                i.Count = 0;
            }

            OneLabelCollection.Clear();
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                PostFunc();
            }));
        }

        private async void PostFunc()
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(DirPath), Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = await client.PostAsync(url, content, cts.Token);
                }
                catch (HttpRequestException)
                {
                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION!", "Info");
                        StopCommandHandler(null, null);
                    }));
                    return;
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    var item = JsonConvert.DeserializeObject<List<TransferFile>>(httpResponse.Content.ReadAsStringAsync().Result);
                    foreach (var pr in item)
                    {
                        await Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ImgCollection.Add(new ImgInf()
                            {
                                Image = ToBitmapImage(Convert.FromBase64String(pr.Image)),
                                Path = pr.Path,
                                Label = pr.Label,
                                Confidence = pr.Confidence,
                            });

                            LabelInf lbl = LabelsCollection.First(i => i.Label == Convert.ToInt32(pr.Label));
                            lbl.Count++;
                        }));
                    }

                    isProcessing = false;
                }
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show("RECOGNITION WAS STOPPED!", "Info");
                }));
            }
        }

        private BitmapImage ToBitmapImage(byte[] array)
        {
            using var ms = new MemoryStream(array);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void CanStopCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isProcessing;
        }

        private void StopCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            cts.Cancel(false);
            cts.Dispose();
            cts = new CancellationTokenSource();
            isProcessing = false;
        }

        private void CanClearDbCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !isProcessing && !isGettingStats;
        }

        private void ClearDbCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            isClearingDb = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                try
                {
                    var httpResponse = client.DeleteAsync(url).Result;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TbStats.Text = "Statistics:";
                    }));
                }
                catch (AggregateException)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION!", "Info");
                    }));
                }

                isClearingDb = false;
            }));
        }

        private void CanGetStatsCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !isProcessing && !isClearingDb;
        }

        private void GetStatsCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            isGettingStats = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                try
                {
                    var httpResponse = client.GetAsync(url).Result;
                    var stats = JsonConvert.DeserializeObject<string[]>(httpResponse.Content.ReadAsStringAsync().Result);
                    if (stats.Length == 0)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TbStats.Text = "Statistics: database is empty.";
                        }));
                    }
                    else
                    {
                        string statsOutput = "Statistics:\n";
                        for (int i = 0; i < stats.Length; i++)
                        {
                            string[] st = stats[i].Split(' ');
                            statsOutput += "Label " + st[0] + " is " + st[1] + " time(s) in database.\n";
                        }

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TbStats.Text = statsOutput;
                        }));
                    }
                }
                catch (AggregateException)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("NO CONNECTION!", "Info");
                    }));
                }

                isGettingStats = false;
            }));
        }

        private void LbOLC_SelectionChanged(object sender, EventArgs e)
        {
            OneLabelCollection.Clear();
            LabelInf lbl = LbOLC.SelectedItem as LabelInf;
            if (lbl != null)
            {
                var q = from i in ImgCollection
                        where i.Label == lbl.Label
                        select i.Image;

                foreach (BitmapImage b in q)
                {
                    OneLabelCollection.Add(new OneLabel() { Image = b });
                }
            }
        }
    }

    public class ImgInf : INotifyPropertyChanged
    {
        private BitmapImage i;
        private string p;
        private int l;
        private float c;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapImage Image
        {
            get
            {
                return i;
            }

            set
            {
                i = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Image"));
            }
        }

        public string Path
        {
            get
            {
                return p;
            }

            set
            {
                p = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Path"));
            }
        }

        public int Label
        {
            get
            {
                return l;
            }

            set
            {
                l = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Label"));
            }
        }

        public float Confidence
        {
            get
            {
                return c;
            }

            set
            {
                c = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Confidence"));
            }
        }
    }

    public class LabelInf : INotifyPropertyChanged
    {
        private int c;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Label { get; set; }

        public int Count
        {
            get
            {
                return c;
            }

            set
            {
                c = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
            }
        }
    }

    public class OneLabel : INotifyPropertyChanged
    {
        private BitmapImage i;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapImage Image
        {
            get
            {
                return i;
            }

            set
            {
                i = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Image"));
            }
        }
    }
}
