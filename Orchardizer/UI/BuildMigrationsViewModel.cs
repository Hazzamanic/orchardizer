using System.Collections.Generic;
using System.Windows.Documents;
using EnvDTE;

namespace Orchardizer.UI
{
    public class BuildMigrationsViewModel
    {
        public BuildMigrationsViewModel(List<MigrationItem> migrations)
        {
            Migrations = migrations;
        }

        public bool UpdateTable { get; set; }
        public List<MigrationItem> Migrations { get; private set; }
    }

    public class MigrationItem
    {
        // whether to create the property in the migrations
        public bool Create { get; set; }

        // actual model name
        public string Name { get; set; }
        // suggested name e.g if you have a reference to another model called Comment then we want the name to be Comment_Id
        public string SuggestedName { get; set; }
        public string Type { get; set; }
        public string SuggestedType { get; set; }
        public bool Nullable { get; set; }
        public bool NotNull { get; set; }
        public string WithDefault { get; set; }


        public bool IsString
        {
            get { return Type == "string"; }
        }
        public int Length { get; set; }
        public bool Unlimited { get; set; }
    }
}
