using System;

namespace WhyAreMacs
{
    internal class Program
    {
        static void Main(string[] args)
        {
            foreach (var val in Enum.GetValues<Environment.SpecialFolder>())
            {
                Console.WriteLine($"For specialfolder {val}, got {Environment.GetFolderPath(val)}");
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
