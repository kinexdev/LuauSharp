# LuauSharp High Level

Safe C# bindings for luau, built with convenience and support for IL2CPP in mind, these bindings are located in the `HighLevel` folder.

# Benchmarks

I haven't gotten a graph of the benchmark for this, but it performs x4-5 worse than LuauSharp Low Level, with 10x more GC allocations as they use reflection and boxing vigorously, keep in mind these bindings are not for performance, they are for developer convenience.

# Why
I wanted modding in my game, and for that i wanted to use luau which is a great language for that purpose, there is currently no actual luau C# bindings that are open source, have support for userdata and are as easy to use like this library, there are some other libraries but they fall short to this, this also works fully in IL2CPP through some hacks i did in this project (like all functions are actually userdata that contain a reference to that delegate) and other tricks.

# Set Up
To set up LuauSharp, you need the binaries. You could compile these yourself via the [cmake project](https://github.com/KinexDev/LuauSharpPInvoke) or you could use the precompiled binaries present in the repo, I only provide binaries for windows, you need to compile it for other platforms and it should work as I don't use any platform specific stuff.

After you imported all the scripts and binaries to your project, to make a simple program that prints `hello world` you do the following.

```cs
using LuauVM vm = new();
vm.globals["print"] = (Action<object>)Console.WriteLine;
vm.DoString("print(\"Hello World!\")");
```



DoString returns an object[] of managed C# objects from returned lua values, if you are using unity and IL2CPP, add `LUAU_UNITY` to the Scripting Symbols for everything to work correctly.

# Functions
After the luaState is created, you can push custom C# functions to the luau VM through globals as casting them to their delegate type, they work on instance methods too, they do not need to be static.

heres an example function

```cs
static void PrintHelloWorld()
{
    Console.WriteLine("Hello World!");
}
```

then you can push the function to the globals.

```cs
using LuauVM vm = new();
...
vm.globals["printHelloWorld"] = (Action)PrintHelloWorld;
...
```

# Userdata
This is an example of a userdata, these allow you to create a new instance of the userdata and modify it's variable called `number`, all of it's metamethods are registered when you register the type, so you don't need to worry about anything, the constructor in lua for C# userdata is `New`

```cs
namespace LuauSharp.HighLevel
{
    public class ExampleUserdataHighLevel
    {
        public float number;

        public ExampleUserdataHighLevel(float number)
        {
            this.number = number;
        }
        
        public void Print()
        {
            Console.WriteLine("C# : " + number + " number!");
        }
    }
}
```

Then you can register the userdata type to the LuauVM (it needs to be registered or else you will get an unknown type error)
```cs
using LuauVM vm = new();
...
vm.userdata.RegisterType<ExampleUserdataHighLevel>();
...
```

You can now instantiate it and use it in lua.

```lua
-- Calling Userdata
local x = ExampleUserdataHighLevel.New(10)
x:Print()
```

# Notes
There is still some things i need to iron out, there isn't any error trace when i throw an error currently and there isn't any api for coroutines and the codegen (i think i might of implemented it wrong? im not sure but im getting around same speeds so its obviously not working im guessing)
