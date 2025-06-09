using System;
using System.Collections.Generic;
using System.IO;

namespace LuauSharp.HighLevel
{
    public unsafe class LuauVM : IDisposable
    {
        public static List<IntPtr> aliveVMs = new();

        public Globals globals { get; private set; }
        public Userdata userdata { get; private set; }
        private LuauNative.lua_State* _luaState;
        private LuauConfig _config;
        public bool disposed { get; private set; }

        public LuauNative.lua_State* luaState => _luaState;

        public class LuauConfig
        {
            public bool OpenLibs;
            public bool UseCodeGen;
            public int OptimizationLevel;
            public int DebugLevel;
            public int TypeInfoLevel;
            public int CoverageLevel;

            public LuauConfig(
                bool openLibs = true,
                bool useCodeGen = false,
                int optimizationLevel = 1,
                int debugLevel = 1,
                int typeInfoLevel = 2,
                int coverageLevel = 0)
            {
                OpenLibs = openLibs;
                UseCodeGen = useCodeGen;
                OptimizationLevel = optimizationLevel;
                DebugLevel = debugLevel;
                TypeInfoLevel = typeInfoLevel;
                CoverageLevel = coverageLevel;
            }

        }

        public LuauVM(LuauConfig config = null)
        {
            if (config == null)
                _config = new LuauConfig();
            else
                _config = config;

            globals = new Globals(this);
            userdata = new Userdata(this);
            _luaState = Luau.New(_config.OpenLibs);

            if (_config.UseCodeGen)
            {
                // enable codegen if its only supported
                _config.UseCodeGen = Luau.EnableCodegen(_luaState);
            }
            aliveVMs.Add((IntPtr)luaState);
        }

        ~LuauVM()
        {
            Dispose();
        }

        public byte[] Compile(string source)
        {
            LuauNative.lua_CompileOptions options = new LuauNative.lua_CompileOptions(_config.OptimizationLevel, _config.DebugLevel, _config.TypeInfoLevel, _config.CoverageLevel);
            return Luau.CompileToByteArray(source, options);
        }

        public object[] DoBytecode(byte[] bytecode, string chunkName = "chunk")
        {
            Luau.Load(_luaState, chunkName, bytecode, _config.UseCodeGen);
            Luau.Execute(luaState, 0, -1);
            return LuauInterop.GetArgumentsAsManaged(luaState);
        }

        public object[] DoFile(string filePath, string chunkName = "chunk")
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found: " + filePath);

            LuauNative.lua_CompileOptions options = new LuauNative.lua_CompileOptions(_config.OptimizationLevel, _config.DebugLevel, _config.TypeInfoLevel, _config.CoverageLevel);

            var source = File.ReadAllText(filePath);
            Luau.Load(luaState, chunkName, source, _config.UseCodeGen, options);
            Luau.Execute(luaState, 0, -1);
            return LuauInterop.GetArgumentsAsManaged(luaState);
        }

        public object[] DoString(string source, string chunkName = "chunk")
        {
            LuauNative.lua_CompileOptions options = new LuauNative.lua_CompileOptions(_config.OptimizationLevel, _config.DebugLevel, _config.TypeInfoLevel, _config.CoverageLevel);

            Luau.Load(luaState, chunkName, source, _config.UseCodeGen, options);
            Luau.Execute(luaState, 0, -1);
            return LuauInterop.GetArgumentsAsManaged(luaState);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            aliveVMs.Remove((IntPtr)luaState);
            globals = null;
            userdata = null;

            if (_luaState != null)
            {
                Luau.Close(_luaState);
                _luaState = null;
            }
        }
    }
}