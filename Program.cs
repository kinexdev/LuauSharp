using LuauSharp.HighLevel;

namespace LuauSharp
{
    class Program
    {
        static unsafe void RunLowLevelExample()
        {
            Console.WriteLine("--- LuauSharp LowLevel ---");
            LuauNative.lua_State* luaState = Luau.New();
            bool result = Luau.EnableCodegen(luaState);
            ExampleUserdataLowLevel.Register(luaState);

            LuauNative.lua_CompileOptions options = new LuauNative.lua_CompileOptions(2, 0, 1);
            Luau.DoFile(luaState, "script", "example.luau", result, options);
            Luau.Close(luaState);
        }
        
        static unsafe void RunHighLevelExample()
        {
            Console.WriteLine("\n-- LuauSharp HighLevel --");
            using LuauVM vm = new();
            vm.userdata.RegisterType<ExampleUserdataHighLevel>();
            vm.DoFile("exampleHighLevel.luau");
        }
        
        static void Main()
        {
            RunLowLevelExample();
            RunHighLevelExample();
        }
    }   
}