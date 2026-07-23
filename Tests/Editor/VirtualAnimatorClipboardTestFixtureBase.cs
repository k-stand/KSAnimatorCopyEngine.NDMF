using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public abstract class VirtualAnimatorClipboardTestFixtureBase
    {
        protected CloneContext CloneContext { get; private set; }

        [SetUp]
        public void BaseSetUp()
        {
            CloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);
        }
    }
}
