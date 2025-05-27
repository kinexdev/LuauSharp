using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if LUAU_UNITY
using AOT;
#endif

namespace LuauSharp
{
    public class LuauException : Exception
    {
        public LuauException(string message)
            : base(message) { }
    }

    public static unsafe class Luau
    {
        public const int LuaRegistryindex = (-8000 - 2000);
        public const int LuaEnvironindex = (-8000 - 2001);
        public const int LuaGlobalsindex = (-8000 - 2002);

        // Keep the delegate (function) alive.
        private static readonly LuauNative.UserdataDestructor DestructorDelegateKA = UserdataDestructor;

        /// <summary>
        /// Store a raw ptr to the destructor c dec so it never needs to be marshalled again.
        /// </summary>
        public static readonly void* DestructorPtr = (void*)Marshal.GetFunctionPointerForDelegate(DestructorDelegateKA);

        /// <summary>
        /// Luau bytecode result.
        /// </summary>
        public struct BytecodeResult
        {
            public byte* data;
            public int size;

            public BytecodeResult(byte* data, int size)
            {
                this.data = data;
                this.size = size;
            }
        }

        /// <summary>
        /// Holds a reference to a userdata.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct UserdataProxy
        {
            public IntPtr data;
        }

        /// <summary>
        /// Compiles the luau code into bytecode, the byte* in the BytecodeResult needs to be free'd via LuauNative.Luau_free or else there will be a memory leak.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static BytecodeResult Compile(string source, LuauNative.lua_CompileOptions options = default)
        {
            int size = 0;
            return new BytecodeResult(LuauNative.Luau_compile(source, &options, &size), size);
        }

        /// <summary>
        /// Compiles the object to a managed byte[] object
        /// </summary>
        /// <param name="source"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static byte[] CompileToByteArray(string source, LuauNative.lua_CompileOptions options = default)
        {
            BytecodeResult result = Compile(source, options);
            byte[] buffer = new byte[result.size];
            Marshal.Copy((IntPtr)result.data, buffer, 0, result.size);
            //this memory is allocated in cpp using memcpy so i need to free it
            LuauNative.Luau_free(result.data);
            return buffer;
        }

        /// <summary>
        /// Creates a new luau state with settings.
        /// </summary>
        /// <param name="openLibs">allow you to interface with luau standard library.</param>
        /// <returns></returns>
        public static LuauNative.lua_State* New(bool openLibs = true)
        {
            LuauNative.lua_State* luaState = LuauNative.Luau_newstate();
            if (openLibs)
                LuauNative.Luau_openlibs(luaState);

            return luaState;
        }

        /// <summary>
        /// Enables codegen on the lua state (JIT compilation), you need to enable this before using codegen when loading a script, returns true for a codegen instance being enabled, false for not supported.
        /// </summary>
        /// <param name="luaState"></param>
        public static bool EnableCodegen(LuauNative.lua_State* luaState)
        {
            if (LuauNative.Luau_codegen_supported() == 1)
            {
                LuauNative.Luau_codegen_create(luaState);
                return true;
            }
            return false;
        }

        /// <summary>
        /// loads a luau script and also frees memory from the bytecode result preventing a memory leak.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="chunkname"></param>
        /// <param name="str"></param>
        /// <param name="options"></param>
        public static void Load(LuauNative.lua_State* luaState, string chunkname, string str, bool useCodegen,
            LuauNative.lua_CompileOptions options = default)
        {
            BytecodeResult result = Compile(str, options);

            int length = Encoding.UTF8.GetByteCount(chunkname) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(chunkname, buffer);
            buffer[written] = 0;

            fixed (byte* namePtr = buffer)
            {
                if (LuauNative.Luau_load(luaState, namePtr, result.data, result.size) == 0)
                {
                    if (useCodegen)
                        LuauNative.Luau_codegen_compile(luaState, -1);
                }
                else
                    throw new LuauException("Failed to load chunk '" + chunkname + "'");
            }

            //this memory is allocated in cpp using memcpy so i need to free it
            LuauNative.Luau_free(result.data);
        }

        /// <summary>
        /// loads a luau script and also frees memory from the bytecode result preventing a memory leak.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="chunkname"></param>
        /// <param name="str"></param>
        /// <param name="options"></param>
        public static void Load(LuauNative.lua_State* luaState, string chunkname, byte[] bytecode, bool useCodegen)
        {
            int length = Encoding.UTF8.GetByteCount(chunkname) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(chunkname, buffer);
            buffer[written] = 0;

            int bytecodeLen = bytecode.Length;

            fixed (byte* bytecodePtr = bytecode)
            {
                fixed (byte* namePtr = buffer)
                {
                    if (LuauNative.Luau_load(luaState, namePtr, bytecodePtr, bytecodeLen) == 0)
                    {
                        if (useCodegen)
                            LuauNative.Luau_codegen_compile(luaState, -1);
                    }
                    else
                        throw new LuauException("Failed to load chunk '" + chunkname + "'");
                }
            }
        }

