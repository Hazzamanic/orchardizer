using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;

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
                { "Feature", _viewModel.Feature },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  }
            };
        }        
    }
}
