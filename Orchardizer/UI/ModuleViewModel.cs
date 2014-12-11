namespace Orchardizer.UI
{
    public class ModuleViewModel
    {
        public string Name { get; set; }
        public bool Codegen { get; set; }

        public bool AntiCodegen
        {
            get { return !Codegen; }
        }
    }
}
