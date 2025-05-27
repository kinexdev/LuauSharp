namespace LuauSharp
{
    class Program
    {
        static unsafe void Main()
        {
            LuauNative.lua_State* luaState = Luau.New();
            bool result = Luau.EnableCodegen(luaState);
            ExampleUserdata.Register(luaState);

            LuauNative.lua_CompileOptions options = new LuauNative.lua_CompileOptions(2, 0, 1);
            Luau.DoFile(luaState, "script", "example.luau", result, options);
            Luau.Close(luaState);
        }
    }   
}