using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Revert behavior.
    /// </summary>
    public sealed class RevertOptions : IConvertableToGitCheckoutOpts
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RevertOptions"/> class.
        /// By default the revert will be committed if there are no conflicts.
        /// </summary>
        public RevertOptions()
        {
            CommitOnSuccess = true;
        }

        /// <summary>
        /// Delegate that checkout progress will be reported through.
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Commit the revert if the revert is successful.
        /// </summary>
        public bool CommitOnSuccess { get; set; }

        /// <summary>
        /// The index of the parent commits to use as the parent commit.
        /// </summary>
        public int Mainline;

        /// <summary>
        /// How Checkout should handle writing out conflicting index entries.
        /// </summary>
        public CheckoutFileConflictStrategy FileConflictStrategy { get; set; }

        #region IConvertableToGitCheckoutOpts

        CheckoutCallbacks IConvertableToGitCheckoutOpts.GenerateCallbacks()
        {
            return CheckoutCallbacks.From(OnCheckoutProgress, null);
        }

        CheckoutStrategy IConvertableToGitCheckoutOpts.CheckoutStrategy
        {
            get
            {
                return CheckoutStrategy.GIT_CHECKOUT_SAFE_CREATE |
                       CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS |
                       CheckoutConflictStrategyFlags;
            }
        }

        CheckoutNotifyFlags IConvertableToGitCheckoutOpts.CheckoutNotifyFlags
        {
            get { return CheckoutNotifyFlags.None; }
        }

        #endregion IConvertableToGitCheckoutOpts

        /// <summary>
        /// Method to translate from <see cref="FileConflictStrategy"/> to <see cref="CheckoutStrategy"/> flags.
        /// </summary>
        private CheckoutStrategy CheckoutConflictStrategyFlags
        {
            get
            {
                CheckoutStrategy flags = default(CheckoutStrategy);

                switch (FileConflictStrategy)
                {
                    case CheckoutFileConflictStrategy.Ours:
                        flags = CheckoutStrategy.GIT_CHECKOUT_USE_OURS;
                        break;
                    case CheckoutFileConflictStrategy.Theirs:
                        flags = CheckoutStrategy.GIT_CHECKOUT_USE_THEIRS;
                        break;
                    case CheckoutFileConflictStrategy.Merge:
                        flags = CheckoutStrategy.GIT_CHECKOUT_CONFLICT_STYLE_MERGE;
                        break;
                    case CheckoutFileConflictStrategy.Diff3:
                        flags = CheckoutStrategy.GIT_CHECKOUT_CONFLICT_STYLE_DIFF3;
                        break;
                }

                return flags;
            }
        }
    }
}
