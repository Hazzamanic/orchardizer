using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace Orchardization
{
    public class FieldGenerator : CodeGenerator
    {
        FieldViewModel _viewModel;

        /// <summary>
        /// Constructor for the custom code generator
        /// </summary>
        /// <param name="context">Context of the current code generation operation based on how scaffolder was invoked(such as selected project/folder) </param>
        /// <param name="information">Code generation information that is defined in the factory class.</param>
        public FieldGenerator(
            CodeGenerationContext context,
            CodeGeneratorInformation information)
            : base(context, information)
        {
            _viewModel = new FieldViewModel();
        }


        /// <summary>
        /// Any UI to be displayed after the scaffolder has been selected from the Add Scaffold dialog.
        /// Any validation on the input for values in the UI should be completed before returning from this method.
        /// </summary>
        /// <returns></returns>
        public override bool ShowUIAndValidate()
        {
            // Bring up the selection dialog and allow user to select a model type
            FieldWindow window = new FieldWindow(_viewModel);
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
            var fieldName = _viewModel.FieldName.Trim();

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
            if(hasProperties)
            {
                firstKey = firstProperty.Key;
                firstType = firstProperty.Value;
            }


            // Setup the scaffolding item creation parameters to be passed into the T4 template. We'll just use one for everything
            var parameters = new Dictionary<string, object>()
            {
                { "FieldName", fieldName },
                { "Module", Context.ActiveProject.Name },
                { "Properties", props },
                { "HasProperty", hasProperties },
                { "FirstKey", firstKey },
                { "FirstType", firstType },
                { "IndexField", _viewModel.IndexField },
                { "Feature", _viewModel.Feature ?? "" },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  }
            };

            // make sure references are there
            var vsproject = Context.ActiveProject.Object as VSLangProj.VSProject;
            vsproject.References.Add("Orchard.Core");
            vsproject.References.Add("Orchard.Framework");

            AddFolder(Context.ActiveProject, "Fields");
            AddFileFromTemplate(Context.ActiveProject,
                    "Fields\\" + fieldName,
                    "Field",
                    parameters,
                    skipIfExists: true);

            // Add the driver folder and file
            AddFolder(Context.ActiveProject, "Drivers");
            AddFileFromTemplate(Context.ActiveProject,
                "Drivers\\" + fieldName + "Driver",
                "FieldDriver",
                parameters,
                skipIfExists: true);

            AddFolder(Context.ActiveProject, @"Views\Fields");
            AddFileFromTemplate(Context.ActiveProject,
                "Views\\Fields\\" + fieldName,
                "FieldView",
                parameters,
                skipIfExists: true);

            AddFolder(Context.ActiveProject, @"Views\EditorTemplates\Fields");
            AddFileFromTemplate(Context.ActiveProject,
                "Views\\EditorTemplates\\Fields\\" + fieldName,
                "FieldEditorView",
                parameters,
                skipIfExists: true);

            // Add placement file if it doesn't exist
            var editPlacement = AddFileFromTemplate(
                Context.ActiveProject,
                "Placement",
                "Placement",
                parameters,
                skipIfExists: true);

            // Edit placement file if it already exists
            if (!editPlacement)
            {
                var placement = "<Place Fields_" + fieldName + @"=""Content:5"" />"
                    + Environment.NewLine
                    + "<Place Fields_" + fieldName + @"_Edit=""Content:5"" />"
                    + Environment.NewLine;
                var projectPath = Context.ActiveProject.GetFullPath();
                var placementPath = projectPath + "Placement.info";
                var placementText = File.ReadAllText(placementPath);
                placementText = placementText.Insert(placementText.LastIndexOf("</Placement>"), placement);
                File.WriteAllText(placementPath, placementText);
            }
        }        
    }
}
