using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnClient.Classes
{
    internal class SearchArguments
    {
        internal SvnUriTarget RootUri { get; set; }
        internal SvnUriTarget ComponentListUri { get; set; }
        internal string SearchPattern { get; set; }
        internal Dictionary<string, List<string>> ComponentDictionary { get; set; }

        internal SearchArguments(string rootUri)
        {            
            this.RootUri = new SvnUriTarget(rootUri);
            this.ComponentListUri = new SvnUriTarget(rootUri + @"/applications");

            this.ComponentDictionary = new Dictionary<string, List<string>>();
        }

        internal SearchArguments(string rootUri, string pattern)
        {
            this.SearchPattern = pattern;

            this.SearchPattern = this.SearchPattern.Replace(".", @"\.");
            this.SearchPattern = this.SearchPattern.Replace("?", ".");
            this.SearchPattern = this.SearchPattern.Replace("*", ".*?");
            this.SearchPattern = this.SearchPattern.Replace(@"\", @"\\");
            this.SearchPattern = this.SearchPattern.Replace(" ", @"\s");

            this.RootUri = new SvnUriTarget(rootUri);
            this.ComponentListUri = new SvnUriTarget(rootUri + @"/applications");
        }
    }
}
