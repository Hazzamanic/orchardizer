using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orchardization.UI
{
    public class ElementViewModel
    {
        public string ElementName { get; set; }
        public string Feature { get; set; }
        public string Properties { get; set; }
        public bool HasEditor { get; set; }
        public string EditorType { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
    }
}
