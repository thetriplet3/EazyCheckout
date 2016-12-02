using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace IfsSvnClient.Classes
{
    internal class SvnComponent
    {
        internal string Name { get; set; }
        internal string Path { get; set; }
        internal SvnComponentType Type { get; private set; }

        internal SvnComponent(string infor)
        {
            string[] values = infor.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);

            this.Path = values[0];
            this.Name = values[1];

            this.SetType();
        }

        internal SvnComponent(SvnListEventArgs component)
        {
            this.Path = component.Uri.AbsoluteUri;
            this.Name = component.Name;

            this.SetType();
        }

        private void SetType()
        {
            if (this.Path.Contains(Properties.Resources.CheckOutPath_Documentation))
            {
                this.Type = SvnComponentType.Document;
            }
            else
            {
                this.Type = SvnComponentType.Work;

            }
        }

        public override string ToString()
        {
            return Name;
        }

        internal enum SvnComponentType
        {
            Document,
            Work
        }
    }
}
