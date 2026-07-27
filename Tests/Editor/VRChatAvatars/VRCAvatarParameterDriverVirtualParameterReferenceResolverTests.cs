using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.vrchatavatars.editor.tests
{
    public class VRCAvatarParameterDriverVirtualParameterReferenceResolverTests
    {
        [Test]
        public void BehaviourType_IsVRCAvatarParameterDriver()
        {
            VRCAvatarParameterDriverVirtualParameterReferenceResolver resolver = new();

            Assert.AreEqual(typeof(VRCAvatarParameterDriver), resolver.BehaviourType);
        }

        [Test]
        public void GetReferencedParameterNames_ReturnsNameOnly_ForSetChangeType()
        {
            VRCAvatarParameterDriver driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            try
            {
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
                {
                    new() { name = "Foo", type = VRC_AvatarParameterDriver.ChangeType.Set },
                };

                VRCAvatarParameterDriverVirtualParameterReferenceResolver resolver = new();
                string[] result = resolver.GetReferencedParameterNames(driver).ToArray();

                CollectionAssert.AreEquivalent(new[] { "Foo" }, result);
            }
            finally
            {
                Object.DestroyImmediate(driver);
            }
        }

        [Test]
        public void GetReferencedParameterNames_ReturnsNameAndSource_ForCopyChangeType()
        {
            VRCAvatarParameterDriver driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            try
            {
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
                {
                    new() { name = "Dest", source = "Src", type = VRC_AvatarParameterDriver.ChangeType.Copy },
                };

                VRCAvatarParameterDriverVirtualParameterReferenceResolver resolver = new();
                string[] result = resolver.GetReferencedParameterNames(driver).ToArray();

                CollectionAssert.AreEquivalent(new[] { "Dest", "Src" }, result);
            }
            finally
            {
                Object.DestroyImmediate(driver);
            }
        }

        [Test]
        public void GetReferencedParameterNames_ExcludesEmptyNames()
        {
            VRCAvatarParameterDriver driver = ScriptableObject.CreateInstance<VRCAvatarParameterDriver>();
            try
            {
                driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
                {
                    new() { name = "", source = "", type = VRC_AvatarParameterDriver.ChangeType.Copy },
                };

                VRCAvatarParameterDriverVirtualParameterReferenceResolver resolver = new();
                string[] result = resolver.GetReferencedParameterNames(driver).ToArray();

                Assert.IsEmpty(result);
            }
            finally
            {
                Object.DestroyImmediate(driver);
            }
        }
    }
}
