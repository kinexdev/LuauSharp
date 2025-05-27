using System.Runtime.InteropServices;
using System;
#if LUAU_UNITY
using AOT;
#endif

namespace LuauSharp
{
    public unsafe class ExampleUserdata
    {
        public float number;

        private static readonly LuauNative.LuaCFunction NewDelegateKA = New;
        private static readonly void* NewPtr = (void*)Marshal.GetFunctionPointerForDelegate(NewDelegateKA);

        private static readonly LuauNative.LuaCFunction IndexDelegateKA = Index;
        private static readonly void* IndexPtr = (void*)Marshal.GetFunctionPointerForDelegate(IndexDelegateKA);

        private static readonly LuauNative.LuaCFunction NewIndexDelegateKA = NewIndex;
        private static readonly void* NewIndexPtr = (void*)Marshal.GetFunctionPointerForDelegate(NewIndexDelegateKA);

        private static readonly LuauNative.LuaCFunction PrintDelegateKA = Print;
        private static readonly void* PrintPtr = (void*)Marshal.GetFunctionPointerForDelegate(PrintDelegateKA);

        public ExampleUserdata(float num)
        {
            number = num;
        }

        public static void Register(LuauNative.lua_State* luaState)
        {
            Luau.NewTable(luaState);
            Luau.PushCFunction(luaState, NewPtr);
            Luau.SetField(luaState, -2, "new");
            Luau.SetGlobal(luaState, "example");
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuauNative.LuaCFunction))]
#endif
        public static int New(LuauNative.lua_State* luaState)
        {
            double? number = Luau.GetNumberSafe(luaState, 1);

            if (number.HasValue)
            {
                ExampleUserdata exampleUserdata = new ExampleUserdata((float)number.Value);
                Luau.PushUserdata(luaState, exampleUserdata);

                //metatable
                Luau.NewTable(luaState);
                Luau.PushCFunction(luaState, IndexPtr);
                Luau.SetField(luaState, -2, "__index");
                Luau.PushCFunction(luaState, NewIndexPtr);
                Luau.SetField(luaState, -2, "__newindex");
                Luau.SetMetatable(luaState, -2);
                return 1;
            }

            return 0;
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuauNative.LuaCFunction))]
#endif
        public static int Index(LuauNative.lua_State* luaState)
        {
            ExampleUserdata userdata = Luau.GetUserdata<ExampleUserdata>(luaState, 1);

            if (userdata == null)
                return 0;
            
            byte* name = Luau.GetBytePtr(luaState, 2);

            if (Luau.StrCmp(name, "print") == 0)
            {
                Luau.PushCFunction(luaState, PrintPtr);
                return 1;
            }

            if (Luau.StrCmp(name, "number") == 0)
            {
                Luau.PushNumber(luaState, userdata.number);
                return 1;
            }

            return 0;
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuauNative.LuaCFunction))]
#endif
        public static int NewIndex(LuauNative.lua_State* luaState)
        {
            ExampleUserdata userdata = Luau.GetUserdata<ExampleUserdata>(luaState, 1);

            if (userdata == null)
                return 0;
            
            byte* name = Luau.GetBytePtr(luaState, 2);
            
            if (Luau.StrCmp(name, "number") == 0)
            {
                double? number = Luau.GetNumberSafe(luaState, 3);

                if (number.HasValue)
                {
                    userdata.number = (float)number.Value;
                }
            }

            return 0;
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuauNative.LuaCFunction))]
#endif
        public static int Print(LuauNative.lua_State* luaState)
        {
            ExampleUserdata number = Luau.GetUserdata<ExampleUserdata>(luaState, 1);

            if (number != null)
                Console.WriteLine("C# : " + number.number + " number!");
            else
                Console.WriteLine("C# : NULL!");

            return 0;
        }
    }
}