        /// <summary>
        /// Executes whatever is on the top of the stack
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="arguments"></param>
        /// <param name="results"></param>
        /// <exception cref="Exception"></exception>
        public static void Execute(LuauNative.lua_State* luaState, int arguments = 0, int results = 0)
        {
            if (LuauNative.Luau_pcall(luaState, arguments, results, 0) != 0)
            {
                var error = $"{Marshal.PtrToStringUTF8((IntPtr)LuauNative.Luau_tostring(luaState, -1))}";
                LuauNative.Luau_pop(luaState, 1);
                throw new LuauException(error);
            }
        }

        /// <summary>
        /// Get's the file, compiles the text into bytecode, loads the bytecode and executes it.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="chunkname"></param>
        /// <param name="source"></param>
        /// <param name="options"></param>
        public static void DoFile(LuauNative.lua_State* luaState, string chunkname, string filePath, bool useCodeGen = false,
            LuauNative.lua_CompileOptions options = default)
        {
            var text = File.ReadAllText(filePath);
            Load(luaState, chunkname, text, useCodeGen, options);
            Execute(luaState);
        }

        /// <summary>
        /// Compiles a string into bytecode, loads the bytecode and executes it.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="chunkname"></param>
        /// <param name="source"></param>
        /// <param name="options"></param>
        public static void DoString(LuauNative.lua_State* luaState, string chunkname, string source, bool useCodeGen = false,
            LuauNative.lua_CompileOptions options = default)
        {
            Load(luaState, chunkname, source, useCodeGen, options);
            Execute(luaState);
        }

        /// <summary>
        /// Loads the bytecode and executes it.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="chunkname"></param>
        /// <param name="source"></param>
        /// <param name="options"></param>
        public static void DoBytecode(LuauNative.lua_State* luaState, string chunkname, byte[] source, bool useCodeGen = false)
        {
            Load(luaState, chunkname, source, useCodeGen);
            Execute(luaState);
        }

        /// <summary>
        /// Creates a new userdata and adds it to the stack on the lua state, it also pins the userdata object in memory so it doesn't get deallocated by the GC until the destructor is called.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="userdataObject"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushUserdata(LuauNative.lua_State* luaState, object userdataObject)
        {
            void* ptr = LuauNative.Luau_newuserdatadtor(luaState, sizeof(UserdataProxy), DestructorPtr);
            GCHandle handle = GCHandle.Alloc(userdataObject, GCHandleType.Pinned);
            UserdataProxy* ud = (UserdataProxy*)ptr;
            ud->data = GCHandle.ToIntPtr(handle);
        }

        /// <summary>
        /// after the destructor is called free the GCHandle
        /// </summary>
        /// <param name="userdata"></param>
#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        private static void UserdataDestructor(void* userdata)
        {
            IntPtr ptr = ((UserdataProxy*)userdata)->data;
            GCHandle.FromIntPtr(ptr).Free();
        }

        /// <summary>
        /// Sets the current top element on the stack to the field of what is on the index, name is the name given to it.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetField(LuauNative.lua_State* luaState, int index, string name)
        {
            int length = Encoding.UTF8.GetByteCount(name) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(name, buffer);
            buffer[written] = 0;

            fixed (byte* namePtr = buffer)
            {
                LuauNative.Luau_setfield(luaState, index, namePtr);
            }
        }

        /// <summary>
        /// Gets the field from the index with the name given to it.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetField(LuauNative.lua_State* luaState, int index, string name)
        {
            int length = Encoding.UTF8.GetByteCount(name) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(name, buffer);
            buffer[written] = 0;

            fixed (byte* namePtr = buffer)
            {
                LuauNative.Luau_getfield(luaState, index, namePtr);
            }
        }

        /// <summary>
        /// Creates a new luau state with settings.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Close(LuauNative.lua_State* luaState) => LuauNative.Luau_close(luaState);

        /// <summary>
        /// Get's a global from name and puts it on the stack.
        /// </summary>
        /// <param name="luaState"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetGlobal(LuauNative.lua_State* luaState, string name)
        {
            int length = Encoding.UTF8.GetByteCount(name) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(name, buffer);
            buffer[written] = 0;

            fixed (byte* namePtr = buffer)
            {
                LuauNative.Luau_getglobal(luaState, namePtr);
            }
        }

