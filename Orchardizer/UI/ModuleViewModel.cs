using System;

namespace Orchardizer.UI
{
    public class ModuleViewModel
    {
        public string Name { get; set; }
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
    }
}
