namespace RecognitionConsoleTest
{
    using System;
    using DigitRecognitionLibrary;
    class Test
    {
        public static void OutputHandler(object sender, Prediction pr)
        {
            Console.WriteLine($"{pr.Path}: {pr.Label} with confidence {pr.Confidence}");
        }

        static void Main(string[] args)
        {
            Console.WriteLine(@"Type a directory with png, jpg, bmp or gif images (for instance, C:\Users\andre\Desktop\DefaultImages) and press ENTER.");
            Console.WriteLine("If the given directory is incorrect, the app gonna use one with default images.");

            string dir = Console.ReadLine();
            Recognition R = new Recognition();
            R.OutputEvent += OutputHandler;
            R.Run(dir);

            Console.WriteLine("\nTesting has passed successfully.");
            return;
        }
    }
}
