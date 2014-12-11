using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace Orchardizer.UI
{
    public class Migration
    {
        public Migration(CodeType type)
        {
            CodeType = type;
            DisplayName = type.Name;
        }

        public CodeType CodeType { get; set; }
        public string DisplayName { get; set; }
    }
}
