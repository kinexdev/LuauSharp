using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

#if LUAU_UNITY
using AOT;
using UnityEngine;
#endif

namespace LuauSharp.HighLevel
{
    public static unsafe class LuauInterop
    {
        public class TypeInformation
        {
            public Dictionary<string, MethodInfo[]> cachedMethodInstanceCandidates;
            public Dictionary<string, MethodInfo[]> cachedMethodStaticCandidates;
            public ConstructorInfo[] cachedMethodConstructorCandidates;
            public Dictionary<string, PropertyInfo> cachedInstanceProperties;
            public Dictionary<string, FieldInfo> cachedInstanceFields;
            public Dictionary<string, PropertyInfo> cachedStaticProperties;
            public Dictionary<string, FieldInfo> cachedStaticFields;
        }


        public static Dictionary<MethodInfo, ParameterInfo[]> cachedParameters = new();
        public static Dictionary<ConstructorInfo, ParameterInfo[]> cachedConstructorParameters = new();
        public static Dictionary<Type, TypeInformation> typeInfo = new();

        public static object GetLuaValueToManaged(LuauNative.lua_State* luaState, int index)
        {
            if (Luau.IsNumber(luaState, index))
                return Luau.GetNumber(luaState, index);
            if (Luau.IsString(luaState, index))
                return Luau.GetString(luaState, index);
            if (Luau.IsBoolean(luaState, index))
                return Luau.GetBoolean(luaState, index);
            if (Luau.IsFunction(luaState, index)) //this needs to be developed into a function class that contains the ref and allows you to call it.
            {
                int _ref = Luau.CreateRef(luaState, index);
                LuauFunction function = new LuauFunction(luaState, _ref);
                return function;
            }
            if (Luau.IsTable(luaState, index)) //this needs to be developed into a function class that contains the ref and allows you to call it.
            {
                int _ref = Luau.CreateRef(luaState, index);
                LuauTable function = new LuauTable(luaState, _ref);
                return function;
            }
            if (Luau.IsUserdata(luaState, index))
            {
                return Luau.GetUserdata(luaState, index);
            }

            return null;
        }

        private static readonly LuauNative.LuaCFunction CallDelegateKA = CallDelegate;
        private static readonly void* CallDelegatePtr = (void*)Marshal.GetFunctionPointerForDelegate(CallDelegateKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int CallDelegate(LuauNative.lua_State* luaState)
        {
            Delegate obj = Luau.GetUserdata<Delegate>(luaState, 1);
            if (obj == null)
                return 0;

            object[] args = GetArguments(luaState, 2);

            ParameterInfo[] _params = null;

            if (!cachedParameters.ContainsKey(obj.Method))
            {
                _params = obj.Method.GetParameters();
                cachedParameters.Add(obj.Method, _params);
            }
            else
            {
                _params = cachedParameters[obj.Method];
            }

            SetTypeInformation(_params, ref args);

            try
            {
                var returned = obj.DynamicInvoke(args);

                if (returned != null)
                {
                    PushManagedValueToLua(luaState, returned);
                    return 1;
                }
            }
            catch (Exception e)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: " + e.Message);
                Luau.Error(luaState);
            }
            return 0;
        }

        private static readonly LuauNative.LuaCFunction ConstructorStaticMethodKA = ConstructorStaticMethod;
        public static readonly void* ConstructorStaticMethodPtr = (void*)Marshal.GetFunctionPointerForDelegate(ConstructorStaticMethodKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int ConstructorStaticMethod(LuauNative.lua_State* luaState)
        {
            ConstructorInfo[] constructors = Luau.GetUserdata<ConstructorInfo[]>(luaState, 1);
            if (constructors == null)
                return 0;


            object[] args = GetArguments(luaState, 2);

            ConstructorInfo method = (ConstructorInfo)CompareMethodCandidates(constructors, args);

            if (method == null)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: No method candidates fit the arguments provided in the function call!");
                Luau.Error(luaState);
            }

            ParameterInfo[] _params = null;

            if (!cachedConstructorParameters.ContainsKey(method))
            {
                _params = method.GetParameters();
                cachedConstructorParameters.Add(method, _params);
            } else
            {
                _params = cachedConstructorParameters[method];
            }

            SetTypeInformation(_params, ref args);

            try
            {
                var returned = method.Invoke(args);

                if (returned != null)
                {
                    LuauInterop.PushManagedValueToLua(luaState, returned);
                    return 1;
                }
            }
            catch (Exception e)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: " + e.Message);
                Luau.Error(luaState);
            }
            return 0;
        }

