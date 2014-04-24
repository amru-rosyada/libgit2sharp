﻿using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Merge behavior.
    /// </summary>
    public sealed class MergeOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeOptions"/> class.
        /// By default, a fast-forward merge will be performed if possible, and
        /// if a merge commit is created, then it will be commited.
        /// </summary>
        public MergeOptions()
        {
            CommitOnSuccess = true;
        }

        /// <summary>
        /// Commit the merge if the merge is successful and this is a non-fast-forward merge.
        /// If this is a fast-forward merge, then there is no merge commit and this option
        /// will not affect the merge.
        /// </summary>
        public bool CommitOnSuccess { get; set; }

        /// <summary>
        /// The type of merge to perform.
        /// </summary>
        public FastForwardStrategy FastForwardStrategy { get; set; }

        /// <summary>
        /// How Checkout should handle writing out conflicting index entries.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

        #region IConvertableToGitCheckoutOpts

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(null, null);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                return CheckoutStrategy.GIT_CHECKOUT_SAFE_CREATE |
                       CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS |
                       GitCheckoutOptsWrapper.CheckoutStrategyFromFileConflictStrategy(FileConflictStrategy);
            }
        }

        CheckoutNotifyFlags IConvertableToGitCheckoutOpts.CheckoutNotifyFlags
        {
            get { return CheckoutNotifyFlags.None; }
        }

        #endregion
    }

    /// <summary>
    /// Strategy used for merging.
    /// </summary>
    public enum FastForwardStrategy
    {
        /// <summary>
        /// Default fast-forward strategy. This will perform a fast-forward merge
        /// if possible, otherwise will perform a non-fast-forward merge that
        /// results in a merge commit.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Do not fast-forward. Always creates a merge commit.
        /// </summary>
        NoFastFoward = 1, /* GIT_MERGE_NO_FASTFORWARD */

        /// <summary>
        /// Only perform fast-forward merges.
        /// </summary>
        FastForwardOnly = 2, /* GIT_MERGE_FASTFORWARD_ONLY */
    }
}
