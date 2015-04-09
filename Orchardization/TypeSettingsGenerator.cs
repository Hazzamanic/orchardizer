using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Orchardization
{
    public class TypeSettingsGenerator : CodeGenerator
    {
        TypeSettingsViewModel _viewModel;

        /// <summary>
        /// Constructor for the custom code generator
        /// </summary>
        /// <param name="context">Context of the current code generation operation based on how scaffolder was invoked(such as selected project/folder) </param>
        /// <param name="information">Code generation information that is defined in the factory class.</param>
        public TypeSettingsGenerator(
            CodeGenerationContext context,
            CodeGeneratorInformation information)
            : base(context, information)
        {
            _viewModel = new TypeSettingsViewModel(Context);
        }


        /// <summary>
        /// Any UI to be displayed after the scaffolder has been selected from the Add Scaffold dialog.
        /// Any validation on the input for values in the UI should be completed before returning from this method.
        /// </summary>
        /// <returns></returns>
        public override bool ShowUIAndValidate()
        {
            // Bring up the selection dialog and allow user to select a model type
            TypeSettingsWindow window = new TypeSettingsWindow(_viewModel);
            bool? showDialog = window.ShowDialog();

            Validate();

            return showDialog ?? false;
        }

        /// <summary>
        /// Validates the users inputs.
        /// </summary>
        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(_viewModel.PartName))
                throw new InvalidOperationException("Must select a part");
        }

        /// <summary>
        /// This method is executed after the ShowUIAndValidate method, and this is where the actual code generation should occur.
        /// In this example, we are generating a new file from t4 template based on the ModelType selected in our UI.
        /// </summary>
        public override void GenerateCode()
        {
            // Get the selected code type
            var part = _viewModel.PartName;
            var props = Regex.Split(_viewModel.Properties, "\\n")
                                .Select(x => x.Split(':'))
                                .Where(x => x.Length > 1 && !String.IsNullOrEmpty(x[0].Trim()) && !String.IsNullOrEmpty(x[1].Trim()))
                                .ToDictionary(x => x[0].Trim(), x => x[1].Trim());

            // Setup the scaffolding item creation parameters to be passed into the T4 template.
            var parameters = new Dictionary<string, object>()
            {
                { "Module", Context.ActiveProject.Name },
                { "PartName", part },
                { "Settings", props },
                { "Feature", _viewModel.Feature },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  }
            };

            // Add settings folder and files
            AddFolder(Context.ActiveProject, "Settings");
            AddFileFromTemplate(Context.ActiveProject,
                "Settings\\" + part + "Settings",
                "TypeSettings",
                parameters,
                skipIfExists: true);
            AddFileFromTemplate(Context.ActiveProject,
                "Settings\\" + part + "EditorEvents",
                "TypeSettingsEditorEvents",
                parameters,
                skipIfExists: true);

            // Add view folder structure
            AddFolder(Context.ActiveProject, @"Views\DefinitionTemplates");
            // Add view
            AddFileFromTemplate(Context.ActiveProject,
                "Views\\DefinitionTemplates\\" + part + "Settings",
                "TypeSettingsEditor",
                parameters,
                skipIfExists: true);
        }


    }
}
