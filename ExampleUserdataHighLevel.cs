namespace LuauSharp.HighLevel
{
    public class ExampleUserdataHighLevel
    {
        public float number;

        public ExampleUserdataHighLevel(float number)
        {
            this.number = number;
        }
        
        public void Print()
        {
            Console.WriteLine("C# : " + number + " number!");
        }
    }
}