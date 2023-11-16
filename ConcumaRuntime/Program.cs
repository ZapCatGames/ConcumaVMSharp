using ConcumaRuntimeFramework;

namespace ConcumaRuntime
{
    static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Not enough args.");
            }
            string inFile = args[0];
            byte[] bytes = File.ReadAllBytes(inFile);
            VM vm = new(bytes);
            vm.Run();
        }
    }
}