        /// <summary>
        /// Set's a global with the name.
        /// </summary>
        /// <param name="luaState"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetGlobal(LuauNative.lua_State* luaState, string name)
        {
            int length = Encoding.UTF8.GetByteCount(name) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(name, buffer);
            buffer[written] = 0;

            fixed (byte* namePtr = buffer)
            {
                LuauNative.Luau_setglobal(luaState, namePtr);
            }
        }

        /// <summary>
        /// Creates a new table in the stack.
        /// </summary>
        /// <param name="luaState"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewTable(LuauNative.lua_State* luaState) => LuauNative.Luau_newtable(luaState);

        /// <summary>
        /// Creates a new table in the stack.
        /// </summary>
        /// <param name="luaState"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMetatable(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_setmetatable(luaState, index);

        /// <summary>
        /// Returns how many elements are at the top of the stack, this could be used to get the count of arguments in a function call.
        /// </summary>
        /// <param name="luaState"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTop(LuauNative.lua_State* luaState) => LuauNative.Luau_gettop(luaState);

        /// <summary>
        /// Pop the element from the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pop(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_pop(luaState, index);

        /// <summary>
        /// pushes a function globally with the name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushGlobalFunction(LuauNative.lua_State* luaState, string name, void* funcPtr)
        {
            PushValue(luaState, LuaGlobalsindex);
            PushCFunction(luaState, funcPtr);
            SetGlobal(luaState, name);
            Pop(luaState, 1);
        }

        /// <summary>
        /// pushes a function globally with the name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushGlobalUserdata(LuauNative.lua_State* luaState, string name, object userdata)
        {
            PushValue(luaState, LuaGlobalsindex);
            PushUserdata(luaState, userdata);
            SetGlobal(luaState, name);
            Pop(luaState, 1);
        }

        /// <summary>
        /// Pushes a C function via the pointer to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushCFunction(LuauNative.lua_State* luaState, void* functionPtr) => LuauNative.Luau_pushcfunction(luaState, functionPtr);

        /// <summary>
        /// Pushes a number to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushNumber(LuauNative.lua_State* luaState, double number) => LuauNative.Luau_pushnumber(luaState, number);

        /// <summary>
        /// Pushes a number to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushInteger(LuauNative.lua_State* luaState, int integer) => LuauNative.Luau_pushinteger(luaState, integer);

        /// <summary>
        /// Pushes nil to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushNil(LuauNative.lua_State* luaState) => LuauNative.Luau_pushnil(luaState);

        /// <summary>
        /// Pushes a string to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushString(LuauNative.lua_State* luaState, string str)
        {
            int length = Encoding.UTF8.GetByteCount(str) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(str, buffer);
            buffer[written] = 0;

            fixed (byte* strPtr = buffer)
            {
                LuauNative.Luau_pushstring(luaState, strPtr);
            }
        }

        /// <summary>
        /// Pushes a byte* string to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushString(LuauNative.lua_State* luaState, byte* str) => LuauNative.Luau_pushstring(luaState, str);

        /// <summary>
        /// Pushes a boolean to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushBoolean(LuauNative.lua_State* luaState, bool boolean) => LuauNative.Luau_pushboolean(luaState, Convert.ToInt32(boolean));

        /// <summary>
        /// Depulicates the value from the index and pushes it to the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushValue(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_pushvalue(luaState, index);

        /// <summary>
        /// Throws a luau error.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(LuauNative.lua_State* luaState) => LuauNative.Luau_error(luaState);

        /// <summary>
        /// Gets the userdata from the index from the stack, this returns null if there was no userdata (safe).
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetUserdataSafe<T>(LuauNative.lua_State* luaState, int index) where T : class
        {
            if (LuauNative.Luau_isuserdata(luaState, index) == 1)
            {
                var ud = (UserdataProxy*)LuauNative.Luau_touserdata(luaState, index);
                return (T?)GCHandle.FromIntPtr(ud->data).Target;
            }

            return null;
        }

        /// <summary>
        /// Gets the userdata from the index from the stack, this returns null if there was no userdata (safe).
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetUserdataSafe(LuauNative.lua_State* luaState, int index)
        {
            if (LuauNative.Luau_isuserdata(luaState, index) == 1)
            {
                var ud = (UserdataProxy*)LuauNative.Luau_touserdata(luaState, index);
                return GCHandle.FromIntPtr(ud->data).Target;
            }

            return null;
        }

