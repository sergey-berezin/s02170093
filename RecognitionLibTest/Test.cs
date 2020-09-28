using System;
using HandwrittenDigitRecognitionLib;

namespace RecognitionLibTest
{
    class Test
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            Console.WriteLine("Type a directory with images (for instance, C:/Users/andre/Desktop/dotnet4/OnnxSample) and press ENTER.");
            Console.WriteLine("If the given directory is incorrect, the app gonna use default one.");
            string dir = Console.ReadLine();
            Recognition R = new Recognition();
            R.Run(dir);
            Console.WriteLine("\nTesting has passed successfully");
            return;
        }
    }
}
