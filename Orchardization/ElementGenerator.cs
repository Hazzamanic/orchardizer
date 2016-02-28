using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using EnvDTE;

namespace Orchardization
{
    public class ElementGenerator : CodeGenerator
    {
        ElementViewModel _viewModel;

        /// <summary>
        /// Constructor for the custom code generator
        /// </summary>
        /// <param name="context">Context of the current code generation operation based on how scaffolder was invoked(such as selected project/folder) </param>
        /// <param name="information">Code generation information that is defined in the factory class.</param>
        public ElementGenerator(
            CodeGenerationContext context,
            CodeGeneratorInformation information)
            : base(context, information)
        {
            _viewModel = new ElementViewModel();
        }


        /// <summary>
        /// Any UI to be displayed after the scaffolder has been selected from the Add Scaffold dialog.
        /// Any validation on the input for values in the UI should be completed before returning from this method.
        /// </summary>
        /// <returns></returns>
        public override bool ShowUIAndValidate()
        {
            // Bring up the selection dialog and allow user to select a model type
            ElementWindow window = new ElementWindow(_viewModel);
            bool? showDialog = window.ShowDialog();

            Validate();

            return showDialog ?? false;
        }

        /// <summary>
        /// Validates the users inputs.
        /// </summary>
        public void Validate()
        {
            
        }

        /// <summary>
        /// This method is executed after the ShowUIAndValidate method, and this is where the actual code generation should occur.
        /// In this example, we are generating a new file from t4 template based on the ModelType selected in our UI.
        /// </summary>
        public override void GenerateCode()
        {
            var name = _viewModel.ElementName.Trim();

            var props = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(_viewModel.Properties) || !String.IsNullOrWhiteSpace(_viewModel.Properties))
            {
                props = Regex.Split(_viewModel.Properties, "\\n")
                            .Select(x => x.Split(':'))
                            .Where(x => x.Length > 1 && !String.IsNullOrEmpty(x[0].Trim()) && !String.IsNullOrEmpty(x[1].Trim()))
                            .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
            }

            var firstProperty = props.FirstOrDefault();
            var hasProperties = firstProperty.Key != null;
            string firstType = "";
            string firstKey = "";
            if (hasProperties)
            {
                firstKey = firstProperty.Key;
                firstType = firstProperty.Value;
            }

            bool formsEditor = _viewModel.EditorType == null ? false : _viewModel.EditorType.Contains("Forms API");
            var namePlusElement = name.EndsWith("element", StringComparison.InvariantCultureIgnoreCase) ? name : name + "Element"; // the element name with element appended onto it

            // Setup the scaffolding item creation parameters to be passed into the T4 template. We'll just use one for everything
            var parameters = new Dictionary<string, object>()
            {
                { "ElementName", name },
                { "NamePlusElement", namePlusElement },
                { "Module", Context.ActiveProject.Name },
                { "Properties", props },
                { "PropCount", props.Count() },
                { "HasProperty", hasProperties },
                { "FirstKey", firstKey },
                { "FirstType", firstType },
                { "Category", String.IsNullOrWhiteSpace(_viewModel.Category) ? "Content" : _viewModel.Category },
                { "Description", String.IsNullOrWhiteSpace(_viewModel.Description) ? $"Add a {name} to the layout" : _viewModel.Description },
                { "Feature", String.IsNullOrWhiteSpace(_viewModel.Feature) ? "" : _viewModel.Feature },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  },
                { "HasEditor", _viewModel.HasEditor },
                { "FormsEditor", formsEditor }
            };

            // make sure references are there
            var vsproject = Context.ActiveProject.Object as VSLangProj.VSProject;
            vsproject.References.Add("Orchard.Core");
            vsproject.References.Add("Orchard.Framework");

            Project orchardForms = null;
            Project orchardLayouts = null;

