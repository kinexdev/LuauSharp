using System;
using System.Reflection;
using System.Collections.Generic;

namespace LuauSharp.HighLevel
{
    public unsafe class Userdata
    {
        public LuauVM vm;

        public Userdata(LuauVM vm)
        {
            this.vm = vm;
        }

        /// <summary>
        /// Caches the type, and also exposes the type in the LuauVM so your able to access static methods and the constructor with 'New'.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterType<T>()
        {
            if (!LuauVM.aliveVMs.Contains((IntPtr)vm.luaState))
                return;

            Type t = typeof(T);
            string name = t.Name;

            Luau.PushValue(vm.luaState, Luau.LuaGlobalsindex);

            if (!t.IsEnum)
            {
                LuauInterop.TypeInformation typeInfo = new LuauInterop.TypeInformation();

                Luau.PushUserdata(vm.luaState, t);
                //metatable
                Luau.NewTable(vm.luaState);
                Luau.PushCFunction(vm.luaState, LuauInterop.IndexTypePtr);
                Luau.SetField(vm.luaState, -2, "__index");
                Luau.PushCFunction(vm.luaState, LuauInterop.NewIndexTypePtr);
                Luau.SetField(vm.luaState, -2, "__newindex");
                Luau.PushString(vm.luaState, name);
                Luau.SetField(vm.luaState, -2, "__type");
                Luau.SetMetatable(vm.luaState, -2);
                //stop metatable

                var instanceMethods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);

                if (typeInfo.cachedMethodInstanceCandidates == null)
                {
                    typeInfo.cachedMethodInstanceCandidates = new Dictionary<string, MethodInfo[]>();
                }

                foreach (var method in instanceMethods)
                {
                    if (!typeInfo.cachedMethodInstanceCandidates.TryGetValue(method.Name, out MethodInfo[] instanceCache))
                    {
                        typeInfo.cachedMethodInstanceCandidates[method.Name] = new[] { method };
                    }
                    else
                    {
                        Array.Resize(ref instanceCache, instanceCache.Length + 1);
                        instanceCache[^1] = method;
                        typeInfo.cachedMethodInstanceCandidates[method.Name] = instanceCache;
                    }
                }

                var staticMethods = t.GetMethods(BindingFlags.Static | BindingFlags.Public);

                if (typeInfo.cachedMethodStaticCandidates == null)
                {
                    typeInfo.cachedMethodStaticCandidates = new Dictionary<string, MethodInfo[]>();
                }


                foreach (var method in staticMethods)
                {
                    if (!typeInfo.cachedMethodStaticCandidates.TryGetValue(method.Name, out MethodInfo[] staticCache))
                    {
                        typeInfo.cachedMethodStaticCandidates[method.Name] = new[] { method };
                    }
                    else
                    {
                        Array.Resize(ref staticCache, staticCache.Length + 1);
                        staticCache[^1] = method;
                        typeInfo.cachedMethodStaticCandidates[method.Name] = staticCache;
                    }
                }

                var constructors = t.GetConstructors();
                typeInfo.cachedMethodConstructorCandidates = constructors;

                var instanceProperties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                typeInfo.cachedInstanceProperties = new Dictionary<string, PropertyInfo>();
                foreach (var prop in instanceProperties)
                {
                    typeInfo.cachedInstanceProperties[prop.Name] = prop;
                }

                var instanceFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
                typeInfo.cachedInstanceFields = new Dictionary<string, FieldInfo>();
                foreach (var field in instanceFields)
                {
                    typeInfo.cachedInstanceFields[field.Name] = field;
                }

                var staticProperties = t.GetProperties(BindingFlags.Static | BindingFlags.Public);
                typeInfo.cachedStaticProperties = new Dictionary<string, PropertyInfo>();
                foreach (var prop in staticProperties)
                {
                    typeInfo.cachedStaticProperties[prop.Name] = prop;
                }

                var staticFields = t.GetFields(BindingFlags.Static | BindingFlags.Public);
                typeInfo.cachedStaticFields = new Dictionary<string, FieldInfo>();
                foreach (var field in staticFields)
                {
                    typeInfo.cachedStaticFields[field.Name] = field;
                }

                LuauInterop.typeInfo.Add(t, typeInfo);
            } else
            {
                Luau.NewTable(vm.luaState);

                foreach(var value in Enum.GetValues(t))
                {
                    Luau.PushUserdata(vm.luaState, value);
                    Luau.SetField(vm.luaState, -2, Enum.GetName(t, value));
                }

                Luau.PushString(vm.luaState, name);
                Luau.SetField(vm.luaState, -2, "__type");
            }

            Luau.SetGlobal(vm.luaState, name);
            Luau.Pop(vm.luaState, 1);
        }
    }
}