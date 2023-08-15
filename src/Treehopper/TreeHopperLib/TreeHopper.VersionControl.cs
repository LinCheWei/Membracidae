using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using TreeHopper.Deserialize;

namespace TreeHopper.VersionControl
{
    public class Hopper
    {
        private Repository Repo;
        private string FileName;
        private string FilePath;
        private List<Commit> Commits;

        public Hopper(string input)
        {
            this.FileName = Path.GetFileName(input);
            this.FilePath = Path.GetDirectoryName(input);

            this.Repo = new Repository(FilePath);

            this.Commits = new List<Commit>();

            /// search through commit that contains the changes of this ghx
            var fileHistory = this.Repo.Commits.QueryBy(this.FileName, new CommitFilter { SortBy = CommitSortStrategies.Time });

            /// Acquire commit info
            foreach (LogEntry e in fileHistory)
            {
                this.Commits.Add(e.Commit);
            }
        }

        public List<string> getVersionList(bool complete)
        {
            var list = new List<string>();
            foreach (Commit c in this.Commits)
            {
                list.Add(this.getTargetSHA(c, complete));
            }
            return list;
        }

        public string getTargetSHA(Commit commit, bool complete)
        {
            string sha = null;
            foreach (TreeEntry te in commit.Tree)
            {
                if (te.Name == this.FileName)
                {
                    sha = te.Target.Sha;
                }
            }

            if (sha != null)
            {
                if (complete) { return  sha; }
                else { return sha.Substring(0,7); }
            }

            return sha;
        }

        public GhxDocument getGhxVersion(Commit commit)
        {
            /// Search for the Blob contains the ghx file 
            var tarBlob = this.Repo.Lookup<Blob>(commit.Tree.Sha + ":" + this.FileName);
            return new GhxDocument(tarBlob.GetContentStream());
        }
    }

}
