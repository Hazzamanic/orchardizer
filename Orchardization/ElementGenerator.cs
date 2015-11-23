using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;

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

            int s = 12;

            // Setup the scaffolding item creation parameters to be passed into the T4 template. We'll just use one for everything
            var parameters = new Dictionary<string, object>()
            {
                { "ElementName", name },
                { "Module", Context.ActiveProject.Name },
                { "Properties", props },
                { "PropCount", props.Count() },
                { "HasProperty", hasProperties },
                { "FirstKey", firstKey },
                { "FirstType", firstType },
                { "Category", String.IsNullOrWhiteSpace(_viewModel.Category) ? "Content" : _viewModel.Category },
                { "Description", String.IsNullOrWhiteSpace(_viewModel.Description) ? $"Add a {name} to the layout" : _viewModel.Description },
                { "Feature", _viewModel.Feature },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  },
                { "HasEditor", _viewModel.HasEditor  }
            };
        }


    }
}
