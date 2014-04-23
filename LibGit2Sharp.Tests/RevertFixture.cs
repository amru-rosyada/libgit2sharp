using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;
using System;

namespace LibGit2Sharp.Tests
{
    public class RevertFixture : BaseFixture
    {
        // Simple revert
        [Fact]
        public void CanRevert()
        {
            const string modifiedFilePath = "a.txt";
            const string revertBranchName = "refs/heads/revert";

            string path = CloneRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch
                Branch branch = repo.Checkout(revertBranchName);
                Assert.NotNull(branch);

                // Revert tip commit
                Commit newCommit = repo.Revert(repo.Head.Tip, Constants.Signature);

                // Verify commit was made
                Assert.NotNull(newCommit);

                // Verify commit ID
                Assert.Equal("04746060fa753c9970d88a0b59151d7b212ac903", newCommit.Id.Sha);

                // Verify workspace is clean
                Assert.True(repo.Index.IsFullyMerged);
                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                // This is the ID of the blob containing the expected content.
                Blob expectedBlob = repo.Lookup<Blob>("bc90ea420cf6c5ae3db7dcdffa0d79df567f219b");
                Assert.NotNull(expectedBlob);

                // Verify contents of Index
                IndexEntry revertedIndexEntry = repo.Index[modifiedFilePath];
                Assert.NotNull(revertedIndexEntry);

                Assert.Equal(expectedBlob.Id, revertedIndexEntry.Id);

                // Verify contents of workspace
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, modifiedFilePath);
                Assert.Equal(expectedBlob.GetContentText(new FilteringOptions(modifiedFilePath)), File.ReadAllText(fullPath));
            }
        }

        // Simple revert no commit
        [Fact]
        public void CanRevertAndNotCommit()
        {
            const string modifiedFilePath = "a.txt";
            const string revertBranchName = "refs/heads/revert";

            string path = CloneRevertTestRepo();
            using (var repo = new Repository(path))
            {
                string modifiedFileFullPath = Path.Combine(repo.Info.WorkingDirectory, modifiedFilePath);

                // Checkout the revert branch
                Branch branch = repo.Checkout(revertBranchName);
                Assert.NotNull(branch);

                // Revert tip commit
                Commit newCommit = repo.Revert(repo.Head.Tip, Constants.Signature, new RevertOptions() { CommitOnSuccess = false });

                // Verify commit was made
                Assert.Null(newCommit);

                // Verify workspace is dirty
                FileStatus fileStatus = repo.Index.RetrieveStatus(modifiedFilePath);
                Assert.Equal(FileStatus.Staged, fileStatus);

                // This is the ID of the blob containing the expected content.
                Blob expectedBlob = repo.Lookup<Blob>("bc90ea420cf6c5ae3db7dcdffa0d79df567f219b");
                Assert.NotNull(expectedBlob);

                // Verify contents of Index
                IndexEntry revertedIndexEntry = repo.Index[modifiedFilePath];
                Assert.NotNull(revertedIndexEntry);

                Assert.Equal(expectedBlob.Id, revertedIndexEntry.Id);

                // Verify contents of workspace
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, modifiedFilePath);
                Assert.Equal(expectedBlob.GetContentText(new FilteringOptions(modifiedFilePath)), File.ReadAllText(fullPath));
            }
        }

        // revert with conflicts
        [Fact]
        public void RevertWithConflictDoesNotCommit()
        {
            const string revertBranchName = "refs/heads/revert";

            string path = CloneRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch
                Branch branch = repo.Checkout(revertBranchName);
                Assert.NotNull(branch);

                Commit commitToRevert = repo.Lookup<Commit>("cb4f7f0eca7a0114cdafd8537332aa17de36a4e9");
                Assert.NotNull(commitToRevert);

                // Revert tip commit
                Commit newCommit = repo.Revert(commitToRevert, Constants.Signature);

                // Verify no commit was made
                Assert.Null(newCommit);

                // Verify there is a conflict
                Assert.False(repo.Index.IsFullyMerged);

                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus("b.txt"));
                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus("c.txt"));
            }
        }

        [Theory]
        [InlineData(CheckoutFileConflictStrategy.Ours)]
        [InlineData(CheckoutFileConflictStrategy.Theirs)]
        public void RevertCanSpecifyFileConflictStrategy(CheckoutFileConflictStrategy conflictStrategy)
        {
            const string conflictedFilePath = "a.txt";
            const string revertBranchName = "refs/heads/revert";

            string path = CloneRevertTestRepo();
            using (var repo = new Repository(path))
            {
                // Checkout the revert branch
                Branch branch = repo.Checkout(revertBranchName);
                Assert.NotNull(branch);

                RevertOptions options = new RevertOptions()
                {
                    FileConflictStrategy = conflictStrategy,
                };

                Commit newCommit = repo.Revert(repo.Head.Tip.Parents.First(), Constants.Signature, options);

                // Verify there is a conflict.
                Assert.False(repo.Index.IsFullyMerged);

                Conflict conflict = repo.Index.Conflicts[conflictedFilePath];
                Assert.NotNull(conflict);

                Assert.NotNull(conflict);
                Assert.NotNull(conflict.Theirs);
                Assert.NotNull(conflict.Ours);

                // Get the blob containing the expected content.
                Blob expectedBlob = null;
                switch (conflictStrategy)
                {
                    case CheckoutFileConflictStrategy.Theirs:
                        expectedBlob = repo.Lookup<Blob>(conflict.Theirs.Id);
                        break;
                    case CheckoutFileConflictStrategy.Ours:
                        expectedBlob = repo.Lookup<Blob>(conflict.Ours.Id);
                        break;
                    default:
                        throw new Exception("Unexpected FileConflictStrategy");
                }

                Assert.NotNull(expectedBlob);

                // Check the content of the file on disk matches what is expected.
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictedFilePath));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictedFilePath)));
            }
        }

        // Revert with progress update
        [Fact]
        public void RevertReportsCheckoutProgress()
        {
            const string modifiedFilePath = "a.txt";
            const string revertBranchName = "refs/heads/revert";

            string repoPath = CloneRevertTestRepo();
            using (var repo = new Repository(repoPath))
            {
                // Checkout the revert branch
                Branch branch = repo.Checkout(revertBranchName);
                Assert.NotNull(branch);

                bool wasCalled = false;

                RevertOptions options = new RevertOptions()
                {
                    OnCheckoutProgress = (path, completed, total) => wasCalled = true
                };

                // Revert tip commit
                Commit newCommit = repo.Revert(repo.Head.Tip, Constants.Signature, options);

                Assert.True(wasCalled);
            }
        }
    }
}
