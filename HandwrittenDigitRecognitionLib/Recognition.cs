using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HandwrittenDigitRecognitionLib
{
    public class Recognition
    {
        public void Run(string dir)
        {
            if (!Directory.Exists(dir))
            {
                // current directory is /bin/Debug/netcoreapp3.1 so we go 3 steps back
                dir = @"../../../";
            }
            string[] imagePaths = Directory.GetFiles(dir, "*.png");
            int count = imagePaths.Count();
            var events = new AutoResetEvent[count];

            CancellationTokenSource ctsForThreadPool = new CancellationTokenSource();
            CancellationTokenSource ctsForEscapeThread = new CancellationTokenSource();
            var t = new Thread(() =>
            {
                Console.WriteLine("Type ESCAPE to stop recognition.");  
                while (true)
                {
                    // 1st way to break is to press ESCAPE and stop recognition
                    var check = Console.ReadKey();
                    if (check.Key == ConsoleKey.Escape)
                    {
                        ctsForThreadPool.Cancel();
                        break;
                    }
                    // 2nd way to break is to wait until the last image goes to recognition
                    if (ctsForEscapeThread.Token.IsCancellationRequested)
                    {
                        ctsForThreadPool.Cancel();
                        break;
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
            });
            t.Start();

            for (int i = 0; i < count; i++)
            {
                events[i] = new AutoResetEvent(false);
                ThreadPool.QueueUserWorkItem(pi => {
                    int idx = Convert.ToInt32(pi);
                    //if (idx == 1) Thread.Sleep(2000); // this is how we can check if it really works parallel
                    if (!ctsForThreadPool.Token.IsCancellationRequested)
                    {
                        OneImgRecognition(imagePaths[idx], idx);
                    }
                    events[idx].Set();
                }, i);
                if (i == count - 1)
                {
                    ctsForEscapeThread.Cancel();
                }
            }

            for (int i = 0; i < count; i++)
            {
                events[i].WaitOne();
            }

            t.Join();
            return;
        }



        public static object lockObj = new object();
        private static void OneImgRecognition(string path, int idx)
        {
            using var image = Image.Load<Rgb24>(path);
            const int TargetWidth = 28;
            const int TargetHeight = 28;

            image.Mutate(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new Size(TargetWidth, TargetHeight),
                    Mode = ResizeMode.Crop
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
                        // we can see input name in the viewer for nn called Netron
                        NamedOnnxValue.CreateFromTensor("Input3", input)
                    };

            // current directory is /bin/Debug/netcoreapp3.1 so we go 3 steps back
            using var session = new InferenceSession(@"../../../mnist-8.onnx");
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);

            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            lock (lockObj)
            {
                Console.WriteLine(path);
                // output probabilities across the 3 of 10 classes
                foreach (var p in softmax
                    .Select((x, idx) => new { Label = classLabels[idx], Confidence = x })
                    .OrderByDescending(x => x.Confidence)
                    .Take(3))
                    Console.WriteLine($"{p.Label} with confidence {p.Confidence}");
            }
        }



        public static readonly string[] classLabels = new[]
        {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9"
        };
    }
}
