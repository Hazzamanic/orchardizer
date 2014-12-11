using EnvDTE;
using Microsoft.AspNet.Scaffolding;
using Microsoft.AspNet.Scaffolding.EntityFramework;
using System.Collections.Generic;
using System.Linq;

namespace Orchardization.UI
{
    /// <summary>
    /// View model for code types so that it can be displayed on the UI.
    /// </summary>
    public class TypeSettingsViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">The code generation context</param>
        public TypeSettingsViewModel(CodeGenerationContext context)
        {
            Context = context;
        }

        public IEnumerable<string> Parts {
            get
            {
                ICodeTypeService codeTypeService = (ICodeTypeService)Context
                    .ServiceProvider.GetService(typeof(ICodeTypeService));

                var types = codeTypeService.GetAllCodeTypes(Context.ActiveProject).Where(t => t.Kind == vsCMElement.vsCMElementClass);

                foreach (var type in types)
                {
                    CodeClass cc = type as CodeClass;
                    if (cc.Namespace == null)
                        continue;
                    if (cc.Namespace.Name.Contains(Context.ActiveProject.Name))
                    {
                        foreach (var d in type.Bases)
                        {
                            var dClass = d as CodeClass;
                            var name = type.Name;
                            if (dClass.Name == "ContentPart")
                            {
                                yield return name;
                            }
                        }
                    }
                }
            }
        }

        public string PartName { get; set; }
        public string Properties { get; set; }

        public CodeGenerationContext Context { get; private set; }
    }

    
}
