namespace Orchardizer.UI
{
    public class ThemeViewModel
    {
        public string ThemeName { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public string BasedOn { get; set; }
        public bool CreateProject { get; set; }
        public bool Responsive { get; set; }
        public string Type { get; set; }
        public bool Codegen { get; set; }
        public string Version { get; set; }
        public string[] Versions
        {
            get
            {
                return OrchardVersions.Versions;
            }
        }

        public bool AntiCodegen
        {
            get { return !Codegen; }
        }

        public bool IncludeHelpFile { get; set; }
    }
}
