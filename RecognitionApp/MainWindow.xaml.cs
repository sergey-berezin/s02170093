namespace RecognitionApp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
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
        public static RoutedCommand ClearDb = new RoutedCommand("ClearDb", typeof(MainWindow));

        private bool isDirPathChosen = false;
        private bool isProcessing = false;
        private bool isClearingDb = false;

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
                LabelsCollection.Add(new LabelInf() { Label = i, Count = 0, CountInDb = 0 });
            }

            OneLabelCollection = new ObservableCollection<OneLabel>();
            Binding BndOneLabelColl = new Binding();
            BndOneLabelColl.Source = OneLabelCollection;
            LbOneLabel.SetBinding(ItemsControl.ItemsSourceProperty, BndOneLabelColl);

            LibContext = new LibraryContext();
        }

        private Recognition R = new Recognition();
        private ObservableCollection<ImgInf> ImgCollection;
        private ObservableCollection<LabelInf> LabelsCollection;
        private ObservableCollection<OneLabel> OneLabelCollection;
        private LibraryContext LibContext;

        private string DirPath { get; set; }

        private void OutputHandler(object sender, Prediction pr)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ImgInf img = ImgCollection.First(i => i.Path == pr.Path);
                img.Label = pr.Label;
                img.Confidence = pr.Confidence;

                LabelInf lbl = LabelsCollection.First(i => i.Label == img.Label);
                lbl.Count++;
                lbl.CountInDb++;

                LibContext.AddResults(img.Path, img.Label, img.Confidence);
            }));
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
            e.CanExecute = isDirPathChosen && !isProcessing && !isClearingDb;
        }

        private void StartCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            isProcessing = true;
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

                    Tuple<int, float, int> res = LibContext.FindResults(path);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (res != null)
                        {
                            ImgInf img = ImgCollection.First(i => i.Path == path);
                            img.Label = res.Item1;
                            img.Confidence = res.Item2;

                            LabelInf lbl = LabelsCollection.First(i => i.Label == img.Label);
                            lbl.Count++;
                            lbl.CountInDb = res.Item3;

                            Trace.WriteLine("Reading results from database...");
                        }
                        else
                        {
                            Recognize(path);
                            Trace.WriteLine("Recognizing results...");
                        }
                    }));
                }

                isProcessing = false;
            }));
        }

        private void Recognize(string path)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                R = new Recognition();
                R.OutputEvent += OutputHandler;
                R.Run(path);
            }));
        }

        private void CanStopCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isProcessing;
        }

        private void StopCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            R.StopRecognition();
        }

        private void CanClearDbCommandHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !isProcessing;
        }

        private void ClearDbCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
            {
                isClearingDb = true;
                LibContext.ClearDb();
                LibContext = new LibraryContext();
                foreach (LabelInf i in LabelsCollection)
                {
                    i.CountInDb = 0;
                }
                isClearingDb = false;
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

    public class LabelInf : INotifyPropertyChanged
    {
        private int c;
        private int cdb;

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

        public int CountInDb
        {
            get
            {
                return cdb;
            }

            set
            {
                cdb = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CountInDb"));
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
