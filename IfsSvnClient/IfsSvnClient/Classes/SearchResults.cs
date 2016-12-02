using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal class SearchResults
    {
        internal string ComponentName { get; set; }
        internal string ComponentPath { get; set; }

        internal SearchResults(string componentName, Uri root, Uri componentUri)
        {
            this.ComponentName = componentName;
            this.ComponentPath = componentUri.AbsoluteUri.Replace(root.AbsoluteUri, "^");
        }

        public override string ToString()
        {
            return this.ComponentPath + "\t" + this.ComponentName + "\r\n";
        }
    }
}
