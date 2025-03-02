using System;

namespace Luau_CSharp
{
    // important luau metamethods supported (https://create.roblox.com/docs/luau/metatables), you can easily add support for more.
    public enum LuauOperators
    {
        Add,
        Sub,
        Mul,
        Div,
        ToString,
        Concat,
        Eq,
        Lt,
        Le
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class LuauMetamethod : Attribute
    {
        public LuauOperators Operator;
        
        public LuauMetamethod(LuauOperators op)
        {
            Operator = op;
        }
    }
    
    [AttributeUsage(AttributeTargets.Method)]
    public class LuauCallableFunction : Attribute
    {
        //public string Name;
        public bool _static;

        public LuauCallableFunction(bool _static = true)
        {
            //Name = name;
            this._static = _static;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class LuauVariable : Attribute
    {
        //public string Name { get; }

        public LuauVariable()
        {
            //Name = name;
        }
    }
}