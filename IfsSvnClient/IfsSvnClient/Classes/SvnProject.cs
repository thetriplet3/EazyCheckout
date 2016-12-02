using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnClient.Classes
{
    internal class SvnProject
    {
        internal string Name { get; set; }
        internal string Path { get; set; }

        internal SvnProject(SvnListEventArgs project)
        {            
            this.Path = project.Uri.AbsoluteUri;
            this.Name = project.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
