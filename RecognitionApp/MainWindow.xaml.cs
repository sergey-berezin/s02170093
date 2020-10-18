namespace RecognitionApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Forms.VisualStyles;
    using System.Windows.Ink;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using DigitRecognitionLibrary;

    public partial class MainWindow : Window
    {
        public static RoutedCommand Start = new RoutedCommand("Start", typeof(MainWindow));
        public static RoutedCommand Stop = new RoutedCommand("Stop", typeof(MainWindow));

        private bool isDirPathChosen = false;
        private bool isRecognising = false;

        public MainWindow()
        {
            InitializeComponent();

            ImgCollection = FindResource("key_OIC") as ObsImgCollection;

            LabelsCollection = FindResource("key_OLC") as ObsLabelCollection;
            for (int i = 0; i < 10; i++)
            {
                LabelsCollection.Add(new LabelInf() { Label = i, Count = 0 });
            }

            OneLabelCollection = FindResource("key_OOLC") as ObsOneLabelCollection;
        }

        private Recognition R = new Recognition();
        private ObsImgCollection ImgCollection = new ObsImgCollection();
        private ObsLabelCollection LabelsCollection = new ObsLabelCollection();
        private ObsOneLabelCollection OneLabelCollection = new ObsOneLabelCollection();

        private string DirPath { get; set; }

        private void OutputHandler(object sender, Prediction pr)
        {
            ImgInf img = ImgCollection.First(i => i.Path == pr.Path);
            img.Label = pr.Label;
            img.Confidence = pr.Confidence;

            LabelInf lbl = LabelsCollection.First(i => i.Label == img.Label);
            lbl.Count++;
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
            e.CanExecute = isDirPathChosen && !isRecognising;
        }

        private void StartCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            ImgCollection.Clear();
            foreach (LabelInf i in LabelsCollection)
            {
                i.Count = 0;
            }

            OneLabelCollection.Clear();
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                foreach (string path in Directory.GetFiles(DirPath))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ImgCollection.Add(new ImgInf()
                        {
                            Image = new BitmapImage(new Uri(path)),
                            Path = path,
                            Label = -1,
                            Confidence = -1,
                        });
                    }));
                }
            }));
            Recognize();
        }

        private void Recognize()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                R = new Recognition();
                R.OutputEvent += OutputHandler;
                isRecognising = true;
                R.Run(DirPath);
                Dispatcher.BeginInvoke(new Action(() => isRecognising = false));
            }));
        }

        private void CanStopCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isRecognising;
        }

        private void StopCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            R.StopRecognition();
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

    public class ObsImgCollection: ObservableCollection<ImgInf> { }

    public class ImgInf: INotifyPropertyChanged
    {
        private int l;
        private float c;

        public event PropertyChangedEventHandler PropertyChanged;

        public BitmapImage Image { get; set; }

        public string Path { get; set; }

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

    public class ObsLabelCollection : ObservableCollection<LabelInf> { }

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

    public class ObsOneLabelCollection : ObservableCollection<OneLabel> { }

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
