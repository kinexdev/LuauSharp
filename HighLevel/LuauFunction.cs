using System;

namespace LuauSharp.HighLevel
{
    public unsafe class LuauFunction : IDisposable
    {
        public int _ref { get; private set; }
        private LuauNative.lua_State* luaState;
        private bool disposed = false;

        public LuauFunction(LuauNative.lua_State* luaState, int _ref)
        {
            this.luaState = luaState;
            this._ref = _ref;
        }

        ~LuauFunction()
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

        public object[] Call(params object[] args)
        {
            if (disposed)
                return null;

            if (!LuauVM.aliveVMs.Contains((IntPtr)luaState))
                return null;

            var originalTop = Luau.GetTop(luaState);

            Luau.GetRef(luaState, _ref);

            foreach (object arg in args)
            {
                LuauInterop.PushManagedValueToLua(luaState, arg);
            }

            Luau.Execute(luaState, args.Length, -1);

            var newTop = Luau.GetTop(luaState);

            int numReturns = newTop - originalTop;

            object[] returnValues = new object[numReturns];

            for (int i = 0; i < numReturns; i++)
            {
                returnValues[i] = LuauInterop.GetLuaValueToManaged(luaState, originalTop + 1 + i);
            }

            Luau.Pop(luaState, numReturns);

            return returnValues;
        }
    }
}