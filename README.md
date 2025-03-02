# Luau-CSharp
C# bindings for Luau that support IL2CPP

# Set Up
To set up Luau-CSharp, you need the binaries. You could compile these yourself via the [cmake project](https://github.com/KinexDev/Luau-CSharp-Build) or you could use the precompiled binaries present in the repo.

After you imported all the scripts and binaries to your project you need to initialize the VM, The VM manages & abstracts the luau state from you, these are not fully high-level bindings though, you still need to manage some of it by yourself.
to initialize the VM, it's the following

```cs
vm = new VM(Console.WriteLine);
```
> [!IMPORTANT]  
> Do not forget `vm.Dispose()` when you are with the vm!
>
> if you are using unity and il2cpp, add `LUAU_UNITY` to the Scripting Symbols.

The VM takes in 1 required argument, it is the print function that will be called by luau.

# Functions
After the vm is initialized, you can push custom C# functions to the luau VM via the `PushGlobalFunction` function, it takes in a string for the name and a `LuaFunction` delegate.

The `LuaFunction` delegate returns an `int` that returns the number of results it returned and takes in an `IntPtr` as the lua state, this will get turned into a function pointer.

heres an example function

```cs
        public static int print(IntPtr L)
        {
            var vm = VM.GetVMInstance(L);
            var msg = vm.ReadString(1);
            Console.WriteLine(msg);
            return 0;
        }
```


> [!IMPORTANT]  
> the `[MonoPInvokeCallback(typeof(LuaFunction))]` attribute needs to be added and the method needs to be static if you are using IL2CPP
> 
> if this is used on userdata, you also need to include the `[LuauCallableFunction]` attribute and it is automatically picked up through reflection.

this works in a similar way to luas function system, `var vm = VM.GetVMInstance(L);` is quite important when you are working with userdata, the vm abstracts pushing + reading objects and overall just making it easier, the most important methods in the `VM` are 
- `GetArguments`
- `ReadNumber`
- `ReadString`
- `ReadBoolean`,
- `ReadUserdata<T>`
- `PushValueToStack`

reading the objects require an index which start at 1 (luau uses 1 based indexes) and they return a nullable, when you push the objects it takes care of everything automatically for you including GC.

to register the function, it's the following

```cs
vm.PushGlobalFunction("Print", print);
```

# Userdata


> [!CAUTION]
> Be careful of what you expose.

To make a userdata class, it's the following

```cs
using Luau_CSharp;

public class Test
{
    [LuauVariable] public double number;

    public Test(float num)
    {
        number = num;
    }

    [LuauCallableFunction]
#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
    public static int New(IntPtr L)
    {
        var vm = VM.GetVMInstance(L);
        var value = vm.ReadNumber(1);
        if (value.HasValue)
        {
            var obj = new Test((float)value);
            vm.PushValueToStack(obj);
            return 1;
        }

        return 0;
    }

    [LuauCallableFunction]
#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
    public static int print(IntPtr L)
    {
        var vm = VM.GetVMInstance(L);
        var go = vm.ReadUserdata<Test>(1);
        go?.Print();
        return 0;
    }
    
    private void Print()
    {
        Console.WriteLine($"C# : userdata was called by luau! number is {number}.");
    }
}
```

the `LuauVariable` is used for registering variables in userdata.

after you are done with the userdata you forward the type to the luau VM

> [!IMPORTANT]  
> the `LuauVariable` is used for registering variables in userdata.
>
> any userdata not forwarded will throw an error.

```cs
vm.RegisterUserdataType<Test>();
```

# Executing a script
There is two ways to execute luau scripts.
- From string
- From bytecode

> [!NOTE]
> Executing from bytecode is faster as it's already been compiled but less dynamic

From now on the `Script` argument refers to script content.

# Do String
To execute a script from a string, it's the following.

```cs
vm.DoString(Script);
```

# Do Bytecode
To do bytecode, it first requires you to compile to bytecode. To compile a script to bytecode, it's the following.

```cs
vm.Compile(Script, out byte[] bytecode);
```

and then you can do the bytecode

```cs
vm.DoByteCode(bytecode);
```

> [!Note]
> DoString and DoByteCode expects nothing to be returned by default, there is a second argument for setting how much returned values there should be and then after that those values will be on the stack for you to manipulate.

# Where have they been used?
These bindings have been used in my own game engine called Bitq, and other of my projects.

# Considerations
- Since this project utilizes reflection heavily, it will probably not work in NAOT, it should work fine in IL2CPP and other platforms.
- This is only single threaded, it takes care of userdatas in a way thats single threaded (storing in lists) which will introduce errors if you try to multithread it! so if you have multiple scripts they all need to run on the same thread.
