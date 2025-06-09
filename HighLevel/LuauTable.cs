using System;

namespace LuauSharp.HighLevel
{
    public unsafe class LuauTable : IDisposable
    {
        public int _ref { get; private set; }
        private LuauNative.lua_State* luaState;
        private bool disposed = false;

        public LuauTable(LuauVM vm)
        {
            luaState = vm.luaState;
            Luau.NewTable(luaState);
            _ref = Luau.CreateRef(luaState, Luau.GetTop(luaState));
            Luau.Pop(luaState, 1);
        }

        public LuauTable(LuauNative.lua_State* luaState, int _ref)
        {
            this.luaState = luaState;
            this._ref = _ref;
        }

        ~LuauTable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (!LuauVM.aliveVMs.Contains((IntPtr)luaState))
                return;

            disposed = true;
            Luau.DeleteRef(luaState, _ref);
        }

        public object? this[string key]
        {
            get
            {
                if (!LuauVM.aliveVMs.Contains((IntPtr)luaState))
                    return null;
                Luau.GetRef(luaState, _ref);
                Luau.GetField(luaState, -1, key);

                if (Luau.IsNoneOrNil(luaState, -1))
                {
                    Luau.Pop(luaState, 2);
                    return null;
                }

                var obj = LuauInterop.GetLuaValueToManaged(luaState, -1);
                Luau.Pop(luaState, 2);
                return obj;
            }
            set
            {
                if (!LuauVM.aliveVMs.Contains((IntPtr)luaState))
                    return;
                Luau.GetRef(luaState, _ref);
                LuauInterop.PushManagedValueToLua(luaState, value);
                Luau.SetField(luaState, -2, key);
                Luau.Pop(luaState, 1);
            }
        }
    }
}