using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static Luau_CSharp.Luau;
#if LUAU_UNITY
using AOT;
#endif

namespace Luau_CSharp
{
    public class VM : IDisposable
    {
        private Action<object> print;
        private bool loaded;
        private string name;
        public IntPtr L;

        private Dictionary<IntPtr, GCHandle> objectHandles = new();

        //approach suggested in the comments of uh luau thing i was reading
        private static Dictionary<IntPtr, IntPtr> userdataToVMRefs = new();
        public static Dictionary<IntPtr, VM> vmRefs = new();
        public Dictionary<Type, CachedType> CachedTypes = new();

        public struct LuaRef
        {
            public int idx;
        }

        //cache type so we don't use reflection all the time especially on update
        public class CachedType
        {
            public Dictionary<string, LuaFunction> metamethods = new();
            public Dictionary<string, LuaFunction> functions = new();
            public Dictionary<string, FieldInfo> fields = new();
            public Dictionary<string, PropertyInfo> properties = new();
        }

        public static VM GetVMInstance(IntPtr L)
        {
            return vmRefs.GetValueOrDefault(L);
        }

        //Forwards a usuable type in luau, this allows all static methods to be accessed in 
        public void RegisterUserdataType<T>(string name = null) where T : class
        {
            var type = typeof(T);

            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
            }

            if (CachedTypes.ContainsKey(typeof(T)))
                throw new Exception("Type already registered");

            Luau_newtable(L);
            //var indexFunction = (LuaFunction)(_ => LuaIndexFunction(this, typeof(T), true));
            //GCHandle idxgch = GCHandle.Alloc(indexFunction);
            //objectHandles.Add((IntPtr)idxgch, idxgch);

            //var newIndexFunction = (LuaFunction)(_ => LuaNewIndexFunction(this, typeof(T), true));
            //GCHandle newgch = GCHandle.Alloc(newIndexFunction);
            //objectHandles.Add((IntPtr)newgch, newgch);

            Luau_newtable(L);
            //Luau_pushcfunction(L, indexFunction, typeof(T).FullName! + "__index");
            //Luau_setfield(L, -2, "__index");
            //Luau_pushcfunction(L, newIndexFunction, typeof(T).FullName! + "__newindex");
            //Luau_setfield(L, -2, "__newindex");
            //Luau_setmetatable(L, -2);
            // it broke for me so i resorted to doing this, static variables do not work here. also with the new stuff
            // im setting up the type caching here.
            var userdataType = new CachedType();
            var metamethods = type.GetMethods()
                .Where(m => m.IsDefined(typeof(LuauMetamethod), false));

            foreach (var metamethod in metamethods)
            {
                //these are supposed to last for the lifetime of the VM for obvious reasons, so i will just pin it
                var _delegate = (LuaFunction)Delegate.CreateDelegate(typeof(LuaFunction), metamethod);
                var metamethodattribute = (LuauMetamethod)metamethod.GetCustomAttribute(typeof(LuauMetamethod), false);
                var metamethodName = $"__{metamethodattribute.Operator.ToString().ToLower()}";
                userdataType.metamethods.Add(metamethodName, _delegate);
            }

            var methods = type.GetMethods()
                .Where(m => m.IsDefined(typeof(LuauCallableFunction), false));

            foreach (var method in methods)
            {
                var _delegate = (LuaFunction)Delegate.CreateDelegate(typeof(LuaFunction), method);
                userdataType.functions.Add(method.Name, _delegate);
                var attribute = (LuauCallableFunction)method.GetCustomAttribute(typeof(LuauCallableFunction), false);
                if (!attribute._static)
                    continue;
                Luau_pushcfunction(L, _delegate, method.Name);
                Luau_setfield(L, -2, method.Name);
            }

            var fields = type.GetFields()
                .Where(m => m.IsDefined(typeof(LuauVariable), false));

            foreach (var field in fields)
            {
                userdataType.fields.Add(field.Name, field);
            }

            var properties = type.GetProperties()
                .Where(m => m.IsDefined(typeof(LuauVariable), false));

            foreach (var property in properties)
            {
                userdataType.properties.Add(property.Name, property);
            }

