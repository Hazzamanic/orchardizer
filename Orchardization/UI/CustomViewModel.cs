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
    public class CustomViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">The code generation context</param>
        public CustomViewModel(CodeGenerationContext context)
        {
            Context = context;
        }

        public IEnumerable<Migration> Migrations
        {
            get
            {
                // too slow
                //ICodeTypeService codeTypeService = (ICodeTypeService) Context
                //    .ServiceProvider.GetService(typeof (ICodeTypeService));

                //var types =
                //    codeTypeService.GetAllCodeTypes(Context.ActiveProject)
                //        .Where(t => t.Kind == vsCMElement.vsCMElementClass);

                //foreach (var type in types)
                //{
                //    CodeClass cc = type as CodeClass;
                //    if (cc.Namespace == null)
                //        continue;
                //    if (cc.Namespace.Name.Contains(Context.ActiveProject.Name))
                //    {
                //        foreach (var d in type.Bases)
                //        {
                //            var dClass = d as CodeClass;
                //            var name = type.Name;
                //            if (dClass.Name == "DataMigrationImpl")
                //            {
                //                yield return new Migration(type);
                //            }

                //        }
                //    }
                //}

                var allClasses = GetProjectItems(Context.ActiveProject.ProjectItems).Where(v => v.Name.Contains(".cs"));
                // check for .cs extension on each

                foreach (var c in allClasses)
                {
                    var eles = c.FileCodeModel;
                    if (eles == null)
                        continue;
                    foreach (var ele in eles.CodeElements)
                    {
                        if (ele is EnvDTE.CodeNamespace)
                        {
                            var ns = ele as EnvDTE.CodeNamespace;
                            // run through classes
                            foreach (var property in ns.Members)
                            {
                                var member = property as CodeType;
                                if (member == null)
                                    continue;

                                foreach (var d in member.Bases)
                                {
                                    var dClass = d as CodeClass;
                                    if (dClass == null)
                                        continue;

                                    var name = member.Name;
                                    if (dClass.Name == "DataMigrationImpl")
                                    {
                                        yield return new Migration(member);
                                    }
                                }
                            }
                        }
                    }


                    //var clases = Context.ActiveProject.CodeModel.CodeElements;

                    //foreach (var e in clases)
                    //{
                    //    var cc = ce as CodeClass;
                    //    foreach (CodeInterface iface in cc.ImplementedInterfaces)
                    //    {
                    //        if(iface.Name == "")
                    //    }

                    //}

                    //return codeTypeService
                    //    .GetAllCodeTypes(Context.ActiveProject)
                    //    .Select(s => s.GetType())
                    //    .Where(x => x.IsAs)
                }
            }
        }


        public string PartName { get; set; }
        public string Properties { get; set; }
        public Migration SelectedMigration { get; set; }
        public string Storage { get; set; }
        public bool ShowAdminSummary { get; set; }
        public bool SiteSetting { get; set; }
        public string SiteSection { get; set; }
        public bool CreateMigrations { get; set; }
        public bool Attachable { get; set; }
        public bool CreateWidget { get; set; }
        public string WidgetName { get; set; }
        public string HelpText { get; set; }
        public string Migration { get; set; }
        public string Feature { get; set; }

        public CodeGenerationContext Context { get; private set; }

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
