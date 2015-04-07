using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Orchardization.UI;
using Microsoft.AspNet.Scaffolding;
using System.Collections.Generic;
using VSLangProj;
using EnvDTE;

namespace Orchardization
{
    public class CustomCodeGenerator : CodeGenerator
    {
        CustomViewModel _viewModel;

        /// <summary>
        /// Constructor for the custom code generator
        /// </summary>
        /// <param name="context">Context of the current code generation operation based on how scaffolder was invoked(such as selected project/folder) </param>
        /// <param name="information">Code generation information that is defined in the factory class.</param>
        public CustomCodeGenerator(
            CodeGenerationContext context,
            CodeGeneratorInformation information)
            : base(context, information)
        {
            _viewModel = new CustomViewModel(Context);
        }


        /// <summary>
        /// Any UI to be displayed after the scaffolder has been selected from the Add Scaffold dialog.
        /// Any validation on the input for values in the UI should be completed before returning from this method.
        /// </summary>
        /// <returns></returns>
        public override bool ShowUIAndValidate()
        {
            SelectModelWindow window = new SelectModelWindow(_viewModel);
            bool? showDialog = window.ShowDialog();

            Validate();

            return showDialog ?? false;
        }

        /// <summary>
        /// Validates the users inputs.
        /// </summary>
        public void Validate()
        {
            if(String.IsNullOrWhiteSpace(_viewModel.PartName))
                throw new InvalidOperationException("Must specify a part name");

            if (String.IsNullOrWhiteSpace(_viewModel.Storage))
                throw new InvalidOperationException("Must specify a storage type");

            if (_viewModel.CreateMigrations) 
            {
                if (_viewModel.SelectedMigration == null && String.IsNullOrWhiteSpace(_viewModel.Migration))
                    throw new InvalidOperationException("Must select a migration or specify a new migrations file");
            }

        }

        /// <summary>
        /// This method is executed after the ShowUIAndValidate method, and this is where the actual code generation should occur.
        /// In this example, we are generating a new file from t4 template based on the ModelType selected in our UI.
        /// </summary>
        public override void GenerateCode()
        {
            // Get the selected code type
            var partName = _viewModel.PartName.Trim();

            // if they don't select a storage, use infoset because fuck validating
            if (String.IsNullOrEmpty(_viewModel.Storage))
                _viewModel.Storage = "Infoset Storage";

            var props = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(_viewModel.Properties) || !String.IsNullOrWhiteSpace(_viewModel.Properties))
            {
                props = Regex.Split(_viewModel.Properties, "\\n")
                            .Select(x => x.Split(':'))
                            .Where(x => x.Length > 1 && !String.IsNullOrEmpty(x[0].Trim()) && !String.IsNullOrEmpty(x[1].Trim()))
                            .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
            }

            var recordStorage = _viewModel.Storage.Contains("Record Storage");
            var handlerTemplate = recordStorage ? "RecordPartHandler" : "InfoPartHandler";

            bool hassitegroup = !String.IsNullOrEmpty(_viewModel.SiteSection);

            // Setup the scaffolding item creation parameters to be passed into the T4 template.
            var parameters = new Dictionary<string, object>()
            {
                { "PartName", partName },
                { "Module", Context.ActiveProject.Name },
                { "Properties", props },
                { "SiteSetting", _viewModel.SiteSetting },
                { "SiteSection", _viewModel.SiteSection ?? "" },
                { "RecordStorage", recordStorage },
                { "ShowAdminSummary", _viewModel.ShowAdminSummary },
                { "HasSiteGroup", hassitegroup },
                { "Feature", _viewModel.Feature },
                { "HasFeature", !String.IsNullOrWhiteSpace(_viewModel.Feature)  }
            };

            // make sure references are there
            var vsproject = Context.ActiveProject.Object as VSLangProj.VSProject;
            vsproject.References.Add("Orchard.Core");
            vsproject.References.Add("Orchard.Framework");

            // Add the Models folder
            AddFolder(Context.ActiveProject, "Models");

