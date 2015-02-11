using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    class Operation
    {
        private readonly Repository repository;

        /// <summary>
        /// Continue the current operation.
        /// </summary>
        public void Continue()
        {

        }

        /// <summary>
        /// Abort the current operation.
        /// </summary>
        public void Abort()
        {

        }

        /// <summary>
        /// Perform a rebase.
        /// </summary>
        /// <param name="branch">The branch to rebase.</param>
        /// <param name="upstream">The starting commit to rebase.</param>
        /// <param name="onto">The branch to rebase onto.</param>
        public void Rebase(Branch branch, Branch upstream, Branch onto)
        {
            ReferenceSafeHandle branchRefPtr = null;
            ReferenceSafeHandle upstreamRefPtr = null;
            ReferenceSafeHandle ontoRefPtr = null;

            GitAnnotatedCommitHandle annotatedBranchCommitHandle = null;
            GitAnnotatedCommitHandle annotatedUpstreamRefPtrCommitHandle = null;
            GitAnnotatedCommitHandle annotatedOntoRefPtrCommitHandle = null;

            GitRebaseOptions options = new GitRebaseOptions();
            GitCheckoutOptsWrapper checkoutOptionsWrapper = null;
            CheckoutOptions checkoutOptions = new CheckoutOptions();

                try
            {
                branchRefPtr = repository.Refs.RetrieveReferencePtr(branch.CanonicalName);
                upstreamRefPtr = repository.Refs.RetrieveReferencePtr(upstream.CanonicalName);
                ontoRefPtr = repository.Refs.RetrieveReferencePtr(onto.CanonicalName);

                annotatedBranchCommitHandle = Proxy.git_annotated_commit_from_ref(repository.Handle, branchRefPtr);
                annotatedUpstreamRefPtrCommitHandle = Proxy.git_annotated_commit_from_ref(repository.Handle, upstreamRefPtr);
                annotatedOntoRefPtrCommitHandle = Proxy.git_annotated_commit_from_ref(repository.Handle, ontoRefPtr);

                RebaseSafeHandle rebaseOperationHandle = Proxy.git_rebase_init(repository.Handle,
                    annotatedBranchCommitHandle,
                    annotatedUpstreamRefPtrCommitHandle,
                    annotatedOntoRefPtrCommitHandle,
                    null, ref options);

                checkoutOptionsWrapper = new GitCheckoutOptsWrapper(checkoutOptions);
                GitCheckoutOpts gitCheckoutOpts = checkoutOptionsWrapper.Options;

                GitRebaseOperation rebaseOperationReport = null;
                bool shouldContinue = true;
                while (shouldContinue)
                {
                    rebaseOperationReport = Proxy.git_rebase_next(rebaseOperationHandle, ref gitCheckoutOpts);

                    switch (rebaseOperationReport.type)
                    {
                        case git_rebase_operation.Pick:
                            // commit and continue.
                            // Proxy.git_rebase_commit()
                            break;
                        case git_rebase_operation.Squash:
                            // Proxy.git_rebase_commit()
                            break;
                        case git_rebase_operation.Edit:
                            break;
                        case git_rebase_operation.Exec:
                            break;
                        case git_rebase_operation.Fixup:
                            break;
                        case git_rebase_operation.Reword:
                            break;
                    }

                    shouldContinue = false;
                }
            }
            finally
            {
                DisposeIfNecessary(branchRefPtr);
                branchRefPtr = null;
                DisposeIfNecessary(upstreamRefPtr);
                upstreamRefPtr = null;
                DisposeIfNecessary(ontoRefPtr);
                ontoRefPtr = null;

                DisposeIfNecessary(annotatedBranchCommitHandle);
                annotatedBranchCommitHandle = null;
                DisposeIfNecessary(annotatedUpstreamRefPtrCommitHandle);
                annotatedUpstreamRefPtrCommitHandle = null;
                DisposeIfNecessary(annotatedOntoRefPtrCommitHandle);
                annotatedOntoRefPtrCommitHandle = null;

                DisposeIfNecessary(checkoutOptionsWrapper);
            }
        }

        private void DisposeIfNecessary(IDisposable disposableObject)
        {
            if(disposableObject != null)
            {
                disposableObject.Dispose();
            }
        }
    }
}
