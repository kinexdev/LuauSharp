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
        Console.WriteLine($"userdata was called by luau! number is {number}.");
    }
}