        private static readonly LuauNative.LuaCFunction CallStaticMethodKA = CallStaticMethod;
        public static readonly void* CallStaticMethodPtr = (void*)Marshal.GetFunctionPointerForDelegate(CallStaticMethodKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int CallStaticMethod(LuauNative.lua_State* luaState)
        {
            MethodInfo[] methods = Luau.GetUserdata<MethodInfo[]>(luaState, 1);

            if (methods == null)
                return 0;

            object[] args = GetArguments(luaState, 2);

            MethodInfo method = (MethodInfo)CompareMethodCandidates(methods, args);

            if (method == null)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: No method candidates fit the arguments provided in the function call!");
                Luau.Error(luaState);
            }

            ParameterInfo[] _params = null;

            if (!cachedParameters.ContainsKey(method))
            {
                _params = method.GetParameters();
                cachedParameters.Add(method, _params);
            }
            else
            {
                _params = cachedParameters[method];
            }

            SetTypeInformation(_params, ref args);

            try
            {
                var returned = method.Invoke(null, args);

                if (returned != null)
                {
                    PushManagedValueToLua(luaState, returned);
                    return 1;
                }
            }
            catch (Exception e)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: " + e.Message);
                Luau.Error(luaState);
            }
            return 0;
        }

        public static void PushDelegate(LuauNative.lua_State* luaState, Delegate obj)
        {
            Luau.PushUserdata(luaState, obj);

            //metatable
            Luau.NewTable(luaState);
            Luau.PushCFunction(luaState, CallDelegatePtr);
            Luau.SetField(luaState, -2, "__call");
            Luau.PushString(luaState, "function");
            Luau.SetField(luaState, -2, "__type");
            Luau.SetMetatable(luaState, -2);
        }

        public static object[] GetArgumentsAsManaged(LuauNative.lua_State* luaState)
        {
            int argsCount = Luau.GetTop(luaState);
            object[] args = new object[argsCount];

            for (int i = 1; i <= argsCount; i++)
            {
                args[i - 1] = GetLuaValueToManaged(luaState, i);
            }

            return args;
        }


        private static readonly LuauNative.LuaCFunction CallInstanceMethodKA = CallInstanceMethod;
        private static readonly void* CallInstanceMethodPtr = (void*)Marshal.GetFunctionPointerForDelegate(CallInstanceMethodKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int CallInstanceMethod(LuauNative.lua_State* luaState)
        {
            MethodInfo[] methods = Luau.GetUserdata<MethodInfo[]>(luaState, 1);
            if (methods == null)
                return 0;

            object userdata = Luau.GetUserdata(luaState, 2);

            if (userdata == null)
                return 0;

            object[] args = GetArguments(luaState, 3);

            MethodInfo method = (MethodInfo)CompareMethodCandidates(methods, args);

            if (method == null)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: No method candidates fit the arguments provided in the function call!");
                Luau.Error(luaState);
            }

            ParameterInfo[] _params = null;

            if (!cachedParameters.ContainsKey(method))
            {
                _params = method.GetParameters();
                cachedParameters.Add(method, _params);
            }
            else
            {
                _params = cachedParameters[method];
            }

            SetTypeInformation(_params, ref args);

            try
            {
                var returned = method.Invoke(userdata, args);

                if (returned != null)
                {
                    PushManagedValueToLua(luaState, returned);
                    return 1;
                }
            }
            catch (Exception e)
            {
                Luau.PushString(luaState, "Invalid function call! \nReason: " + e.Message);
                Luau.Error(luaState);
            }
            return 0;
        }

        public static object[] GetArguments(LuauNative.lua_State* luaState, int starting)
        {
            int count = Luau.GetTop(luaState) - starting + 1;
            object[] args = new object[count];

            for (int x = 0; x < count; x++)
            {
                int i = x + starting;
                args[x] = LuauInterop.GetLuaValueToManaged(luaState, i);
            }

            return args;
        }

        public static void SetTypeInformation(ParameterInfo[] infos, ref object[] args)
        {
            for (int i = 0; i < infos.Length; i++)
            {
                var methodParam = infos[i].ParameterType;
                if (methodParam == typeof(int))
                {
                    if (args[i] is double d)
                        args[i] = Convert.ToInt32(d);
                }
                else if (methodParam == typeof(float))
                {
                    if (args[i] is double d)
                        args[i] = Convert.ToSingle(d);
                }
                else if (methodParam == typeof(string))
                {
                    if (args[i] is not string)
                        args[i] = args[i]?.ToString();
                }
            }
        }

        public static MethodBase CompareMethodCandidates(MethodBase[] candidates, object[] args)
        {
            foreach (MethodBase candidate in candidates)
            {
                ParameterInfo[] parameters = candidate.GetParameters();

                if (parameters.Length != args.Length)
                    continue;

                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var arg = args[i];

                    if (arg == null && paramType.IsValueType)
                    {
                        match = false;
                        break;
                    }

                    if (arg == null)
                        continue;

                    if ((arg is double && (paramType == typeof(int) || paramType == typeof(float))) || (arg.GetType() == paramType))
                    {
                        continue;
                    }
                    else
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return candidate;
            }

            return null;
        }

        private static readonly LuauNative.LuaCFunction IndexUserdataKA = IndexUserdata;
        public static readonly void* IndexUserdataPtr = (void*)Marshal.GetFunctionPointerForDelegate(IndexUserdataKA);


