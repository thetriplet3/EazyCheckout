using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IfsSvnClient.Classes
{
    internal class SvnManagerArguments
    {
        internal JobType Type { get; set; }
        internal List<string> ComponentList { get; private set; }
        internal string ProjectName { get; set; }

        internal SvnManagerArguments(JobType type, string param)
        {
            if (string.IsNullOrWhiteSpace(param) == false)
            {
                this.Type = type;

                if (this.Type == JobType.CreateProject)
                {
                    this.ProjectName = param.Trim().ToLower();
                }
                else if (this.Type == JobType.CreateComponents)
                {
                    List<string> tempList = param.Split(new char[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    this.ComponentList = tempList.Where(c => string.IsNullOrWhiteSpace(c) == false).Distinct().Select(c => c.Trim().ToLower()).ToList();
                }
            }
        }
    }
}
