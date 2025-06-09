using System;

namespace LuauSharp.HighLevel
{
    public unsafe class Globals
    {
        public LuauVM vm;

        public Globals(LuauVM vm)
        {
            this.vm = vm;
        }

        public object? this[string key]
        {
            get
            {
                if (!LuauVM.aliveVMs.Contains((IntPtr)vm.luaState))
                    return null;

                Luau.GetGlobal(vm.luaState, key);

                if (Luau.IsNoneOrNil(vm.luaState, -1))
                {
                    Luau.Pop(vm.luaState, 1);
                    return null;
                }

                var obj = LuauInterop.GetLuaValueToManaged(vm.luaState, -1);
                Luau.Pop(vm.luaState, 1);
                return obj;
            }
            set
            {
                if (!LuauVM.aliveVMs.Contains((IntPtr)vm.luaState))
                    return;

                Luau.PushValue(vm.luaState, Luau.LuaGlobalsindex);
                LuauInterop.PushManagedValueToLua(vm.luaState, value);
                Luau.SetGlobal(vm.luaState, key);
                Luau.Pop(vm.luaState, 1);
            }
        }
    }
}