using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("checkin")]
    [Description("checkin [options] [ref-to-checkin]")]
    [RequiresValidGitRepository]
    public class Checkin : GitTfsCommand
    {
        private readonly Globals globals;
        private readonly TextWriter stdout;
        private readonly CheckinOptions checkinOptions;

        public Checkin(Globals globals, TextWriter stdout, CheckinOptions checkinOptions)
        {
            this.globals = globals;
            this.checkinOptions = checkinOptions;
            this.stdout = stdout;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeOptionResults(checkinOptions); }
        }

        public int Run(IList<string> args)
        {
            if (args.Count != 0 && args.Count != 1)
                return Help.ShowHelpForInvalidArguments(this);
            var refToCheckin = args.Count > 0 ? args[0] : "HEAD";
            var tfsParents = globals.Repository.GetParentTfsCommits(refToCheckin);
            if (globals.UserSpecifiedRemoteId != null)
                tfsParents = tfsParents.Where(changeset => changeset.Remote.Id == globals.UserSpecifiedRemoteId);
            switch (tfsParents.Count())
            {
                case 1:
                    var changeset = tfsParents.First();
                    changeset.Remote.Checkin(refToCheckin, changeset);
                    return GitTfsExitCodes.OK;
                case 0:
                    stdout.WriteLine("No TFS parents found!");
                    return GitTfsExitCodes.InvalidArguments;
                default:
                    stdout.WriteLine("More than one parent found! Use -i to choose the correct parent from: ");
                    foreach (var parent in tfsParents)
                    {
                        stdout.WriteLine("  " + parent.Remote.Id);
                    }
                    return GitTfsExitCodes.InvalidArguments;
            }
        }
    }
}
