using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal enum JobType
    {
        CheckOut,
        Load,
        LoadProjects,
        LoadTags,
        LoadComponents,
        LoadChildFolders,
        CreateProject,
        CreateComponents,
        CreateBranch,
        CreateTag,
    }
}
