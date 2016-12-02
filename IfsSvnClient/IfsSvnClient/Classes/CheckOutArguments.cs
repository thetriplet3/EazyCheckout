using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal class CheckOutArguments
    {
        internal JobType Type { get; set; }
        internal SvnComponent[] CompornentArray { get; set; }
        internal bool HasDocCompornents { get; set; }
        internal string CheckOutPathProject { get; set; }
        internal string CheckOutPathNbproject { get; set; }
        internal string CheckOutPathDocument { get; private set; }
        internal string CheckOutPathDocumentEn { get; private set; }
        internal string CheckOutPathWorkspace { get; private set; }
        internal Uri ProjectNbprojectUri { get; private set; }
        internal Uri ProjectWorkspaceUri { get; private set; }
        internal Uri ProjectDocumentUri { get; private set; }
        internal bool ShowMoreLogInformation { get; private set; }
        internal bool CleanUpWhenLocked { get; private set; }

        internal CheckOutArguments(JobType type)
        {
            this.Type = type;
        }

        internal CheckOutArguments(JobType type, bool showMoreLogInformation, bool cleanUpWhenLocked, string projectPath, string checkOutPathProject, SvnComponent[] componentArray)
        {
            this.Type = type;
            this.ShowMoreLogInformation = showMoreLogInformation;
            this.CleanUpWhenLocked = cleanUpWhenLocked;
            this.CheckOutPathProject = checkOutPathProject;

            this.ProjectNbprojectUri = new Uri(projectPath + Properties.Resources.CheckOutPath_NbProject);
            this.ProjectDocumentUri = new Uri(projectPath + Properties.Resources.CheckOutPath_Documentation);
            this.ProjectWorkspaceUri = new Uri(projectPath + Properties.Resources.CheckOutPath_WorkSpace);
            
            this.CheckOutPathNbproject = this.CheckOutPathProject;
            this.CheckOutPathDocument = this.CheckOutPathProject;
            this.CheckOutPathWorkspace = this.CheckOutPathProject;
            if (this.CheckOutPathProject.EndsWith(@"\"))
            {
                this.CheckOutPathNbproject += Properties.Resources.CheckOutPath_NbProject;
                this.CheckOutPathDocument += Properties.Resources.CheckOutPath_Documentation;
                this.CheckOutPathWorkspace += Properties.Resources.CheckOutPath_WorkSpace;
            }
            else
            {
                this.CheckOutPathNbproject += @"\" + Properties.Resources.CheckOutPath_NbProject;
                this.CheckOutPathDocument += @"\" + Properties.Resources.CheckOutPath_Documentation;
                this.CheckOutPathWorkspace += @"\" + Properties.Resources.CheckOutPath_WorkSpace;
            }
            this.CheckOutPathNbproject += @"\";
            this.CheckOutPathDocument += @"\";
            this.CheckOutPathDocumentEn = this.CheckOutPathDocument + @"en";
            this.CheckOutPathWorkspace += @"\";

            this.CompornentArray = componentArray;

            if (this.CompornentArray.Count(c => c.Type == SvnComponent.SvnComponentType.Document) > 0)
            {
                this.HasDocCompornents = true;
            }
            else
            {
                this.HasDocCompornents = false;
            }
        }
    }
}
