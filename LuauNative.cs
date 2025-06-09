using System;
using System.Runtime.InteropServices;

namespace LuauSharp
{
    public static unsafe class LuauNative
    {
        public enum LuaType : uint
        {
            Nil = 0,
            Boolean = 1,
            LightUserData,
            Number,
            LUA_TVECTOR,
            String,
            Table,
            Function,
            UserData,
            LUA_TTHREAD,
            LUA_TPROTO,
            LUA_TUPVAL,
            LUA_TDEADKEY,
            LUA_T_COUNT = LUA_TPROTO,
        }
        
        /// <summary>
        /// the lua state
        /// taken from luauSharp
        /// https://github.com/TigersUniverse/LuauSharp/blob/main/LuauSharp/Luau.cs
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct lua_State
        {
            public byte tt;
            public byte marked;
            public byte memcat;
            public byte status;
            public byte activememcat;
            [MarshalAs(UnmanagedType.I1)]
            public bool isactive;
            [MarshalAs(UnmanagedType.I1)]
            public bool singlestep;

            public IntPtr top;
            public IntPtr @base;
            public IntPtr global;
            public IntPtr ci;
            public IntPtr stack_last;
            public IntPtr stack;
            public IntPtr end_ci;
            public IntPtr base_ci;

            public int stacksize;
            public int size_ci;

            public ushort nCcalls;
            public ushort baseCcalls;

            public int cachedslot;

            public IntPtr gt;
            public IntPtr openupval;
            public IntPtr gclist;
            public IntPtr namecall;
            public IntPtr userdata;
        }
        
        /// <summary>
        /// the compiler settings
        /// taken from luauSharp
        /// https://github.com/TigersUniverse/LuauSharp/blob/main/LuauSharp/Luau.cs
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct lua_CompileOptions
        {
            public int optimizationLevel;
            public int debugLevel;
            public int typeInfoLevel;
            public int coverageLevel;

            public IntPtr vectorLib;
            public IntPtr vectorCtor;
            public IntPtr vectorType;

            public IntPtr mutableGlobals;
            public int mutableGlobalsCount;

            public IntPtr userdataTypes;
            public int userdataTypesCount;

            public IntPtr librariesWithKnownMembers;
            public int librariesWithKnownMembersCount;

            public IntPtr libraryMemberTypeCb;
            public IntPtr libraryMemberConstantCb;

            public IntPtr disabledBuiltins;
            public int disabledBuiltinsCount;
            
            public lua_CompileOptions(
                int optimizationLevel = 1,
                int debugLevel = 1,
                int typeInfoLevel = 0,
                int coverageLevel = 0)
            {
                this.optimizationLevel = optimizationLevel;
                this.debugLevel = debugLevel;
                this.typeInfoLevel = typeInfoLevel;
                this.coverageLevel = coverageLevel;

                this.vectorLib = default;
                this.vectorCtor = default;
                this.vectorType = default;

                this.mutableGlobals = default;
                this.mutableGlobalsCount = default;

                this.userdataTypes = default;
                this.userdataTypesCount = default;

                this.librariesWithKnownMembers = default;
                this.librariesWithKnownMembersCount = default;

                this.libraryMemberTypeCb = default;
                this.libraryMemberConstantCb = default;

                this.disabledBuiltins = default;
                this.disabledBuiltinsCount = default;
            }
        }

        
        public const string LuauDLL = "luau.dll";

        [DllImport(LuauDLL)]
        public static extern lua_State* Luau_newstate();
        [DllImport(LuauDLL)]
        public static extern void Luau_sandbox(lua_State* L);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* Luau_compile(string source, lua_CompileOptions* options, int* size);
        [DllImport(LuauDLL)]
        public static extern void Luau_openlibs(lua_State* L);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Luau_load(lua_State* L, byte* chunkname, byte* data, int size, int env = 0);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Luau_pcall(lua_State* L, int nargs, int nresults, int errfunc);
        [DllImport(LuauDLL)]
        public static extern void Luau_close(lua_State* L);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte* Luau_tostring(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_isstring(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_isfunction(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnumber(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_pop(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_type(lua_State* L, int idx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaCFunction(lua_State* L);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Luau_pushcfunction(lua_State* L, void* fn);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Luau_pushcfunction(lua_State* L, LuaCFunction fn);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushvalue(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushboolean(lua_State* L, int b);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushstring(lua_State* L, byte* s);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushinteger(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushnil(lua_State* L);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushnumber(lua_State* L, double n);
        [DllImport(LuauDLL)]
        public static extern void Luau_setglobal(lua_State* L, byte* s);
        [DllImport(LuauDLL)]
        public static extern int Luau_getglobal(lua_State* L, byte* s);
        [DllImport(LuauDLL)]
        public static extern uint Luau_tounsigned(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_toboolean(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern LuaCFunction Luau_tocfunction(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_tointeger(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern double Luau_tonumber(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_gettop(lua_State* L);
        [DllImport(LuauDLL)]
        public static extern int Luau_getfield(lua_State* L, int idx, byte* s);
        [DllImport(LuauDLL)]
        public static extern void Luau_setfield(lua_State* L, int idx, byte* s);
        [DllImport(LuauDLL)]
        public static extern int Luau_ref(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_getref(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_unref(lua_State* L, int reference);
        [DllImport(LuauDLL)]
        public static extern void Luau_newtable(lua_State* L);
        [DllImport(LuauDLL)]
        public static extern void* Luau_newuserdata(lua_State* L, UIntPtr s);
        [DllImport(LuauDLL)]
        public static extern void Luau_error(lua_State* L);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushlightuserdata(lua_State* L, void* p); 
        [DllImport(LuauDLL)]
        public static extern void* Luau_touserdata(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_settable(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_gettable(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_setmetatable(lua_State* L, int objidx);
        [DllImport(LuauDLL)]
        public static extern int Luau_isboolean(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnil(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isuserdata(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_islightuserdata(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_istable(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnone(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnoneornil(lua_State* L, int n);
        [DllImport(LuauDLL)]
        public static extern void Luau_setsafeenv(lua_State* L, int objidx, int enabled);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UserdataDestructor(void* userdata);
        [DllImport(LuauDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void* Luau_newuserdatadtor(lua_State* L, int sz, void* dtor);
        [DllImport(LuauDLL)]
        public static extern int Luau_codegen_supported();        
        [DllImport(LuauDLL)]
        public static extern void Luau_codegen_create(lua_State* L);
        [DllImport(LuauDLL)]
        public static extern void Luau_codegen_compile(lua_State* L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_free(void* ptr);
    }
}