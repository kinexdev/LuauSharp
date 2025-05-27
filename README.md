# LuauSharp
Very unsafe C# bindings for luau, built with flexibility, performance, support for AOT platforms and cross platform in mind.

# Benchmarks
The benchmarks I ran were to create 25000 C# userdata objects (managed) and call a function in that managed object without precompiling the source to bytecode. LuauSharp performed exceptionally well, it is the fastest lua/u interpreter and with zero allocs compared to baseline C# - It has no GC overhead! I used benchmarkdotnet for the benchmark.

![graph2](https://github.com/user-attachments/assets/d0da98af-1d95-4efe-b20d-3f324a029709)

# Low level
These luau bindings are really low level because they are built for speed and performance, not for convenience. LuauSharp is just a thin C# wrapper over the Luau C API. LuauSharp often forces you to use pointers for example the luaState or function pointers. Although its not just pure pointer usage, you also get some abstractions like for userdata or when you pass in a string. LuauSharp also uses no reflection for AOT and performance reasons - you need to do index and newindex manually.

# Set Up
To set up Luau-CSharp, you need the binaries. You could compile these yourself via the [cmake project](https://github.com/KinexDev/LuauSharpPInvoke) or you could use the precompiled binaries present in the repo, I only provide binaries for windows, you need to compile it for other platforms and it should work as I don't use any platform specific stuff.

After you imported all the scripts and binaries to your project, to make a simple program that prints `hello world` you do the following.

```cs
LuauNative.lua_State* luaState = Luau.New();
// this is optional
bool result = Luau.EnableCodegen(luaState);

Luau.DoString(luaState, "Script", "print(\"hello world!\")", result);
Luau.Close(luaState);
```

If you are using unity and il2cpp, add `LUAU_UNITY` to the Scripting Symbols for everything to work correctly.

# Functions
After the luaState is created, you can push custom C# functions to the luau VM via the `PushGlobalFunction` or `PushCFunction` function, it takes in a string for the name and a pointer to the function.

heres an example function

```cs
        // Create the function keep alive delegate
        private static readonly LuauNative.LuaCFunction PrintDelegateKA = Print;
        // Create the function pointer to that delegate
        private static readonly void* printPtr = (void*)Marshal.GetFunctionPointerForDelegate(PrintDelegateKA);

        // Create the actual lua function
#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.LuaCFunction))]
#endif
        public static int Print(LuauNative.lua_State* luaState)
        {
            int nargs = Luau.GetTop(luaState);

            for (int i = 1; i <= nargs; i++)
            {
                if (Luau.IsString(luaState, i))
                {
                    var s = Luau.GetString(luaState, i);
                    Console.WriteLine(s);
                }
                else
                {
                    var t = Luau.GetType(luaState, i);
                    switch (t)
                    {
                        case LuauNative.LuaType.Boolean:
                            var b = Luau.GetBoolean(luaState, i);
                            Console.WriteLine(b);
                            break;
                        case LuauNative.LuaType.Number:
                            var n = Luau.GetNumber(luaState, i);
                            Console.WriteLine(n);
                            break;
                        default:
                            Console.WriteLine(t);
                            break;
                    }
                }
            }

            return 0;
        }
```

the `[MonoPInvokeCallback(typeof(LuaFunction))]` attribute needs to be added and the method needs to be static if you are using IL2CPP.

then you can push the function globally.

```cs

LuauNative.lua_State* luaState = Luau.New();
...
Luau.PushGlobalFunction(luaState, "print", printPtr);
...
```

# Userdata
This is an example of a userdata with a index and a newindex function, these allow you to create a new instance of the userdata and modify it's variable called `number`. 

```cs
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
```

To make it exist in your luaState, you can call the `register` function provided in the userdata.

```cs

LuauNative.lua_State* luaState = Luau.New();
...
ExampleUserdata.Register(luaState);
...
```

Userdata can be any C# object, it can be a class, struct or a list, unmanaged object etc it can be anything.

# Notes
Most of the code is self documented and contains XML documents explaining what it does.
These bindings are intentionally unsafe and require you to understand low level C#, if you want safety or higher level abstraction. This library isn't for you.
