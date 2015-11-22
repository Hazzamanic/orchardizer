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
    public class FieldViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public FieldViewModel()
        {
        }

        public string FieldName { get; set; }
        public string Properties { get; set; }
        public string Feature { get; set; }
        public bool IndexField { get; set; }        

        /// <summary>
        /// Recursively gets all the ProjectItem objects in a list of projectitems from a Project
        /// </summary>
        /// <param name="projectItems">The project items.</param>
        /// <returns></returns>
        public IEnumerable<ProjectItem> GetProjectItems(EnvDTE.ProjectItems projectItems)
        {
            foreach (EnvDTE.ProjectItem item in projectItems)
            {
                yield return item;

                if (item.SubProject != null)
                {
                    foreach (EnvDTE.ProjectItem childItem in GetProjectItems(item.SubProject.ProjectItems))
                        yield return childItem;
                }
                else
                {
                    foreach (EnvDTE.ProjectItem childItem in GetProjectItems(item.ProjectItems))
                        yield return childItem;
                }
            }

        }
    }
}