            var formProject = Context.ActiveProject.UniqueName;
            var projects = vsproject.DTE.Solution.Projects;
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var p = item.Current as Project;
                if (p.Name != "Modules")
                    continue;

                for (var i = 1; i <= p.ProjectItems.Count; i++)
                {
                    var subProject = p.ProjectItems.Item(i).SubProject;
                    if (subProject == null)
                    {
                        continue;
                    }

                    // If this is another solution folder, do a recursive call, otherwise add
                    if (subProject.Name == "Orchard.Forms")
                        orchardForms = subProject;

                    if (subProject.Name == "Orchard.Layouts")
                        orchardLayouts = subProject;

                    // this wont work if those modules are not in the solution
                    if (orchardLayouts != null && orchardForms != null)
                        break;
                }
                break;
            }

            vsproject.References.AddProject(orchardForms);
            vsproject.References.AddProject(orchardLayouts);

            // in case people are trying to add elements to Orchard.Layouts
            //if (!vsproject.Project.Name.Contains("Orchard.Layouts")) vsproject.References.Add("Orchard.Layouts.csproj");

            AddFolder(Context.ActiveProject, "Elements");
            AddFileFromTemplate(Context.ActiveProject,
                    "Elements\\" + name,
                    "Element",
                    parameters,
                    skipIfExists: true);

            

            // Add the driver folder and file
            AddFolder(Context.ActiveProject, "Drivers");
            if (_viewModel.HasEditor && formsEditor)
            {
                

                AddFileFromTemplate(Context.ActiveProject,
                    "Drivers\\" + namePlusElement + "Driver",
                    "FormsElementDriver",
                    parameters,
                    skipIfExists: true);
            }
            else
            {
                AddFileFromTemplate(Context.ActiveProject,
                    "Drivers\\" + name + "Driver",
                    "ElementDriver",
                    parameters,
                    skipIfExists: true);
            }

            AddFolder(Context.ActiveProject, @"Views\Elements");
            AddFileFromTemplate(Context.ActiveProject,
                "Views\\Elements\\" + name,
                "ElementView",
                parameters,
                skipIfExists: true);

            if(_viewModel.HasEditor && !formsEditor)
            {
                AddFolder(Context.ActiveProject, @"ViewModels");
                AddFileFromTemplate(Context.ActiveProject,
                    "ViewModels\\" + namePlusElement + "ViewModel",
                    "FormsElementDriver",
                    parameters,
                    skipIfExists: true);

                AddFolder(Context.ActiveProject, @"Views\EditorTemplates");
                AddFileFromTemplate(Context.ActiveProject,
                    "Views\\EditorTemplates\\Elements." + name,
                    "ElementEditorView",
                    parameters,
                    skipIfExists: true);
            }
        }

        //public DTE2 GetActiveIDE()
        //{
        //    // Get an instance of currently running Visual Studio IDE.
        //    DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
        //    return dte2;
        //}

        //public IList<Project> Projects()
        //{
        //    Projects projects = GetActiveIDE().Solution.Projects;
        //    List<Project> list = new List<Project>();
        //    var item = projects.GetEnumerator();
        //    while (item.MoveNext())
        //    {
        //        var project = item.Current as Project;
        //        if (project == null)
        //        {
        //            continue;
        //        }

        //        if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
        //        {
        //            list.AddRange(GetSolutionFolderProjects(project));
        //        }
        //        else
        //        {
        //            list.Add(project);
        //        }
        //    }

        //    return list;
        //}

        //private IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        //{
        //    List<Project> list = new List<Project>();
        //    for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
        //    {
        //        var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
        //        if (subProject == null)
        //        {
        //            continue;
        //        }

        //        // If this is another solution folder, do a recursive call, otherwise add
        //        if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
        //        {
        //            list.AddRange(GetSolutionFolderProjects(subProject));
        //        }
        //        else
        //        {
        //            list.Add(subProject);
        //        }
        //    }
        //    return list;
        //}
    }
}
