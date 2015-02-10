﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Handlers;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CloneFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        //[InlineData("git@github.com:libgit2/TestGitRepository")]
        public void CanClone(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);

                Assert.True(File.Exists(Path.Combine(scd.RootedDirectoryPath, "master.txt")));
                Assert.Equal(repo.Head.Name, "master");
                Assert.Equal(repo.Head.Tip.Id.ToString(), "49322bb17d3acc9146f98c97d078513228bbf3c0");
            }
        }

        [Theory]
        [InlineData("br2", "a4a7dce85cf63874e984719f4fdd239f5145052f")]
        [InlineData("packed", "41bc8c69075bbdb46c5c6f0566cc8cc5b46e8bd9")]
        [InlineData("test", "e90810b8df3e80c413d903f631643c716887138d")]
        public void CanCloneWithCheckoutBranchName(string branchName, string headTipId)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(BareTestRepoPath, scd.DirectoryPath, new CloneOptions { BranchName = branchName });

            using (var repo = new Repository(clonedRepoPath))
            {
                var head = repo.Head;

                Assert.Equal(branchName, head.Name);
                Assert.True(head.IsTracking);
                Assert.Equal(headTipId, head.Tip.Sha);
            }
        }

        private void AssertLocalClone(string url, string path = null, bool isCloningAnEmptyRepository = false)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var clonedRepo = new Repository(clonedRepoPath))
            using (var originalRepo = new Repository(path ?? url))
            {
                Assert.NotEqual(originalRepo.Info.Path, clonedRepo.Info.Path);
                Assert.Equal(originalRepo.Head, clonedRepo.Head);

                Assert.Equal(originalRepo.Branches.Count(), clonedRepo.Branches.Count(b => b.IsRemote));
                Assert.Equal(isCloningAnEmptyRepository ? 0 : 1, clonedRepo.Branches.Count(b => !b.IsRemote));

                Assert.Equal(originalRepo.Tags.Count(), clonedRepo.Tags.Count());
                Assert.Equal(1, clonedRepo.Network.Remotes.Count());
            }
        }

        [Fact]
        public void CanCloneALocalRepositoryFromALocalUri()
        {
            var uri = new Uri(Path.GetFullPath(BareTestRepoPath));
            AssertLocalClone(uri.AbsoluteUri, BareTestRepoPath);
        }

        [Fact]
        public void CanCloneALocalRepositoryFromAStandardPath()
        {
            AssertLocalClone(BareTestRepoPath);
        }

        [Fact]
        public void CanCloneALocalRepositoryFromANewlyCreatedTemporaryPath()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory(path);
            Repository.Init(scd.DirectoryPath);
            AssertLocalClone(scd.DirectoryPath, isCloningAnEmptyRepository: true);
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        //[InlineData("git@github.com:libgit2/TestGitRepository")]
        public void CanCloneBarely(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions
                {
                    IsBare = true
                });

            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(scd.RootedDirectoryPath + Path.DirectorySeparatorChar, repo.Info.Path);
                Assert.True(repo.Info.IsBare);
            }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        public void WontCheckoutIfAskedNotTo(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
            {
                Checkout = false
            });

            using (var repo = new Repository(clonedRepoPath))
            {
                Assert.False(File.Exists(Path.Combine(repo.Info.WorkingDirectory, "master.txt")));
            }
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        public void CallsProgressCallbacks(string url)
        {
            bool transferWasCalled = false;
            bool progressWasCalled = false;
            bool updateTipsWasCalled = false;
            bool checkoutWasCalled = false;

            var scd = BuildSelfCleaningDirectory();

            Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
            {
                OnTransferProgress = _ => { transferWasCalled = true; return true; },
                OnProgress = progress => { progressWasCalled = true; return true; },
                OnUpdateTips = (name, oldId, newId) => { updateTipsWasCalled = true; return true; },
                OnCheckoutProgress = (a, b, c) => checkoutWasCalled = true
            });

            Assert.True(transferWasCalled);
            Assert.True(progressWasCalled);
            Assert.True(updateTipsWasCalled);
            Assert.True(checkoutWasCalled);
        }

        [SkippableFact]
        public void CanCloneWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(Constants.PrivateRepoUrl, scd.DirectoryPath,
                new CloneOptions()
                {
                    CredentialsProvider = Constants.PrivateRepoCredentials
                });


            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);
            }
        }

        [Theory]
        [InlineData("https://libgit2@bitbucket.org/libgit2/testgitrepository.git", "libgit3", "libgit3")]
        public void CanCloneFromBBWithCredentials(string url, string user, string pass)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
            {
                CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                {
                    Username = user,
                    Password = pass,
                }
            });

            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);
            }
        }

        [Fact]
        public void CloningAnUrlWithoutPathThrows()
        {
            var scd = BuildSelfCleaningDirectory();

            Assert.Throws<InvalidSpecificationException>(() => Repository.Clone("http://github.com", scd.DirectoryPath));
        }

        [Theory]
        [InlineData("git://github.com/libgit2/TestGitRepository")]
        public void CloningWithoutWorkdirPathThrows(string url)
        {
            Assert.Throws<ArgumentNullException>(() => Repository.Clone(url, null));
        }

        [Fact]
        public void CloningWithoutUrlThrows()
        {
            var scd = BuildSelfCleaningDirectory();

            Assert.Throws<ArgumentNullException>(() => Repository.Clone(null, scd.DirectoryPath));
        }

        /// <summary>
        /// Private helper to record the callbacks that were called as part of a clone.
        /// </summary>
        private class CallbacksCalled
        {
            /// <summary>
            /// Was checkout progress called.
            /// </summary>
            public bool CheckoutProgressCalled { get; set; }

            /// <summary>
            /// Was remote ref update called.
            /// </summary>
            public bool RemoteRefUpdateCalled { get; set; }

            /// <summary>
            /// Was the transition callback called when starting
            /// work on this repository.
            /// </summary>
            public bool StartingWorkInRepositoryCalled { get; set; }

            /// <summary>
            /// Was the transition callback called when finishing
            /// work on this repository.
            /// </summary>
            public bool FinishedWorkInRepositoryCalled { get; set; }
        }

        [Fact]
        public void CanRecursivelyCloneSubmodules()
        {
            var uri = new Uri(Path.GetFullPath(SandboxSubmoduleSmallTestRepo()));
            var scd = BuildSelfCleaningDirectory();
            string relativeSubmodulePath = "submodule_target_wd";

            Dictionary<string, CallbacksCalled> callbacks = new Dictionary<string, CallbacksCalled>();

            CallbacksCalled currentEntry = null;
            bool unexpectedOrderOfCallbacks = false;

            CheckoutProgressHandler checkoutProgressHandler = (x, y, z) =>
                {
                    if (currentEntry != null)
                    {
                        currentEntry.CheckoutProgressCalled = true;
                    }
                    else
                    {
                        unexpectedOrderOfCallbacks = true;
                    }
                };

            UpdateTipsHandler remoteRefUpdated = (x, y, z) =>
            {
                if (currentEntry != null)
                {
                    currentEntry.RemoteRefUpdateCalled = true;
                }
                else
                {
                    unexpectedOrderOfCallbacks = true;
                }

                return true;
            };

            CurrentRepositoryHandler repositoryTransition = (x, y) =>
                {
                    if (y == CurrentRepositoryTransition.Finished)
                    {
                        if (currentEntry != null)
                        {
                            currentEntry.FinishedWorkInRepositoryCalled = true;
                        }
                        else
                        {
                            unexpectedOrderOfCallbacks = true;
                        }
                    }
                    else
                    {
                        currentEntry = new CallbacksCalled();
                        currentEntry.StartingWorkInRepositoryCalled = true;
                        callbacks.Add(x.RepositoryPath, currentEntry);
                    }

                    return true;
                };
            
            CloneOptions options = new CloneOptions()
            {
                RecurseSubmodules = true,
                OnCheckoutProgress = checkoutProgressHandler,
                OnUpdateTips = remoteRefUpdated,
                CurrentRepositoryChanged = repositoryTransition,
            };

            string clonedRepoPath = Repository.Clone(uri.AbsolutePath, scd.DirectoryPath, options);
            string workDirPath;
            using(Repository repo = new Repository(clonedRepoPath))
            {
                workDirPath = repo.Info.WorkingDirectory.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            }

            // Verification:
            // No callbacks were called out of the expected order.
            Assert.False(unexpectedOrderOfCallbacks);

            string[] expectedRepositoryPaths = new string[] { workDirPath, Path.Combine(workDirPath, relativeSubmodulePath) };

            // Callbacks for each expected repository that is cloned
            foreach (string repoName in expectedRepositoryPaths)
            {
                CallbacksCalled entry = null;
                Assert.True(callbacks.TryGetValue(repoName, out entry), string.Format("{0} was not found in callbacks.", repoName));
                Assert.True(entry.StartingWorkInRepositoryCalled);
                Assert.True(entry.FinishedWorkInRepositoryCalled);
                Assert.True(entry.CheckoutProgressCalled);
                Assert.True(entry.RemoteRefUpdateCalled);
            }

            // submodule is initialized
            // To Verify: submodule head commit
            using(Repository repo = new Repository(clonedRepoPath))
            {
                var sm = repo.Submodules[relativeSubmodulePath];
                Assert.True(sm.RetrieveStatus().HasFlag(SubmoduleStatus.InWorkDir |
                                                        SubmoduleStatus.InConfig |
                                                        SubmoduleStatus.InIndex |
                                                        SubmoduleStatus.InHead));

                Assert.False(repo.RetrieveStatus().IsDirty);
            }
        }
    }
}
