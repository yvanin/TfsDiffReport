﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace TfsDiffReport
{
    public class DiffReportRunner
    {
        private static readonly object Lock = new object();

        private readonly Options _options;
        private VersionControlServer _sourceControl;

        /// <summary>
        /// Constructor. Requires valid options.
        /// </summary>
        /// <param name="options"></param>
        public DiffReportRunner(Options options)
        {
            if (!options.Validate())
                throw new ArgumentException("TfsDiffReport options are not valid.");

            _options = options;
            ConnectToTfs();
        }

        private void ConnectToTfs()
        {
            var tfsTeamProjects = new TfsTeamProjectCollection(new Uri(_options.ServerUrl),
                                                               new TfsClientCredentials(new WindowsCredential(), true));
            tfsTeamProjects.EnsureAuthenticated();
            _sourceControl = tfsTeamProjects.GetService<VersionControlServer>();
        }

        public void GenerateReport()
        {
            Parallel.For(_options.FirstChangeset, _options.LastChangeset + 1, i =>
                {
                    try
                    {
                        GenerateReportForChangeset(i);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed report for C{0}. Exception: {1}\r\n", i, ex.Message);
                    }
                });
        }

        private void GenerateReportForChangeset(int changesetId)
        {
            Changeset changeset = _sourceControl.GetChangeset(changesetId);
            Change[] changes = _sourceControl.GetChangesForChangeset(changesetId, false, Int32.MaxValue, null);

            Parallel.ForEach(changes.Where(c => c.Item.ItemType == ItemType.File), change =>
                {
                    // filter by path if needed
                    if (_options.Paths != null)
                        if (!_options.Paths.Any(change.Item.ServerItem.StartsWith))
                            return;

                    // filter by extension
                    if (!_options.Extensions.Any(change.Item.ServerItem.EndsWith))
                        return;

                    var itemName = change.Item.ServerItem.Substring(change.Item.ServerItem.LastIndexOf('/') + 1);

                    string diffFileName;
                    lock (Lock)
                    {
                        diffFileName = String.Format("{0}_{1}.diff", changesetId, itemName);

                        int i = 1;
                        while (File.Exists(diffFileName))
                        {
                            i++;
                            diffFileName = String.Format("{0}_{1} ({2}).diff", changesetId, itemName, i);
                        }
                        File.Create(diffFileName).Close();
                    }

                    try
                    {
                        if (change.ChangeType.HasFlag(ChangeType.Add) || change.ChangeType.HasFlag(ChangeType.Undelete))
                        {
                            GenerateAddOrUndeleteReport(diffFileName, changeset, change);
                        }
                        else if (change.ChangeType.HasFlag(ChangeType.Delete))
                        {
                            GenerateDeleteReport(diffFileName, changeset, change);
                        }
                        else if (change.ChangeType.HasFlag(ChangeType.Edit))
                        {
                            GenerateEditReport(diffFileName, changeset, change);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Failed C{0}: {1}. Exception: {2}\r\n",
                                                changesetId, change.Item.ServerItem, ex.Message);
                    }
                });
        }

        /// <summary>
        /// Generates a report about Add or Undelete change
        /// </summary>
        private void GenerateAddOrUndeleteReport(string diffFileName, Changeset changeset, Change change)
        {
            var item = _sourceControl.GetItem(change.Item.ServerItem, new ChangesetVersionSpec(changeset.ChangesetId));

            string[] itemLines;

            using (var itemStream = item.DownloadFile())
            using (var reader = new StreamReader(itemStream))
            {
                itemLines = reader.ReadToEnd().Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
            }

            using (var writer = new StreamWriter(diffFileName))
            {
                WriteHeader(writer, changeset);
                writer.WriteLine("File {0}", change.ChangeType.HasFlag(ChangeType.Add) ? "added" : "undeleted");
                writer.WriteLine();
                writer.WriteLine("===================================================================");
                writer.WriteLine("--- Server: {0};C{1}", change.Item.ServerItem, changeset.ChangesetId);
                writer.WriteLine("***************");
                writer.WriteLine("*** 0,0 ****");
                writer.WriteLine("--- 1,{0} ----", itemLines.Length);
                foreach (var line in itemLines)
                {
                    writer.WriteLine("+{0}", line);
                }
                writer.WriteLine("===================================================================");
            }
        }

        /// <summary>
        /// Generates a report about Delete change
        /// </summary>
        private void GenerateDeleteReport(string diffFileName, Changeset changeset, Change change)
        {
            var item = _sourceControl.GetItem(change.Item.ServerItem,
                                              new ChangesetVersionSpec(changeset.ChangesetId - 1));

            string[] itemLines;

            using (var itemStream = item.DownloadFile())
            using (var reader = new StreamReader(itemStream))
            {
                itemLines = reader.ReadToEnd().Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
            }

            using (var writer = new StreamWriter(diffFileName))
            {
                WriteHeader(writer, changeset);
                writer.WriteLine("File deleted");
                writer.WriteLine();
                writer.WriteLine("===================================================================");
                writer.WriteLine("*** Server: {0};C{1}", change.Item.ServerItem, changeset.ChangesetId);
                writer.WriteLine("***************");
                writer.WriteLine("*** 1,{0} ****", itemLines.Length);
                foreach (var line in itemLines)
                {
                    writer.WriteLine("-{0}", line);
                }
                writer.WriteLine("--- 0,0 ----");
                writer.WriteLine("===================================================================");
            }
        }

        /// <summary>
        /// Generates a report about Edit change
        /// </summary>
        private void GenerateEditReport(string diffFileName, Changeset changeset, Change change)
        {
            IDiffItem origItem = new DiffItemVersionedFile(_sourceControl, change.Item.ServerItem,
                                                           new ChangesetVersionSpec(changeset.ChangesetId - 1));
            IDiffItem newItem = new DiffItemVersionedFile(_sourceControl, change.Item.ServerItem,
                                                          new ChangesetVersionSpec(changeset.ChangesetId));

            using (var writer = new StreamWriter(diffFileName))
            {
                WriteHeader(writer, changeset);

                var diffOptions = new DiffOptions
                    {
                        UseThirdPartyTool = false,
                        OutputType = DiffOutputType.Context,
                        ContextLines = 0,
                        StreamWriter = writer
                    };

                Difference.DiffFiles(_sourceControl, origItem, newItem, diffOptions, null, true);
            }
        }

        /// <summary>
        /// Writes changeset description to the stream
        /// </summary>
        private static void WriteHeader(StreamWriter writer, Changeset changeset)
        {
            writer.WriteLine("User: {0}", changeset.CommitterDisplayName);
            writer.WriteLine("Date: {0}", changeset.CreationDate.ToShortDateString());
            writer.WriteLine("Changeset: {0}", changeset.ChangesetId);
        }
    }
}
