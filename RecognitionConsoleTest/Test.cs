using System;
using DigitRecognitionLibrary;

namespace RecognitionConsoleTest
{
    class Test
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            Console.WriteLine("This code works in VS. Working in VS Code may require changes of paths for the onnx model and the directory with default images.");
            Console.WriteLine("Type a directory with images (for instance, C:/Users/andre/Desktop/dotnet4/OnnxSample) and press ENTER.");
            Console.WriteLine("If the given directory is incorrect, the app gonna use one with default images.");
            string dir = Console.ReadLine();
            Recognition R = new Recognition();
            R.Run(dir);
            Console.WriteLine("\nTesting has passed successfully");
            return;
        }
    }
}
