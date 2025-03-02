namespace Luau_CSharp
{
    class Program
    {
        public VM vm;
    
        static void Main()
        {
            new Program();
        }

        public Program()
        {
            vm = new VM(Console.WriteLine);
            vm.RegisterUserdataType<Test>();
            vm.DoString(File.ReadAllText("example.luau"));
        }
    }   
}