        /// <summary>
        /// Gets the string from the index from the stack, this returns null if there was no string (safe).
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? GetStringSafe(LuauNative.lua_State* luaState, int index)
        {
            if (LuauNative.Luau_isstring(luaState, index) == 1)
            {
                return Marshal.PtrToStringUTF8((IntPtr)(LuauNative.Luau_tostring(luaState, index)));
            }

            return null;
        }

        /// <summary>
        /// Gets the number from the index from the stack, this returns null if there was no number (safe).
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? GetNumberSafe(LuauNative.lua_State* luaState, int index)
        {
            if (LuauNative.Luau_isnumber(luaState, index) == 1)
            {
                return LuauNative.Luau_tonumber(luaState, index);
            }

            return null;
        }

        /// <summary>
        /// Gets the boolean from the index from the stack, this returns null if there was no boolean (safe).
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? GetBooleanSafe(LuauNative.lua_State* luaState, int index)
        {
            if (LuauNative.Luau_isboolean(luaState, index) == 1)
            {
                return LuauNative.Luau_toboolean(luaState, index) != 0;
            }
            return null;
        }

        /// <summary>
        /// Gets the userdata from the index from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetUserdata<T>(LuauNative.lua_State* luaState, int index) where T : class
        {
            var ud = (UserdataProxy*)LuauNative.Luau_touserdata(luaState, index);
            return (T)GCHandle.FromIntPtr(ud->data).Target!;
        }

        /// <summary>
        /// Gets the userdata from the index from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetUserdata(LuauNative.lua_State* luaState, int index)
        {
            var ud = (UserdataProxy*)LuauNative.Luau_touserdata(luaState, index);
            return GCHandle.FromIntPtr(ud->data).Target;
        }

        /// <summary>
        /// Gets the string from the index from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? GetString(LuauNative.lua_State* luaState, int index) =>
            Marshal.PtrToStringUTF8((IntPtr)(LuauNative.Luau_tostring(luaState, index)));

        /// <summary>
        /// Gets a string in raw byte* form from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* GetBytePtr(LuauNative.lua_State* luaState, int index) =>
            LuauNative.Luau_tostring(luaState, index);

        /// <summary>
        /// Gets the number from the index from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetNumber(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_tonumber(luaState, index);

        /// <summary>
        /// Gets the boolean from the index from the stack with no safety.
        /// </summary>
        /// <param name="luaState"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBoolean(LuauNative.lua_State* luaState, int index) => Convert.ToBoolean(LuauNative.Luau_toboolean(luaState, index));

        /// <summary>
        /// Checks if the value from the index is a booleanb.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBoolean(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isboolean(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is a numbner.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumber(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isnumber(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is nil.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNil(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isnil(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is a function.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFunction(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isfunction(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is nil.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNone(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isnone(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is a userdata.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUserdata(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isuserdata(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is none or nil.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOrNil(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isnoneornil(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is a string.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_isstring(luaState, index) == 1;

        /// <summary>
        /// Checks if the value from the index is a table.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTable(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_istable(luaState, index) == 1;

        /// <summary>
        /// Creates a reference to the object in the stack based on index.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CreateRef(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_ref(luaState, index);

        /// <summary>
        /// Gets the object from the ref and pushes it on the stack.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRef(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_getref(luaState, index);

        /// <summary>
        /// Deletes the ref.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteRef(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_unref(luaState, index);

        /// <summary>
        /// Gets the type of 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuauNative.LuaType GetType(LuauNative.lua_State* luaState, int index) => (LuauNative.LuaType)LuauNative.Luau_type(luaState, index);

        /// <summary>
        /// Gets the type of 
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeRaw(LuauNative.lua_State* luaState, int index) => LuauNative.Luau_type(luaState, index);

        /// <summary>
        /// Compares two char*'s, similar to C strcmp.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrCmp(byte* s1, byte* s2)
        {
            while (*s1 != '\0' && *s1 == *s2)
            {
                s1++;
                s2++;
            }
            return *s1 - *s2;
        }

        /// <summary>
        /// Compares two char*'s, similar to C strcmp.
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int StrCmp(byte* s1, string s2s)
        {
            int length = Encoding.UTF8.GetByteCount(s2s) + 1;
            Span<byte> buffer = length <= 256 ? stackalloc byte[length] : new byte[length];
            int written = Encoding.UTF8.GetBytes(s2s, buffer);
            buffer[written] = 0;

            fixed (byte* ptr = buffer)
            {
                byte* s2 = ptr;
                while (*s1 != '\0' && *s1 == *s2)
                {
                    s1++;
                    s2++;
                }
                return *s1 - *s2;
            }
        }
    }
}