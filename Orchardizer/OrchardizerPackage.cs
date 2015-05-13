using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using EnvDTE80;
using Microsoft.AspNet.Scaffolding;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Orchardizer.UI;
using VSLangProj;
using Process = System.Diagnostics.Process;

namespace Orchardizer
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidThemeCreatorPkgString)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    public sealed class OrchardizerPackage : Package
    {
        private EnvDTE.DTE dte;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public OrchardizerPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            dte = this.GetService(typeof(SDTE)) as EnvDTE.DTE;

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID themeCommandID = new CommandID(GuidList.guidThemeCreatorCmdSet,
                    (int)PkgCmdIDList.cmdidCreateTheme);
                var themeMenuItem = new OleMenuCommand(ThemeCallback, themeCommandID);
                // callback added to enable only when right clicking Themes folder
                themeMenuItem.BeforeQueryStatus += themeBeforeQuery;
                mcs.AddCommand(themeMenuItem);

                CommandID moduleCommandId = new CommandID(GuidList.guidThemeCreatorCmdSet,
                    (int)PkgCmdIDList.cmdidCreateModule);
                var moduleMenuItem = new OleMenuCommand(ModuleCallback, moduleCommandId);
                // callback added to enable only when right clicking Modules folder
                moduleMenuItem.BeforeQueryStatus += moduleBeforeQuery;
                mcs.AddCommand(moduleMenuItem);

                // add the Orchard.exe menu item
                CommandID exeCommandId = new CommandID(GuidList.guidThemeCreatorCmdSet,
                    (int)PkgCmdIDList.cmdidRunExe);
                var exeMenuItem = new OleMenuCommand(ExeCallback, exeCommandId);
                mcs.AddCommand(exeMenuItem);

                // add the Build Precompiled command
                CommandID buildCommandId = new CommandID(GuidList.guidThemeCreatorCmdSet,
                    (int)PkgCmdIDList.cmdidRunBuild);
                var buildMenuItem = new OleMenuCommand(BuildCallback, buildCommandId);
                mcs.AddCommand(buildMenuItem);

                // add the Generate Migrations command
                CommandID migrationsCommandId = new CommandID(GuidList.guidThemeCreatorCmdSet,
                    (int)PkgCmdIDList.cmdidMigrations);
                var migrationsMenuItem = new OleMenuCommand(MigrationsCallback, migrationsCommandId);
                mcs.AddCommand(migrationsMenuItem);
            }
        }

        #endregion


        /// <summary>
        /// Fired when a user selects the create a new theme menu option
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ThemeCallback(object sender, EventArgs e)
        {
            var ssol = dte.Solution;
            var projects = ssol.Projects;
            //if we need to use codegen or can use our extended theme generator
            bool extended = CheckFramework(projects);

            var vm = new ThemeViewModel();
            vm.Codegen = !extended;
            var window = new ThemeWindow(vm);
            var success = window.ShowDialog();

            if (String.IsNullOrWhiteSpace(vm.ThemeName))
            {
                FireError("Please specify a name for your theme!");
                return;
            }

            vm.ThemeName = vm.ThemeName.Replace(" ", String.Empty);

            if (success.GetValueOrDefault() == false)
                return;

            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if (!extended)
            //if(true)
            {
                var path = GetOrchardExe(solution);
                if (!File.Exists(path))
                {
                    FireError("Cannot find Orchard.exe, try building the solution and then creating your theme again");
                    return;
                }

                var basedon = String.IsNullOrEmpty(vm.BasedOn) ? String.Empty : "/BasedOn:" + vm.BasedOn.Trim();
                var cproj = vm.CreateProject ? "/CreateProject:true" : String.Empty;

                var args = String.Format("codegen theme {0} {1} {2} /IncludeInSolution:true",
                    Regex.Replace(vm.ThemeName, @"\s+", ""), cproj, basedon);

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "feature enable Orchard.CodeGeneration"
                };
                Process.Start(start).WaitForExit();

                ProcessStartInfo start2 = new ProcessStartInfo { FileName = path, Arguments = args };
                Process.Start(start2);
                return;
            }

            if (vm.Type == null) vm.Type = "Blank";

            // get the Themes folder in the solution
            Project themesFolderProject = (from Project p in projects where p.Name == "Themes" select p).FirstOrDefault();
            if (themesFolderProject == null)
                FireError("There appears to be no Themes folder");
            SolutionFolder themesFolder = themesFolderProject.Object as SolutionFolder;

            // Orchard templates
            var templates = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "OrchardTemplates");
            Project theme = (from ProjectItem item in themesFolderProject.ProjectItems where item.Name == "Themes" select item.Object as Project).FirstOrDefault();

            if (vm.Type.Contains("Bootstrap"))
            {
                FireError("I haven't created the bootstrap theme... but I will! Maybe. Probably not. I'm lazy");
            }

            if (vm.Type.Contains("Blank"))
            {
                if (vm.CreateProject)
                {
                    BuildThemeWithProject(themesFolder, vm, templates, "__BlankThemeProject");
                    return;
                }

                BuildThemeFromTemplate(theme, vm, templates, "__BlankTheme");
                return;
            }

            if (vm.Type.Contains("Theme Machine"))
            {
                if (vm.CreateProject)
                {
                    var themeType = vm.Responsive ? "__TMRP" : "__TMP";
                    BuildThemeWithProject(themesFolder, vm, templates, themeType);
                    return;
                }
                else
                {
                    var themeType = vm.Responsive ? "__TMR" : "__TM";
                    BuildThemeFromTemplate(theme, vm, templates, themeType);
                    return;
                }
            }

            //// get the themes project
            //if (theme == null)
            //    FireError("Could not find themes folder!");

            //var projItems = theme.ProjectItems;
            //var themePath = theme.FileName.Replace("Themes.csproj", vm.ThemeName);

            //var newproj = projItems.AddFromDirectory(templates + "\\BlankTheme");
            //newproj.Name = vm.ThemeName;
            //Insert(themePath + "\\Theme.txt", new[]
            //{
            //    new KeyValuePair<string, string>("$ThemeName$", vm.ThemeName),
            //    new KeyValuePair<string, string>("$Author$", vm.Author ?? "Hazzamanic"),
            //    new KeyValuePair<string, string>("$Description$", vm.Description ?? "Theme created by Orchardizer"),
            //    new KeyValuePair<string, string>("$BasedOn$", vm.BasedOn ?? "")
            //});

            //if (vm.IncludeHelpFile)
            //    newproj.ProjectItems.AddFromFile(templates + "\\ThemeHelp.md");
        }


        /// <summary>
        /// Fired when the user selects to create a new module
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ModuleCallback(object sender, EventArgs e)
        {
            //var ssol = dte.Solution;
            //var solProjects = ssol.Projects;
            ////if we need to use codegen or can use our extended theme generator
            //bool extended = CheckFramework(solProjects);

            var vm = new ModuleViewModel();
            var window = new ModuleWindow(vm);
            var success = window.ShowDialog();
            if (success.GetValueOrDefault() == false)
                return;

            if (String.IsNullOrEmpty(vm.Name))
            {
                FireError("You need to give your module a name!");
                return;
            }

            var name = Regex.Replace(vm.Name, @"\s+", "");

            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var templates = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "OrchardTemplates");

            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            var envsol = dte.Solution;
            var path = envsol.FullName;

            string solpath;
            string solfile;
            string soloptions;
            solution.GetSolutionInfo(out solpath, out solfile, out soloptions);

            var projects = dte.Solution.Projects;
            // get the modules folder project
            Project modulesFolderProject = (from Project p in projects where p.Name == "Modules" select p).FirstOrDefault();
            // pointless error handling
            if (modulesFolderProject == null)
            {
                FireError("There appears to be no Modules folder");
                return;
            }

            // cast to solutionfolder object
            SolutionFolder modulesFolder = modulesFolderProject.Object as SolutionFolder;
            if (modulesFolder == null)
            {
                FireError("There appears to be no Modules folder");
                return;
            }

            var newDir = Path.Combine(solpath, "Orchard.Web", "Modules", name);

            var newproj = modulesFolder.AddFromTemplate(Path.Combine(templates, "__BlankModule", "BlankModule.csproj"), newDir, name);
            // it does not edit the assembly name so we will do that ourselves
            EditCsProj(Path.Combine(newDir, name + ".csproj"), name);

            newproj.Properties.Item("AssemblyName").Value = name;

            // edit module.txt
            Insert(newDir + "\\Module.txt", new[]
            {
                new KeyValuePair<string, string>("$name$", name),
            });

            // save our shiny new module
            newproj.Save();
        }


        /// <summary>
        /// Opens up the Orchard.exe command line tool
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ExeCallback(object sender, EventArgs e)
        {
            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            var path = GetOrchardExe(solution);
            // check it exists
            // if not, todo: build solution and run again
            if (!File.Exists(path))
            {
                FireError("Cannot find Orchard.exe, try building the solution and trying again");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "\"" + path + "\""
            };

            Process.Start(startInfo).Exited += new EventHandler(process_Exited);
        }

        /// <summary>
        /// Runs build precompiled command
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void BuildCallback(object sender, EventArgs e)
        {
            var vs = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../", @"../", "VC", "vcvarsall.bat"));
            if (!File.Exists(vs))
            {
                // cancel execution
            }
            //FireError(vs);


            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            string solDir, solFile, userOpts;
            solution.GetSolutionInfo(out solDir, out solFile, out userOpts);




            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WorkingDirectory = solDir,
                WindowStyle = ProcessWindowStyle.Normal,
                //CreateNoWindow = true,
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                //RedirectStandardOutput = true,
                //RedirectStandardOutput = true,
                UseShellExecute = false,
                Arguments = String.Format(@"%comspec% /k ""{0}"" x86", vs)
            };

            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(process_Exited);

            process.Start();
            process.StandardInput.WriteLine("cd..");
            process.StandardInput.WriteLine("build precompiled /k");
            //process.WaitForExit();
        }

        private void process_Exited(object sender, EventArgs e)
        {
            var p = (Process)sender;
            int exitCode = p.ExitCode;
        }

        /// <summary>
        /// Generates the content type migration from Orchard export code.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GenerateContentTypeMigration(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Called when the Generate Migrations command is run
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void MigrationsCallback(object sender, EventArgs e)
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            IVsMonitorSelection monitorSelection = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

            IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
            if (hierarchy == null)
            {
                FireError("Fuck knows why but it is broken, sorry");
                return;
            }
            object projObj;
            hierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out projObj);

            ProjectItem projItem = projObj as ProjectItem;

            if (projItem == null)
            {
                FireError("The item you have selected is not playing nicely. Apologies");
                return;
            }

            var project = projItem.ContainingProject;
            var migrations = new List<Migration>();


            var allClasses = GetProjectItems(project.ProjectItems).Where(v => v.Name.Contains(".cs"));
            // check for .cs extension on each,

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

                            // check all classes they derive from to see if any of them are migration classes, add them if so
                            migrations.AddRange(from object b in member.Bases select b as CodeClass into bClass where bClass != null && bClass.Name == "DataMigrationImpl" select new Migration(member));
                        }
                    }
                }
            }

            var model = projItem.FileCodeModel;
            if (model == null)
            {
                FireError("This class you have selected is weird and broken. Choose another one");
                return;
            }
            var elements = model.CodeElements;

            var classes = new List<Model>();

            // run through elements (they are done in a hierachy, so first we have using statements and the namespace) to find namespace
            foreach (var ele in elements)
            {
                if (ele is EnvDTE.CodeNamespace)
                {
                    var ns = ele as EnvDTE.CodeNamespace;
                    // run through classes 
                    foreach (var c in ns.Members)
                    {
                        var member = c as CodeType;
                        if (member == null)
                            continue;

                        classes.Add(new Model() { Class = member, Name = member.Name });
                    }
                }
            }

            if (!classes.Any())
            {
                FireError("No classes in the selected file!");
                return;
            }


            var vm = new MigrationsViewModel(migrations, classes);
            var window = new MigrationsWindow(vm);
            var success = window.ShowDialog();

            if (!success.GetValueOrDefault())
            {
                return;
            }

            CodeType selectedClass = vm.SelectedClass.Class;

            if (selectedClass == null)
            {
                FireError("No class to generate migrations from!");
                return;
            }

            // name of class
            var modelName = selectedClass.Name;
            // get code class
            var cc = selectedClass as CodeClass;
            // get all members of the class
            var members = cc.Members;

            bool contentPartRecord = false;
            foreach (var d in cc.Bases)
            {
                var dClass = d as CodeClass;
                if (dClass != null && dClass.Name == "ContentPartRecord")
                {
                    contentPartRecord = true;
                }

            }

            var props = new List<MigrationItem>();

            //iterate through to find properties
            foreach (var member in members)
            {
                var prop = member as CodeProperty;
                if (prop == null)
                    continue;

                if (prop.Access != vsCMAccess.vsCMAccessPublic)
                    continue;

                var type = prop.Type;
                var name = prop.Name;
                var fullName = type.AsFullName;
                var nullable = fullName.Contains(".Nullable<");
                var sType = type.AsString.Replace("?", "");

                var sName = name;
                // if model, add _Id for nhibernate
                if (fullName.Contains(".Models.") || fullName.Contains(".Records."))
                {
                    sName += "_Id";
                    sType = "int";
                }

                var mi = new MigrationItem()
                {
                    Name = name,
                    SuggestedName = sName,
                    Type = fullName,
                    SuggestedType = sType,
                    Nullable = nullable,
                    Create = true,
                };

                props.Add(mi);
            }

            var createMigrationFile = !String.IsNullOrEmpty(vm.NewMigration);

            if (!createMigrationFile)
            {
                if (vm.SelectedMigration == null)
                {
                    FireError("Select a migration or choose a new one!");
                    return;
                }
                var mig = vm.SelectedMigration.CodeType;
                string path = (string)mig.ProjectItem.Properties.Item("FullPath").Value;
                string text = File.ReadAllText(path);

                var noTimesCreated = Regex.Matches(text, @"SchemaBuilder.Create\(""" + modelName + @""",").Count;
                var noTimesDropped = Regex.Matches(text, @"SchemaBuilder.DropTable\(""" + modelName + @"""\)").Count;
                bool created = noTimesCreated > noTimesDropped;

                if (noTimesCreated == 1 && noTimesDropped == 0)
                {
                    foreach (var p in props.Where(p => text.Contains(p.Name)))
                    {
                        p.Create = false;
                    }
                }

                if (created)
                {
                    foreach (var p in props)
                    {
                        var name = p.Name;
                        var noDrops = Regex.Matches(text, @".DropColumn\(""" + name).Count;
                        var noOccurences = Regex.Matches(text, name).Count;

                        if ((noOccurences - noDrops) > noDrops)
                            p.Create = false;
                    }
                }
            }

            // if a collection ignore
            foreach (var p in props) if (p.Type.Contains("System.Collection")) p.Create = false;

            var bmvm = new BuildMigrationsViewModel(props);
            var bmWindow = new BuildMigrationsWindow(bmvm);
            var bmSuccess = bmWindow.ShowDialog();

            if (!bmSuccess.GetValueOrDefault())
            {
                return;
            }

            var sb = new StringBuilder();
            var createTable = "SchemaBuilder.CreateTable(\"" + modelName + "\", t => t";
            sb.AppendLine(createTable);
            if (contentPartRecord) sb.AppendLine("\t.ContentPartRecord()");
            foreach (var p in bmvm.Migrations.Where(z => z.Create))
            {
                sb.AppendLine("\t" + ParseMigrationItem(p));
            }

            sb.AppendLine(");");

            var editViewModel = new EditMigrationsViewModel()
            {
                Migrations = sb.ToString()
            };

            var editWindow = new EditMigrationsWindow(editViewModel);
            var emSuccess = editWindow.ShowDialog();

            if (!emSuccess.GetValueOrDefault())
            {
                return;
            }

            var migrationsCode = new StringBuilder();
            using (StringReader reader = new StringReader(editViewModel.Migrations))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    migrationsCode.AppendLine(line.Insert(0, "\t\t\t"));
                }
            }

            if (!createMigrationFile)
                EditMigrations(vm.SelectedMigration.CodeType, migrationsCode.ToString());
            else
            {
                var templates = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "OrchardTemplates");
                var newMigration = project.ProjectItems.AddFromFileCopy(templates + "\\Migrationsxxx.cs");
                Insert(newMigration.Properties.Item("FullPath").Value.ToString(), new[]
                {
                    new KeyValuePair<string, string>("$module$", project.Name),
                    new KeyValuePair<string, string>("$migrationName$", vm.NewMigration),
                    new KeyValuePair<string, string>("$code$", migrationsCode.ToString())
                });

                var cs = vm.NewMigration.EndsWith(".cs") ? "" : ".cs";
                newMigration.Name = vm.NewMigration + cs;
            }
        }

        private string ParseMigrationItem(MigrationItem item)
        {
            bool key = item.Name == "Id";

            // column switches
            var switches = "";

            if (key)
                switches += ".PrimaryKey().Identity()";

            if (item.SuggestedType == "string" && item.Length != 0)
                switches += ".WithLength(" + item.Length + ")";

            if (item.Nullable)
                switches += ".Nullable()";

            if (item.NotNull && !item.Nullable)
                switches += ".NotNull()";

            if (!String.IsNullOrEmpty(item.WithDefault))
            {
                if (item.SuggestedType == "string")
                    switches += ".WithDefault(\"" + item.WithDefault + "\")";
                else
                    switches += ".WithDefault(" + item.WithDefault + ")";
            }

            var line = String.Format(".Column<{0}>(\"{1}\"{2}{3})", item.SuggestedType, item.SuggestedName, String.IsNullOrEmpty(switches) ? "" : ", c => c", switches);

            return line;
        }

        private void EditMigrations(CodeType migration, string updateMethod)
        {
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

            var returnVal = update + 1;
            ep.Insert(updateMethod + Environment.NewLine + Environment.NewLine + "return " + returnVal + ";");

            tp.CreateEditPoint().SmartFormat(ep);
        }

        private string GenerateDbType(string property)
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

        private void BuildBootstrap()
        {

        }

        /// <summary>
        /// Builds a blank theme, no project file
        /// </summary>
        /// <param name="theme">The theme.</param>
        /// <param name="vm">The vm.</param>
        /// <param name="templates">The templates.</param>
        /// <param name="themeType">Type of theme.</param>
        private void BuildThemeFromTemplate(Project theme, ThemeViewModel vm, string templates, string themeType)
        {
            var projItems = theme.ProjectItems;
            var themePath = theme.FileName.Replace("Themes.csproj", vm.ThemeName);

            var newproj = projItems.AddFromDirectory(templates + "\\" + themeType);
            newproj.Name = vm.ThemeName;
            EditThemeFile(themePath, vm);

            if (vm.IncludeHelpFile)
                newproj.ProjectItems.AddFromFile(templates + "\\ThemeHelp.md");

            newproj.Save();
        }

        /// <summary>
        /// Builds a blank theme including a project file.
        /// </summary>
        /// <param name="themesFolder">The themes folder.</param>
        /// <param name="vm">The vm.</param>
        /// <param name="templates">The templates.</param>
        /// <param name="themeType">Type of theme.</param>
        private void BuildThemeWithProject(SolutionFolder themesFolder, ThemeViewModel vm, string templates, string themeType)
        {
            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            var envsol = dte.Solution;

            string solpath;
            string solfile;
            string soloptions;
            solution.GetSolutionInfo(out solpath, out solfile, out soloptions);

            //TODO: fix this...
            var newDir = Path.Combine(solpath, "Orchard.Web", "Themes", vm.ThemeName);
            var newproj = themesFolder.AddFromTemplate(Path.Combine(templates, themeType, themeType + ".csproj"), newDir, vm.ThemeName);

            // this doesn't work
            EditCsProj(Path.Combine(newDir, vm.ThemeName + ".csproj"), vm.ThemeName);
            // this does work...
            newproj.Properties.Item("AssemblyName").Value = vm.ThemeName;
            // need this as well because... I have no idea
            newproj.Properties.Item("RootNamespace").Value = vm.ThemeName;
            EditThemeFile(newDir, vm);

            if (vm.IncludeHelpFile)
                newproj.ProjectItems.AddFromFile(templates + "\\ThemeHelp.md");

            newproj.Save();

            // for some reason this doesn't appear to work, references are manually added in the csproj file :(
            //var vsproj = newproj.Object as VSProject;
            //vsproj.References.Add("Orchard.Core");
            //vsproj.References.Add("Orchard.Framework");
        }

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


        /// <summary>
        /// Checks the framework of the version of Orchard, checks it is v4.5
        /// </summary>
        /// <param name="projects">The projects.</param>
        /// <returns>true if v4.5</returns>
        private bool CheckFramework(Projects projects)
        {
            foreach (var p in projects)
            {
                var proj = p as Project;
                if (proj.FileName == null || proj.FileName == String.Empty)
                    continue;
                var fw = proj.Properties.Item("TargetFrameworkMoniker").Value;
                // maybe create an array of supported frameworks? but meh, might need new models. fuck that
                if (fw.ToString().Contains("v4.5"))
                    return true;
                break;
            }

            return false;
        }


        /// <summary>
        /// Inserts values into a given template
        /// </summary>
        /// <param name="templateFile">The template file.</param>
        /// <param name="replacements">Key Value pairs of values you want replaced e.g. "$$guid$$", "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4"</param>
        public void Insert(string templateFile, IEnumerable<KeyValuePair<string, string>> replacements)
        {
            //TODO: use using statements here
            // Create the file stream for reading the template file 
            FileStream fs = new FileStream(templateFile, FileMode.Open, FileAccess.Read);
            // Use a stream reader to read the file into a string 
            StreamReader sr = new StreamReader(fs);
            string strFile = sr.ReadToEnd();
            sr.Close();
            fs.Close();
            // Replace the template fields in the file with the appropriate values 
            foreach (var replacer in replacements)
            {
                strFile = strFile.Replace(replacer.Key, replacer.Value);
            }

            FileStream fs2 = new FileStream(templateFile, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs2);
            sw.Write(strFile);
            sw.Close();
            fs2.Close();
        }


        /// <summary>
        /// Called before theme menu item is shown so we can hide it if it is not the Themes folder
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void themeBeforeQuery(object sender, EventArgs args)
        {
            var item = sender as OleMenuCommand;
            if (item == null)
                return;

            item.Visible = false;
            item.Enabled = false;


            if (GetFolderName() == "Themes")
            {
                item.Visible = true;
                item.Enabled = true;
            }
        }

        /// <summary>
        /// Called before module menu item is shown so we can hide it if it is not the Modules folder
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void moduleBeforeQuery(object sender, EventArgs args)
        {
            var item = sender as OleMenuCommand;
            if (item == null)
                return;

            item.Visible = false;
            item.Enabled = false;


            if (GetFolderName() == "Modules")
            {
                item.Visible = true;
                item.Enabled = true;
            }
        }

        /// <summary>
        /// Called before the Orchard.exe and build menu items are shown
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void orchardBeforeQuery(object sender, EventArgs args)
        {
            var item = sender as OleMenuCommand;
            if (item == null)
                return;

            item.Visible = false;
            item.Enabled = false;

            // check solution is called Orchard... doesn't work, seem to be using GetFolderName method...
            if (GetFolderName() == "Orchard")
            {
                item.Visible = true;
                item.Enabled = true;
            }
        }

        /// <summary>
        /// Gets the name of the folder the user clicked on.
        /// </summary>
        /// <returns></returns>
        private string GetFolderName()
        {
            IntPtr hierarchyPtr, selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            IVsMonitorSelection monitorSelection = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

            IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
            if (hierarchy != null)
            {
                object value;
                hierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_Name, out value);

                return value.ToString();
            }
            return "";
        }

        /// <summary>
        /// Gets the orchard executable path.
        /// </summary>
        /// <returns></returns>
        private string GetOrchardExe(IVsSolution solution)
        {
            string solDir, solFile, userOpts;
            solution.GetSolutionInfo(out solDir, out solFile, out userOpts);

            var exe = Path.Combine(solDir, "Orchard.Web", "bin", "Orchard.exe");
            return exe;
        }

        /// <summary>
        /// Edits the theme file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="vm">The vm.</param>
        private void EditThemeFile(string path, ThemeViewModel vm)
        {
            Insert(path + "\\Theme.txt", new[]
            {
                new KeyValuePair<string, string>("$ThemeName$", vm.ThemeName),
                new KeyValuePair<string, string>("$Author$", vm.Author ?? "Orchardizer"),
                new KeyValuePair<string, string>("$Description$", vm.Description ?? "Theme created by Orchardizer"),
                new KeyValuePair<string, string>("$BasedOn$", String.IsNullOrWhiteSpace(vm.BasedOn) ? "" : "BaseTheme: " + vm.BasedOn)
            });
        }

        /// <summary>
        /// Edits the cs proj... doesn't work
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="name">The name.</param>
        private void EditCsProj(string path, string name)
        {
            Insert(path,
                new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("$guid$", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>("$name$", name)
                });
        }

        /// <summary>
        /// Fires the error message.
        /// </summary>
        /// <param name="message">The message you want displayed.</param>
        private void FireError(string message)
        {
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;

            ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                0,
                ref clsid,
                "Orchardizer",
                message,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0, // false
                out result));
        }

        /// <summary>
        /// Determines whether the specified properties has property.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        private bool HasProperty(Properties properties, string propertyName)
        {
            if (properties != null)
            {
                foreach (Property item in properties)
                {
                    if (item != null && item.Name == propertyName)
                        return true;
                }
            }
            return false;
        }
    }
}