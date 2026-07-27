using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public abstract class VirtualAnimatorCopyEngineTestFixtureBase
    {
        protected CloneContext CloneContext { get; private set; }

        [SetUp]
        public void BaseSetUp()
        {
            CloneContext = new CloneContext(GenericPlatformAnimatorBindings.Instance);
        }
    }
}