#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int IndexUserdata(LuauNative.lua_State* luaState)
        {
            object obj = Luau.GetUserdata(luaState, 1);

            if (obj == null)
                return 0;

            Type t = obj.GetType();

            string name = Luau.GetStringSafe(luaState, 2);

            TypeInformation information = typeInfo[t];

            if (information.cachedMethodInstanceCandidates.TryGetValue(name, out var methods))
            {
                Luau.PushUserdata(luaState, methods);

                //metatable
                Luau.NewTable(luaState);
                Luau.PushCFunction(luaState, CallInstanceMethodPtr);
                Luau.SetField(luaState, -2, "__call");
                Luau.PushString(luaState, "function");
                Luau.SetField(luaState, -2, "__type");
                Luau.SetMetatable(luaState, -2);
                return 1;
            }

            if (information.cachedInstanceFields.TryGetValue(name, out var field))
            {
                PushManagedValueToLua(luaState, field.GetValue(obj));
                return 1;
            }

            if (information.cachedInstanceProperties.TryGetValue(name, out var property))
            {
                if (!property.CanRead)
                    return 0;

                PushManagedValueToLua(luaState, property.GetValue(obj));
                return 1;
            }

            return 0;
        }

        private static readonly LuauNative.LuaCFunction NewIndexUserdataKA = NewIndexUserdata;
        public static readonly void* NewIndexUserdataPtr = (void*)Marshal.GetFunctionPointerForDelegate(NewIndexUserdataKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int NewIndexUserdata(LuauNative.lua_State* luaState)
        {
            object obj = Luau.GetUserdata(luaState, 1);

            if (obj == null)
                return 0;

            Type t = obj.GetType();

            string name = Luau.GetStringSafe(luaState, 2);

            TypeInformation information = typeInfo[t];

            if (information.cachedInstanceFields.TryGetValue(name, out var field))
            {
                object newObj = GetLuaValueToManaged(luaState, 3);

                var type = field.FieldType;

                if (type == typeof(int))
                {
                    if (newObj is double d)
                        newObj = Convert.ToInt32(d);
                }
                else if (type == typeof(float))
                {
                    if (newObj is double d)
                        newObj = Convert.ToSingle(d);
                }
                else if (type == typeof(string))
                {
                    if (newObj is not string)
                        newObj = newObj?.ToString();
                }

                field.SetValue(obj, newObj);
                return 0;
            }

            if (information.cachedInstanceProperties.TryGetValue(name, out var property))
            {
                if (!property.CanWrite)
                    return 0;

                object newObj = GetLuaValueToManaged(luaState, 3);

                var type = property.PropertyType;

                if (type == typeof(int))
                {
                    if (newObj is double d)
                        newObj = Convert.ToInt32(d);
                }
                else if (type == typeof(float))
                {
                    if (newObj is double d)
                        newObj = Convert.ToSingle(d);
                }
                else if (type == typeof(string))
                {
                    if (newObj is not string)
                        newObj = newObj?.ToString();
                }

                property.SetValue(obj, newObj);
                return 0;
            }

            return 0;
        }

        private static readonly LuauNative.LuaCFunction IndexTypeKA = IndexType;
        public static readonly void* IndexTypePtr = (void*)Marshal.GetFunctionPointerForDelegate(IndexTypeKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int IndexType(LuauNative.lua_State* luaState)
        {
            Type obj = Luau.GetUserdata<Type>(luaState, 1);

            if (obj == null)
                return 0;

            string name = Luau.GetStringSafe(luaState, 2);

            TypeInformation information = typeInfo[obj];

            if (name == "New")
            {
                if (information.cachedMethodConstructorCandidates.Length > 0)
                {
                    Luau.PushUserdata(luaState, information.cachedMethodConstructorCandidates);

                    //metatable
                    Luau.NewTable(luaState);
                    Luau.PushCFunction(luaState, ConstructorStaticMethodPtr);
                    Luau.SetField(luaState, -2, "__call");
                    Luau.PushString(luaState, "function");
                    Luau.SetField(luaState, -2, "__type");
                    Luau.SetMetatable(luaState, -2);
                    return 1;
                }
            }

            if (information.cachedMethodStaticCandidates.TryGetValue(name, out var methods))
            {
                Luau.PushUserdata(luaState, methods);

                //metatable
                Luau.NewTable(luaState);
                Luau.PushCFunction(luaState, CallStaticMethodPtr);
                Luau.SetField(luaState, -2, "__call");
                Luau.PushString(luaState, "function");
                Luau.SetField(luaState, -2, "__type");
                Luau.SetMetatable(luaState, -2);
                return 1;
            }

            if (information.cachedStaticFields.TryGetValue(name, out var field))
            {
                PushManagedValueToLua(luaState, field.GetValue(obj));
                return 1;
            }

            if (information.cachedStaticProperties.TryGetValue(name, out var property))
            {
                if (!property.CanRead)
                    return 0;

                PushManagedValueToLua(luaState, property.GetValue(obj));
                return 1;
            }

            return 0;
        }


        private static readonly LuauNative.LuaCFunction NewIndexTypeKA = NewIndexType;
        public static readonly void* NewIndexTypePtr = (void*)Marshal.GetFunctionPointerForDelegate(NewIndexTypeKA);

#if LUAU_UNITY
        [MonoPInvokeCallback(typeof(LuauNative.UserdataDestructor))]
#endif
        public static int NewIndexType(LuauNative.lua_State* luaState)
        {
            Type obj = Luau.GetUserdata<Type>(luaState, 1);

            if (obj == null)
                return 0;

            string name = Luau.GetStringSafe(luaState, 2);

            TypeInformation information = typeInfo[obj];

            if (information.cachedStaticFields.TryGetValue(name, out var field))
            {
                object newObj = GetLuaValueToManaged(luaState, 3);

                var type = field.FieldType;

                if (type == typeof(int))
                {
                    if (newObj is double d)
                        newObj = Convert.ToInt32(d);
                }
                else if (type == typeof(float))
                {
                    if (newObj is double d)
                        newObj = Convert.ToSingle(d);
                }
                else if (type == typeof(string))
                {
                    if (newObj is not string)
                        newObj = newObj?.ToString();
                }

                field.SetValue(obj, newObj);
                return 0;
            }

            if (information.cachedStaticProperties.TryGetValue(name, out var property))
            {
                if (!property.CanWrite)
                    return 0;

                object newObj = GetLuaValueToManaged(luaState, 3);

                var type = property.PropertyType;

                if (type == typeof(int))
                {
                    if (newObj is double d)
                        newObj = Convert.ToInt32(d);
                }
                else if (type == typeof(float))
                {
                    if (newObj is double d)
                        newObj = Convert.ToSingle(d);
                }
                else if (type == typeof(string))
                {
                    if (newObj is not string)
                        newObj = newObj?.ToString();
                }

                property.SetValue(obj, newObj);
                return 0;
            }

            return 0;
        }

        public static void GetOverload(LuauNative.lua_State* luaState, TypeInformation information, string opName, string metatableName)
        {
            if (information.cachedMethodStaticCandidates.TryGetValue(opName, out var candidates))
            {
                Luau.PushUserdata(luaState, candidates);

                //metatable
                Luau.NewTable(luaState);
                Luau.PushCFunction(luaState, CallStaticMethodPtr);
                Luau.SetField(luaState, -2, "__call");
                Luau.PushString(luaState, "function");
                Luau.SetField(luaState, -2, "__type");
                Luau.SetMetatable(luaState, -2);
                Luau.SetField(luaState, -2, metatableName);
            }
        }

        public static void PushManagedValueToLua(LuauNative.lua_State* luaState, object? obj)
        {
            if (obj == null)
            {
                Luau.PushNil(luaState);
                return;
            }

            if (obj is float)
            {
                float floatValue = (float)obj;
                Luau.PushNumber(luaState, floatValue);
            }
            else if (obj is double)
            {
                double doubleValue = (double)obj;
                Luau.PushNumber(luaState, doubleValue);
            }
            else if (obj is int)
            {
                int intValue = (int)obj;
                Luau.PushInteger(luaState, intValue);
            }
            else if (obj is string)
            {
                string strValue = (string)obj;
                Luau.PushString(luaState, strValue);
            }
            else if (obj is bool)
            {
                bool boolValue = (bool)obj;
                Luau.PushBoolean(luaState, boolValue);
            }
            else if (obj is Delegate)
            {
                Delegate d = (Delegate)obj;
                LuauInterop.cachedParameters.Add(d.Method, d.Method.GetParameters());
                PushDelegate(luaState, d);
            }
            else if (obj is LuauFunction)
            {
                LuauFunction function = (LuauFunction)obj;
                Luau.GetRef(luaState, function._ref);
            }
            else if (obj is LuauTable)
            {
                LuauTable table = (LuauTable)obj;
                Luau.GetRef(luaState, table._ref);
            }
            else
            {
                Type t = obj.GetType();

                if (t.IsEnum)
                {
                    Luau.PushUserdata(luaState, obj);
                    return;
                }
                
                if (!typeInfo.ContainsKey(t))
                {
                    Luau.PushString(luaState, $"Type {obj.GetType().Name} is not a registered type, cannot access userdata!");
                    Luau.Error(luaState);
                }

                Luau.PushUserdata(luaState, obj);

                TypeInformation information = typeInfo[t];
                //metatable
                Luau.NewTable(luaState);
                Luau.PushCFunction(luaState, IndexUserdataPtr);
                Luau.SetField(luaState, -2, "__index");
                Luau.PushCFunction(luaState, NewIndexUserdataPtr);
                Luau.SetField(luaState, -2, "__newindex");
                Luau.PushString(luaState, t.Name);
                Luau.SetField(luaState, -2, "__type");
                GetOverload(luaState, information, "op_Addition", "__add");
                GetOverload(luaState, information, "op_Subtraction", "__sub");
                GetOverload(luaState, information, "op_Multiply", "__mul");
                GetOverload(luaState, information, "op_Division", "__div");
                GetOverload(luaState, information, "op_UnaryNegation", "__unm");
                GetOverload(luaState, information, "op_Equality", "__eq");
                GetOverload(luaState, information, "op_LessThan", "__lt");
                GetOverload(luaState, information, "op_LessThanOrEqual", "__le");
                Luau.SetMetatable(luaState, -2);
            }
        }
    }
}