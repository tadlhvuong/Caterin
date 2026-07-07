namespace Shared.Helpers
{
    public class StatusCssAttribute : Attribute
    {
        public string Name { get; private set; }

        public StatusCssAttribute(string name)
        {
            Name = name;
        }
    }
}
