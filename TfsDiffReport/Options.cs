using System;

namespace TfsDiffReport
{
    public class Options
    {
        /// <summary>
        /// TFS url (including team project)
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// The first changeset to report
        /// </summary>
        public int FirstChangeset { get; set; }

        /// <summary>
        /// The last changeset to report
        /// </summary>
        public int LastChangeset { get; set; }

        /// <summary>
        /// If specified, only files from these paths are reported
        /// </summary>
        public string[] Paths { get; set; }

        private string[] _extensions;
        /// <summary>
        /// Only files with these extensions are reported
        /// </summary>
        public string[] Extensions
        {
            get
            {
                return _extensions ?? new[] {".cs", ".csproj", ".cshtml", ".js"};
            }
            set { _extensions = value; }
        }

        /// <summary>
        /// Checks that the options are valid
        /// </summary>
        public bool Validate()
        {
            return !String.IsNullOrEmpty(ServerUrl) && FirstChangeset >= 0 && FirstChangeset <= LastChangeset;
        }
    }
}
