namespace DigitRecognitionLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;

    public delegate void OutputHandler(object sender, Prediction pr);

    public struct Prediction
    {
        public Prediction(string p, int l, float c)
        {
            this.Path = p;
            this.Label = l;
            this.Confidence = c;
        }

        public string Path { get; }

        public int Label { get; }

        public float Confidence { get; }
    }

    public class Recognition
    {
        private static InferenceSession session;

        private LibraryContext LibContext;

        private object lockObj;

        private static CancellationTokenSource cts = new CancellationTokenSource();

        public Recognition()
        {
            session = new InferenceSession(@"..\DigitRecognitionLibrary\mnist-8.onnx");
            LibContext = new LibraryContext();
            lockObj = new object();
        }

        public event OutputHandler OutputEvent;

        public void StopRecognition()
        {
            cts.Cancel();
        }

        public void Run(string dir)
        {
            cts = new CancellationTokenSource();

            if (!Directory.Exists(dir))
            {
                dir = @"..\DigitRecognitionLibrary\DefaultImages";
                Trace.WriteLine("Using library with default images...");
            }

            string[] imagePaths = Directory.GetFiles(dir).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".gif")).ToArray();

            int count = imagePaths.Count();
            if (count == 0)
            {
                Trace.WriteLine("Your directory is empty.");
                return;
            }

            var events = new AutoResetEvent[count];

            for (int i = 0; i < count; i++)
            {
                events[i] = new AutoResetEvent(false);

                ThreadPool.QueueUserWorkItem(
                pi =>
                {
                    int idx = Convert.ToInt32(pi);

                    Tuple<int, float> res;
                    lock (lockObj)
                    {
                        res = LibContext.FindResults(imagePaths[idx]);
                    }

                    if (res != null)
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            Prediction output = new Prediction(imagePaths[idx], res.Item1, res.Item2);
                            OutputEvent?.Invoke(this, output);
                        }

                        events[idx].Set();
                    }
                    else
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            Prediction output = OneImgRecognition(imagePaths[idx]);
                            OutputEvent?.Invoke(this, output);
                            lock (lockObj)
                            {
                                LibContext.AddResults(output.Path, output.Label, output.Confidence);
                            }
                        }

                        events[idx].Set();
                    }
                }, i);
            }

            for (int i = 0; i < count; i++)
            {
                events[i].WaitOne();
            }

            return;
        }

        private static Prediction OneImgRecognition(string path)
        {
            using var image = Image.Load<Rgb24>(path);
            const int TargetWidth = 28;
            const int TargetHeight = 28;

            image.Mutate(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new Size(TargetWidth, TargetHeight),
                    Mode = ResizeMode.Crop,
                });
            });

            var input = new DenseTensor<float>(new[] { 1, 1, TargetHeight, TargetWidth });
            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var stddev = new[] { 0.229f, 0.224f, 0.225f };
            for (int y = 0; y < TargetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < TargetWidth; x++)
                {
                    input[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("Input3", input),
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            float confidence = softmax.Max();
            int label = softmax.ToList().IndexOf(confidence);

            return new Prediction(path, label, confidence);
        }
    }
}