            CachedTypes.Add(type, userdataType);
            Luau_setglobal(L, name);
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
        private LuaFunction PinnedLuaDelegate(LuaFunction function)
        {
            GCHandle gch = GCHandle.Alloc(function);

            objectHandles.Add((IntPtr)gch, gch);

            return function;
        }


        public VM(Action<object> printAc, bool openStandardLibs = true, string scriptName = null)
        {
            if (string.IsNullOrEmpty(scriptName))
                this.name = "Script";
            else
                this.name = name;

            print = printAc;
            L = Luau_newstate();
            Luau_setsafeenv(L, LUA_ENVIRONINDEX, 1);
            if (openStandardLibs)
                Luau_openlibs(L);
            vmRefs.Add(L, this);
            PushGlobalFunction("print", LuaPrint);
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
        private static int LuaPrint(IntPtr L)
        {
            var vm = GetVMInstance(L);
            int nargs = Luau_gettop(vm.L);
            for (int i = 1; i <= nargs; i++)
            {
                if (Luau_isstring(vm.L, i) == 1)
                {
                    var s = Luau_tostring(vm.L, i);
                    vm.print.Invoke(ptr_tostring(s));
                }
                else
                {
                    var t = Luau_type(vm.L, i);
                    switch ((LuaType)t)
                    {
                        case LuaType.Boolean:
                            var b = Luau_toboolean(vm.L, i);
                            vm.print.Invoke(b == 0 ? "false" : "true");
                            break;
                        case LuaType.Number:
                            var n = Luau_tonumber(vm.L, i);
                            vm.print.Invoke(n);
                            break;
                        default:
                            vm.print.Invoke((LuaType)t);
                            break;
                    }
                }
            }

            return 0;
        }

        public IntPtr Compile(string source, out byte[] bytecode)
        {
            var size = UIntPtr.Zero;
            var res = Luau_compile(source, ref size);

            //export bytecode
            var managedSize = (int)size.ToUInt32();
            bytecode = new byte[managedSize];
            Marshal.Copy(res, bytecode, 0, managedSize);
            return res;
        }

        public IntPtr Compile(string source, out UIntPtr size)
        {
            size = UIntPtr.Zero;
            var res = Luau_compile(source, ref size);
            return res;
        }

        public int Load(IntPtr bytecode, UIntPtr size)
        {
            if (loaded)
                throw new Exception("VM is already loaded");
            loaded = true;
            return Luau_load(L, name, bytecode, size, 0);
        }

        public void Execute(int results = 0, params object[] args)
        {
            foreach (var arg in args)
            {
                PushValueToStack(arg);
            }

            if (Luau_pcall(L, args.Length, results, 0) != 0)
            {
                var error = $"{ptr_tostring(Luau_tostring(L, -1))}";
                Luau_pop(L, 1);
                throw new Exception(error);
            }
        }

        public void ExecuteRef(LuaRef luaRef, int results = 0, params object[] args)
        {
            LoadRefToStack(luaRef);
            foreach (var arg in args)
            {
                PushValueToStack(arg);
            }

            if (Luau_pcall(L, args.Length, results, 0) != 0)
            {
                var error = $"{ptr_tostring(Luau_tostring(L, -1))}";
                Luau_pop(L, 1);
                throw new Exception(error);
            }

            Pop(1);
        }

        public void Pop(int index)
        {
            Luau_pop(L, index);
        }

        public void ExecuteFunction(string name, int results = 0, params object[] args)
        {
            Luau_getglobal(L, name);

            foreach (var arg in args)
            {
                PushValueToStack(arg);
            }

            if (Luau_pcall(L, args.Length, results, 0) != 0)
            {
                var error = $"{ptr_tostring(Luau_tostring(L, -1))}";
                Luau_pop(L, 1);
                throw new Exception(error);
            }
        }

        public LuaType GetGlobalType(string name)
        {
            Luau_getglobal(L, name);
            var type = (LuaType)Luau_type(L, -1);
            Luau_pop(L, 1);
            return type;
        }

        public void PushGlobalFunction(string name, LuaFunction fn)
        {
            Luau_pushvalue(L, LUA_GLOBALSINDEX);
            Luau_pushcfunction(L, PinnedLuaDelegate(fn), name);
            Luau_setglobal(L, name);
            Luau_pop(L, 1);
        }

        public void PushFunctionToStack(LuaFunction fn)
        {
            Luau_pushcfunction(L, PinnedLuaDelegate(fn), name);
        }

        public void PushGlobalUserdata<T>(string name, T obj) where T : class
        {
            if (!CachedTypes.ContainsKey(obj.GetType()))
                throw new Exception("Userdata type not registered");

            Luau_pushvalue(L, LUA_GLOBALSINDEX);

            PushLightUserdataToStack(obj);

            Luau_setglobal(L, name);
            Luau_pop(L, 1);
        }

        public void PushLightUserdataToStack<T>(T obj) where T : class
        {
            if (!CachedTypes.ContainsKey(obj.GetType()))
                throw new Exception("Userdata type not registered");

            GCHandle handle = GCHandle.Alloc(obj);
            IntPtr ptr = (IntPtr)handle;
            objectHandles[ptr] = handle;
            Luau_pushlightuserdata(L, ptr);

            RegisterMetatable(obj);
        }

        public void PushGlobalValue(string name, object obj)
        {
            Luau_pushvalue(L, LUA_GLOBALSINDEX);
            PushValueToStack(obj);
            Luau_setglobal(L, name);
            Luau_pop(L, 1);
        }

        //ion even know how this works its a miracle because it does get GC'd too i think at least from me creating 10000 objects on tick.
        public void PushUserdataToStack(object obj)
        {
            if (!CachedTypes.ContainsKey(obj.GetType()))
                throw new Exception("Userdata type not registered");

            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Normal);

            IntPtr userdata = Luau_newuserdatadtor(
                L,
                UIntPtr.Zero,
                UserdataDestructor
            );

            userdataToVMRefs.Add(userdata, L);

            //Luau_getglobal(L, registeredTypes[typeof(T)]);
            RegisterMetatable(obj);

            objectHandles[userdata] = handle;
        }

