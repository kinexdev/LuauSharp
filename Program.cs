namespace Luau_CSharp
{
    class Program
    {
        public static VM.LuaRef Ref;
        public static VM vm;
        
        static void Main()
        {
            Console.WriteLine("What example do you want to try? 1 = Normal example, 2 = GC example, 3 = Event Example");
            var key = Console.ReadKey().Key.ToString();
            var file = "example.luau";
            if (key == "D2")
                file = "GCExample.luau";
            else if (key == "D3")
                file = "EventExample.luau";
            Console.Write("\n");
            
            vm = new VM(Console.WriteLine);
            vm.RegisterUserdataType<Test>();
            vm.PushGlobalFunction("OnEvent", OnEvent);
            vm.DoString(File.ReadAllText(file));

            if (Ref.idx != 0)
            {
                Console.WriteLine("C# : Calling reference!");
                vm.ExecuteRef(Ref);
            }

            if (vm.GetGlobalType("_tick") == Luau.LuaType.Function)
            {
                for (int i = 0; i < 1000000; i++)
                {
                    vm.ExecuteFunction("_tick");
                }   
            }
            
            vm.Dispose();
        }
        
        // this is how you declare a function, with userdata you need to include the LuauCallableFunction attribute, if you are using IL2CPP you need the MonoPInvokeCallback(typeof(LuaFunction)) attribute, this was built with IL2CPP in mind so it is a little verbose
        public static int OnEvent(IntPtr L)
        {
            var vm = VM.GetVMInstance(L);
            if (vm.IsFunction(1))
            {
                Console.WriteLine("C# : A new reference to function.");
                Ref = vm.StoreRef(1);
            }
            else
                vm.ThrowError("Expected a function!");
            
            return 0;
        }
    }   
}