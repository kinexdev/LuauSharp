using System;
using System.Runtime.InteropServices;

namespace Luau_CSharp
{
    public static class Luau
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
        
        public const int LUA_REGISTRYINDEX = (-8000 - 2000);
        public const int LUA_ENVIRONINDEX = (-8000 - 2001);
        public const int LUA_GLOBALSINDEX = (-8000 - 2002);

        public const string LuauDLL = "luau.dll";

        [DllImport(LuauDLL)]
        public static extern IntPtr Luau_newstate();
        
        [DllImport(LuauDLL)]
        public static extern void Luau_sandbox(IntPtr L);

        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Luau_compile(string source, ref UIntPtr size);

        [DllImport(LuauDLL)]
        public static extern void Luau_openlibs(IntPtr L);

        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Luau_load(IntPtr L, string chunkname, IntPtr data, UIntPtr size, int env);
        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Luau_pcall(IntPtr L, int nargs, int nresults, int errfunc);

        [DllImport(LuauDLL)]
        public static extern IntPtr Luau_close(IntPtr L);

        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Luau_tostring(IntPtr L, int index);

        [DllImport(LuauDLL)]
        public static extern int Luau_isstring(IntPtr L, int index);
        
        [DllImport(LuauDLL)]
        public static extern int Luau_isfunction(IntPtr L, int index);

        [DllImport(LuauDLL)]
        public static extern int Luau_isnumber(IntPtr L, int index);

        [DllImport(LuauDLL)]
        public static extern IntPtr Luau_pop(IntPtr L, int n);

        [DllImport(LuauDLL)]
        public static extern int Luau_type(IntPtr L, int idx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int LuaFunction(IntPtr L);

        [DllImport(LuauDLL, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Luau_pushcfunction(IntPtr L, LuaFunction fn, string name);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushvalue(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushboolean(IntPtr L, int b);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushstring(IntPtr L, string s);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushinteger(IntPtr L, int n);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushnil(IntPtr L);

        [DllImport(LuauDLL)]
        public static extern void Luau_pushnumber(IntPtr L, double n);

        [DllImport(LuauDLL)]
        public static extern void Luau_setglobal(IntPtr L, string s);

        [DllImport(LuauDLL)]
        public static extern int Luau_getglobal(IntPtr L, string s);

        [DllImport(LuauDLL)]
        public static extern uint Luau_tounsigned(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern int Luau_toboolean(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern LuaFunction Luau_tocfunction(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern int Luau_tointeger(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern double Luau_tonumber(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern int Luau_gettop(IntPtr L);

        [DllImport(LuauDLL)]
        public static extern int Luau_getfield(IntPtr L, int idx, string s);

        [DllImport(LuauDLL)]
        public static extern void Luau_setfield(IntPtr L, int idx, string s);

        [DllImport(LuauDLL)]
        public static extern int Luau_ref(IntPtr L, int idx);

        [DllImport(LuauDLL)]
        public static extern int Luau_getref(IntPtr L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_unref(IntPtr L, int reference);
        [DllImport(LuauDLL)]
        public static extern void Luau_newtable(IntPtr L);
        [DllImport(LuauDLL)]
        public static extern IntPtr Luau_newuserdata(IntPtr L, UIntPtr s);
        [DllImport(LuauDLL)]
        public static extern void Luau_error(IntPtr L);
        [DllImport(LuauDLL)]
        public static extern void Luau_pushlightuserdata(IntPtr L, IntPtr p); 
        [DllImport(LuauDLL)]
        public static extern IntPtr Luau_touserdata(IntPtr L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_settable(IntPtr L, int idx);
        [DllImport(LuauDLL)]
        public static extern int Luau_gettable(IntPtr L, int idx);
        [DllImport(LuauDLL)]
        public static extern void Luau_setmetatable(IntPtr L, int objIndex);
        [DllImport(LuauDLL)]
        public static extern int Luau_isboolean(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnil(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isuserdata(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_islightuserdata(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_istable(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnone(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern int Luau_isnoneornil(IntPtr L, int n);
        [DllImport(LuauDLL)]
        public static extern void Luau_setsafeenv(IntPtr L, int objindex, int enabled);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UserdataDestructor(IntPtr userdata);
        
        [DllImport(LuauDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Luau_newuserdatadtor(IntPtr L, UIntPtr sz, UserdataDestructor dtor);

        public static string? ptr_tostring(IntPtr ptr)
        {
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}