            // Add the part
            if (_viewModel.Storage.Contains("Infoset Storage"))
            {
                AddFileFromTemplate(Context.ActiveProject,
                    "Models\\" + partName,
                    "InfoPart",
                    parameters,
                    skipIfExists: true);
            }
            else if (_viewModel.Storage.Contains("Infoset and Record Storage"))
            {
                AddFileFromTemplate(Context.ActiveProject,
                    "Models\\" + partName + "Record",
                    "PartRecord",
                    parameters,
                    skipIfExists: true);
                AddFileFromTemplate(Context.ActiveProject,
                    "Models\\" + partName,
                    "InfoAndRecordPart",
                    parameters,
                    skipIfExists: true);
            }
            else if (_viewModel.Storage.Contains("Record Storage"))
            {
                AddFileFromTemplate(Context.ActiveProject,
                    "Models\\" + partName + "Record",
                    "PartRecord",
                    parameters,
                    skipIfExists: true);
                AddFileFromTemplate(Context.ActiveProject,
                    "Models\\" + partName,
                    "RecordPart",
                    parameters,
                    skipIfExists: true);
            }


            // Add the driver folder and file
            AddFolder(Context.ActiveProject, "Drivers");
            AddFileFromTemplate(Context.ActiveProject,
                "Drivers\\" + partName + "Driver",
                "PartDriver",
                parameters,
                skipIfExists: true);

            // Add handler folder and file
            AddFolder(Context.ActiveProject, "Handlers");
            AddFileFromTemplate(Context.ActiveProject,
                "Handlers\\" + partName + "Handler",
                handlerTemplate,
                parameters,
                skipIfExists: true);

            // Add views
            if (!_viewModel.SiteSetting)
            {
                AddFolder(Context.ActiveProject, @"Views\Parts");
                AddFileFromTemplate(Context.ActiveProject,
                    "Views\\Parts\\" + partName,
                    "PartView",
                    parameters,
                    skipIfExists: true);
            }
            AddFolder(Context.ActiveProject, @"Views\EditorTemplates\Parts");
            AddFileFromTemplate(Context.ActiveProject,
                "Views\\EditorTemplates\\Parts\\" + partName,
                "PartEditorView",
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
                var placement = "<Place Parts_" + partName + @"=""Content:2"" />"
                    + Environment.NewLine
                    + "<Place Parts_" + partName + @"_Edit=""Content:5"" />"
                    + Environment.NewLine;
                var projectPath = Context.ActiveProject.GetFullPath();
                var placementPath = projectPath + "Placement.info";
                var placementText = File.ReadAllText(placementPath);
                placementText = placementText.Insert(placementText.LastIndexOf("</Placement>"), placement);
                File.WriteAllText(placementPath, placementText);
            }

            // build migrations
            if (_viewModel.CreateMigrations)
            {
                if (String.IsNullOrEmpty(_viewModel.Migration))
                {
                    if (_viewModel.SelectedMigration == null)
                    {
                        
                    }
                    EditMigrations(props);
                }
                else
                {
                    var migrationName = (_viewModel.Migration ?? partName + "Migrations").Trim();
                    _viewModel.HelpText = _viewModel.HelpText ?? string.Empty;
                    var migrationParams = new Dictionary<string, object>()
                    {
                        { "PartName", partName },
                        { "Module", Context.ActiveProject.Name },
                        { "Properties", props },
                        { "RecordStorage", recordStorage },
                        { "MigrationName", migrationName },
                        { "Attachable", _viewModel.Attachable },
                        { "Description", _viewModel.HelpText },
                        { "CreateWidget", _viewModel.CreateWidget },
                        { "WidgetName", (_viewModel.WidgetName ?? partName + "Widget").Trim() },
                    };

                    AddFileFromTemplate(
                        Context.ActiveProject,
                        migrationName,
                        "Migrations",
                        migrationParams,
                        skipIfExists: true);
                }
            }

        }

