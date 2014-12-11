using System.Collections.Generic;
using System.Windows.Documents;
using EnvDTE;

namespace Orchardizer.UI
{
    public class MigrationsViewModel
    {
        public MigrationsViewModel(List<Migration> migrations, List<Model> classes)
        {
            Migrations = migrations;
            Classes = classes;
        }

        public List<Migration> Migrations { get; private set; }
        public List<Model> Classes { get; private set; }

        public Migration SelectedMigration { get; set; }
        public string NewMigration { get; set; }
        public Model SelectedClass { get; set; }
    }

    public class Model
    {
        public string Name { get; set; }
        public CodeType Class { get; set; }
    }
}
