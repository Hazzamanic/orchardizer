using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.Linq;

namespace Orchardizer.Helpers
{
    public class ExportTypesReader
    {
        public void ReadExport()
        {
            var doc = XElement.Load("C:/Users/Harry/Downloads/export.xml");
            if (doc.Name.ToString() != "Metadata")
                return;

            var typeElement = doc.Element("Types");
            if (typeElement == null)
                return;

            var partsElement = doc.Element("Parts");
            bool checkParts = partsElement != null;

            var partsToCheck = new List<string>();

            foreach (var ele in typeElement.Elements())
            {
                var definition = new List<string>();
                var name = ele.Name.ToString();
                // add name of type to parts we need to check for fields
                // Orchard types have a default part that shares the name of the type, where fields are added to
                Add(partsToCheck, name);
                // create definition
                definition.Add(TypeCreator(name));
                // add type settings
                AddContentTypeSettings(definition, ele);

                foreach (var part in ele.Elements())
                {
                    // add part to content type
                    definition.Add(String.Format(".WithPart(\"{0}\")", part.Name));
                    // add part to list we need to check
                    Add(partsToCheck, name);
                    // add part settings
                    definition.AddRange(part.Attributes().Select(attr => String.Format(".WithSetting(\"{0}\",\"{1}\")", attr.Name, attr.Value)));
                }
            }

            // here we will check for fields
            if (partsElement != null) return;
            foreach (var part in partsToCheck)
            {
                var partElement = partsElement.Element(part);
                if (partElement == null) continue;
                // no fields so continue
                if(!partElement.HasElements) continue;

                var definition = new List<string>();

                definition.Add(TypeCreator(part));
                foreach (var field in partElement.Elements())
                {
                    var fieldInfo = field.Name.ToString().Split(',');
                    definition.Add(String.Format(".WithField(\"{0}\", field => field.OfType(\"{1}\"", fieldInfo[0], fieldInfo[1]));
                    var displayName = field.Attribute("DisplayName") == null ? field.Attribute("DisplayName").Value : fieldInfo[0];
                    definition.Add(String.Format(".WithDisplayName(\"{0}\")", displayName));

                    // add field settings
                    definition.AddRange(field.Attributes()
                        .Where(e => e.Name != "DisplayName")
                        .Select(attr => String.Format(".WithSetting(\"{0}\",\"{1}\")", attr.Name, attr.Value))
                    );
                }
            }
            
        }

        /// <summary>
        /// Builds the migration statements from the lists of strings.
        /// </summary>
        /// <param name="migrationSteps">The migration steps.</param>
        /// <returns></returns>
        private string BuildMigrations(List<string> migrationSteps)
        {
            // build the nice looking migration sexiness here

            return "";
        }

        /// <summary>
        /// Builds the type definition
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private string TypeCreator(string name)
        {
            return String.Format("ContentDefinitionManager.AlterTypeDefinition(\"{0}\", type => type", name);
        }

        /// <summary>
        /// Builds the alter part statement
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        private string PartAlterer(string name)
        {
            return String.Format("ContentDefinitionManager.AlterPartDefinition((\"{0}\", part => part", name);
        }

        /// <summary>
        /// Adds the content type settings.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="element">The element.</param>
        private void AddContentTypeSettings(List<string> definition, XElement element)
        {
            foreach (var attr in element.Attributes())
            {
                switch (attr.Name.ToString())
                {
                    case "ContentTypeSettings.Creatable":
                        if(attr.Value == "True") definition.Add(".Creatable()");
                        continue;
                    case "ContentTypeSettings.Draftable":
                        if (attr.Value == "True") definition.Add(".Draftable()");
                        continue;
                    case "DisplayName":
                        definition.Add(attr.Value);
                        continue;
                    case "TypeIndexing.Indexes":
                        definition.Add(attr.ToString());
                        continue;
                }
            }
        }

        private void Add(List<string> parts, string part)
        {
            if(parts.All(e => e != part)) parts.Add(part);
        }
    }
}