        private void EditMigrations(Dictionary<string, string> properties)
        {
            // Edit migrations
            var migration = _viewModel.SelectedMigration.CodeType;
            var cc = migration as CodeClass;
            // get functions
            var members = cc.Members;
            // list of ints
            List<int> migrations = new List<int>();
            // iterate through functions
            foreach (CodeElement member in members)
            {
                var func = member as CodeFunction;
                if (func == null)
                    continue;
                // TODO: investigate use of CodeFunction
                var createIndex = member.Name == "Create";
                if (createIndex)
                {
                    migrations.Add(0);
                    continue;
                }

                var index = member.Name.IndexOf("UpdateFrom");
                if (index == -1)
                    continue;
                
                migrations.Add(Int32.Parse(member.Name.Last().ToString()));
            }
            // sort numbers, just in case
            migrations.Sort();
            // get new update number
            var update = migrations.Count == 0 ? 0 : migrations.Last() + 1;
            // create method, either update or create
            var methodName = update == 0 ? "Create" : "UpdateFrom" + update;
            CodeFunction cf = cc.AddFunction(methodName, vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefInt, -1, vsCMAccess.vsCMAccessPublic);
            // access new method
            TextPoint tp = cf.GetStartPoint(vsCMPart.vsCMPartBody);
            TextPoint end = cf.GetEndPoint(vsCMPart.vsCMPartBody);
            EditPoint ep = tp.CreateEditPoint();
            // delete auto generated code
            ep.Delete(end);

            var partName = _viewModel.PartName.Trim();

            // add part bits and bobs
            if (_viewModel.Attachable)
            {
                ep.Insert(String.Format(@"ContentDefinitionManager.AlterPartDefinition(""{0}"", builder => builder", partName) + Environment.NewLine);
                // make attachable
                ep.Insert(".Attachable()");
                // add description
                if (!String.IsNullOrEmpty(_viewModel.HelpText))
                    ep.Insert(Environment.NewLine + @".WithDescription(""" + _viewModel.HelpText + @""")");
                ep.Insert(");" + Environment.NewLine + Environment.NewLine);
            }

            // add record migration
            if (_viewModel.Storage.Contains("Record Storage"))
            {
                ep.Insert(String.Format(@"SchemaBuilder.CreateTable(""{0}Record"", table => table", partName) +
                    Environment.NewLine +
                    ".ContentPartRecord()");
                foreach (var prop in properties)
                {
                    ep.Insert(Environment.NewLine + ".Column<" + prop.Value + @">(""" + prop.Key + @""")");
                }
                ep.Insert(");" + Environment.NewLine + Environment.NewLine);
            }

            // create widget
            if (_viewModel.CreateWidget)
            {
                ep.Insert(String.Format(@"ContentDefinitionManager.AlterTypeDefinition(""{0}"", widget => widget", (_viewModel.WidgetName ?? partName + "Widget").Trim()) +
                    Environment.NewLine +
                    @".WithPart(""CommonPart"")" +
                    Environment.NewLine +
                    @".WithPart(""WidgetPart"")" +
                    Environment.NewLine +
                    @".WithPart(""" + partName + @""")" +
                    Environment.NewLine +
                    @".WithSetting(""Stereotype"", ""Widget"")"
                );
                ep.Insert(");" + Environment.NewLine + Environment.NewLine + Environment.NewLine);
            }

            var returnVal = update + 1;
            ep.Insert("return" + returnVal);

            // format document
            tp.CreateEditPoint().SmartFormat(ep);
            
        }

        public string GenerateDbType(string property)
        {
            property = property.ToLowerInvariant();
            var dbtype = "DbType.";
            switch (property)
            {
                case "int":
                    return dbtype + "Int32";
                case "string":
                    return dbtype + "String";
                case "Guid":
                    return dbtype + "Guid";
                case "bool":
                    return dbtype + "Boolean";
                case "date":
                    return dbtype + "Date";
                case "time":
                    return dbtype + "Time";
                case "decimal":
                    return dbtype + "Decimal";
                case "double":
                    return dbtype + "Double";
                default:
                    return dbtype + "Unknown";

            }
        }
    }
}