        //approach suggested in the comments of uh luau thing i was reading

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
        private static void UserdataDestructor(IntPtr userdataPtr)
        {
            if (userdataToVMRefs.ContainsKey(userdataPtr))
            {
                VM vm = GetVMInstance(userdataToVMRefs[userdataPtr]);
                vm.ClearUserdata(userdataPtr);

                userdataToVMRefs.Remove(userdataPtr);
            }
        }

        private void RegisterMetatable(object obj)
        {
            Luau_newtable(L);
            Luau_pushcfunction(L, LuaIndexFunction,
                obj.GetType().FullName! + "__index");
            Luau_setfield(L, -2, "__index");
            Luau_pushcfunction(L, LuaNewIndexFunction, obj.GetType().FullName! + "__newindex");
            Luau_setfield(L, -2, "__newindex");

            if (CachedTypes.TryGetValue(obj.GetType(), out var cachedType))
            {
                foreach (var method in cachedType.metamethods)
                {
                    Luau_pushcfunction(L, method.Value, "");
                    Luau_setfield(L, -2, method.Key);
                }
            }

            Luau_setmetatable(L, -2);
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
        private static int LuaIndexFunction(IntPtr L)
        {
            var vm = GetVMInstance(L);
            object obj = vm.GetUserdataRaw(Luau_touserdata(vm.L, 1));
            string memberName = ptr_tostring(Luau_tostring(vm.L, 2));

            if (!vm.CachedTypes.TryGetValue(obj.GetType(), out var cachedType))
                throw new Exception("Userdata type not registered");

            if (memberName == null || obj == null)
                throw new Exception("Cannot get member name or obj is null!");

            if (cachedType.functions.TryGetValue(memberName, out var _delegate))
            {
                Luau_pushcfunction(L, _delegate, memberName);
                return 1;
            }

            if (cachedType.fields.TryGetValue(memberName, out var field))
            {
                vm.PushValueToStack(field.GetValue(obj));
                return 1;
            }

            if (cachedType.properties.TryGetValue(memberName, out var property))
            {
                vm.PushValueToStack(property.GetValue(obj));
                return 1;
            }

            //var method = obj.GetType().GetMethod(memberName);

            //if (method != null && method.IsDefined(typeof(LuauCallableFunction), false))
            //{
            //    var _delegate = (LuaFunction)Delegate.CreateDelegate(typeof(LuaFunction), method);
            //    Luau_pushcfunction(L, _delegate, method.Name);
            //    return 1;
            //}

            //var field = obj.GetType().GetField(memberName);
            //if (field != null &&
            //    field.IsDefined(typeof(LuauVariable), false))
            //{
            //    vm.PushValueToStack(field.GetValue(obj));
            //    return 1;
            //}

            //var property = obj.GetType().GetProperty(memberName);
            //if (property != null && property.CanRead &&
            //    property.IsDefined(typeof(LuauVariable), false))
            //{
            //    vm.PushValueToStack(property.GetValue(obj));
            //    return 1;
            //}

            return 0;
        }

#if LUAU_UNITY
    [MonoPInvokeCallback(typeof(LuaFunction))]
#endif
        private static int LuaNewIndexFunction(IntPtr L)
        {
            var vm = GetVMInstance(L);
            var obj = vm.GetUserdataRaw(Luau_touserdata(L, 1));
            string memberName = ptr_tostring(Luau_tostring(L, 2));
            object newValue = GetLuaValue(vm, 3);
            if (!vm.CachedTypes.TryGetValue(obj.GetType(), out var cachedType))
                throw new Exception("Userdata type not registered");

            if (memberName == null || newValue == null || obj == null)
                throw new Exception("Cannot index new function because the obj, member, or value is null!");

            if (cachedType.fields.TryGetValue(memberName, out var field))
            {
                field.SetValue(obj, newValue);
                return 0;
            }

            if (cachedType.properties.TryGetValue(memberName, out var property))
            {
                property.SetValue(obj, newValue);
                return 0;
            }

            //var field = obj.GetType().GetField(memberName);
            //if (field != null && field.FieldType.IsInstanceOfType(newValue) && field.IsDefined(typeof(LuauVariable), false))
            //{
            //    field.SetValue(obj, newValue);
            //    return 0;
            //}

            //var property = obj.GetType().GetProperty(memberName);
            //if (property != null && property.CanWrite && property.PropertyType.IsInstanceOfType(newValue) && property.IsDefined(typeof(LuauVariable), false))
            //{
            //    property.SetValue(obj, newValue);
            //    return 0;
            //}
            return 0;
        }

        public static object GetLuaValue(VM vm, int index)
        {
            if (Luau_isnumber(vm.L, index) == 1)
                return Luau_tonumber(vm.L, index);
            if (Luau_isstring(vm.L, index) == 1)
                return ptr_tostring(Luau_tostring(vm.L, index));
            if (Luau_isboolean(vm.L, index) == 1)
                return Convert.ToBoolean(Luau_toboolean(vm.L, index));
            if (Luau_isfunction(vm.L, index) == 1)
                return Luau_tocfunction(vm.L, index);
            if (Luau_isuserdata(vm.L, index) == 1)
            {
                IntPtr userdataPtr = Luau_touserdata(vm.L, index);
                return vm.GetUserdataRaw(userdataPtr);
            }

            return null;
        }

        public string? ReadString(int index)
        {
            if (Luau_isstring(L, index) == 1)
            {
                return ptr_tostring(Luau_tostring(L, index));
            }

            return null;
        }

        public double? ReadNumber(int index)
        {
            if (Luau_isnumber(L, index) == 1)
            {
                return Luau_tonumber(L, index);
            }

            return null;
        }

        public bool? ReadBoolean(int index)
        {
            if (Luau_isboolean(L, index) == 1)
            {
                return Convert.ToBoolean(Luau_toboolean(L, index));
            }
            return null;
        }

        public T? ReadUserdata<T>(int index) where T : class
        {
            if (Luau_isuserdata(L, index) == 1)
            {
                var ptr = Luau_touserdata(L, index);
                return GetUserdata<T>(ptr);
            }

            return null;
        }

        public int GetArguments()
        {
            return Luau_gettop(L);
        }

        public bool IsFunction(int index)
        {
            return Luau_isfunction(L, index) == 1;
        }

        public bool IsTable(int index)
        {
            return Luau_istable(L, index) == 1;
        }

        public LuaRef StoreRef(int index)
        {
            return new LuaRef() { idx = Luau_ref(L, index) };
        }

        public int LoadRefToStack(LuaRef luaRef)
        {
            return Luau_getref(L, luaRef.idx);
        }

        public void DisposeRef(LuaRef luaRef)
        {
            Luau_unref(L, luaRef.idx);
        }

        public LuaType GetType(int index)
        {
            return (LuaType)Luau_type(L, index);
        }

        public void ClearUserdata(IntPtr ptr)
        {
            if (objectHandles.ContainsKey(ptr))
            {
                objectHandles[ptr].Free();
                objectHandles.Remove(ptr);
            }
        }

        public void ThrowError(string message)
        {
            Luau_pushstring(L, message);
            Luau_error(L);
        }

        public void PushValueToStack(object obj)
        {
            if (obj == null)
            {
                Luau_pushnil(L);
                return;
            }

            if (obj is float)
            {
                float floatValue = (float)obj;
                Luau_pushnumber(L, floatValue);
            }
            else if (obj is double)
            {
                double doubleValue = (double)obj;
                Luau_pushnumber(L, doubleValue);
            }
            else if (obj is int)
            {
                int intValue = (int)obj;
                Luau_pushnumber(L, intValue);
            }
            else if (obj is string)
            {
                string strValue = (string)obj;
                Luau_pushstring(L, strValue);
            }
            else if (obj is bool)
            {
                bool boolValue = (bool)obj;
                Luau_pushboolean(L, boolValue ? 1 : 0);
            }
            else if (obj is LuaFunction)
            {
                Luau_pushcfunction(L, (LuaFunction)obj, "");
            }
            else
            {
                PushUserdataToStack(obj);
            }
        }

        public object GetGlobal(string name)
        {
            Luau_getglobal(L, name);
            var returnValue = GetFromStack(-1);
            return returnValue;
        }

        public object GetFromStack(int idx)
        {
            var type = (LuaType)Luau_type(L, idx);

            switch (type)
            {
                case LuaType.Boolean:
                    return Convert.ToBoolean(Luau_toboolean(L, idx));
                    break;
                case LuaType.String:
                    return Luau_tostring(L, idx);
                    break;
                case LuaType.Number:
                    return Luau_tonumber(L, idx);
                    break;
                case LuaType.UserData or LuaType.LightUserData:
                    return GetUserdataRaw(Luau_touserdata(L, idx));
            }

            return null;
        }

        private T GetUserdata<T>(IntPtr ptr) where T : class
        {
            if (!CachedTypes.ContainsKey(typeof(T)))
                throw new Exception("Userdata type not registered");

            if (objectHandles.TryGetValue(ptr, out GCHandle handle))
            {
                return (T)handle.Target;
            }

            return null;
        }

        private object GetUserdataRaw(IntPtr ptr)
        {
            if (objectHandles.TryGetValue(ptr, out GCHandle handle))
            {
                return handle.Target;
            }

            return null;
        }

        public void DoString(string source)
        {
            var bytecode = Compile(source, out UIntPtr size);
            if (Load(bytecode, size) != 0)
                throw new Exception("VM load failed");
            else
                Execute();
        }

        public void DoByteCode(byte[] bytecode)
        {
            GCHandle pinnedArray = GCHandle.Alloc(bytecode, GCHandleType.Pinned);
            UIntPtr size = new UIntPtr((uint)bytecode.Length);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            if (Load(pointer, size) != 0)
            {
                pinnedArray.Free();
                throw new Exception("VM load failed");
            }
            else
                Execute();

            pinnedArray.Free();
        }

        public void Dispose()
        {
            Luau_close(L);
            vmRefs.Remove(L);
            foreach (var objects in objectHandles)
            {
                if (objects.Value.IsAllocated)
                    objects.Value.Free();
            }
        }
    }
}