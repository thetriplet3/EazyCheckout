using SharpSvn;
using System.Collections.Generic;
using System.Windows.Controls;

namespace IfsSvnClient.Classes
{
    internal class TagArguments
    {
        internal JobType Type { get; set; }
        internal TreeViewItem SelectedFolderView { get; set; }
        internal SvnListEventArgs SelectedTag { get; set; }
        internal string BranchName { get; set; }

        internal SvnListEventArgs SelectedTrunk { get; set; }
        internal string TagName { get; set; }

        internal List<SvnListEventArgs> FolderList { get; set; }

        internal TagArguments(JobType type)
        {
            this.Type = type;
        }
    }
}