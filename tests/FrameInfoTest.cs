// Copyright (c) Team CharLS.
// SPDX-License-Identifier: BSD-3-Clause

using System;
using NUnit.Framework;

namespace CharLS.Native.Test
{
    [TestFixture]
    public class FrameInfoTest
    {
        [Test]
        public void ConstructDefault()
        {
            var frameInfo = new FrameInfo(256, 1024, 8, 3);

            Assert.AreEqual(256, frameInfo.Width);
            Assert.AreEqual(1024, frameInfo.Height);
            Assert.AreEqual(8, frameInfo.BitsPerSample);
            Assert.AreEqual(3, frameInfo.ComponentCount);
        }

        [Test]
        public void EquatableSameObjects()
        {
            var a = new FrameInfo(256, 1024, 8, 3);
            var b = new FrameInfo(256, 1024, 8, 3);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals((object)b));
            Assert.AreEqual(a, b);
            Assert.AreEqual(b, a);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a == b);
        }

        [Test]
        public void EquatableDifferentObjects()
        {
            var a = new FrameInfo(256, 1024, 8, 3);
            var b = new FrameInfo(256, 1024, 8, 4);

            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a.Equals((object)b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void EquatableWithNull()
        {
            var a = new FrameInfo(256, 1024, 8, 3);

            Assert.IsFalse(a.Equals(null!));
            Assert.IsFalse(a!.Equals((object)null!));
        }

        [Test]
        public void NativeWidthTooLarge()
        {
            var frameInfoNative = new FrameInfoNative
            {
                Width = uint.MaxValue
            };

            _ = Assert.Throws<OverflowException>(() => {
                var _ = new FrameInfo(frameInfoNative);
            });
        }

        [Test]
        public void NativeHeightTooLarge()
        {
            var frameInfoNative = new FrameInfoNative {
                Height = uint.MaxValue
            };

            _ = Assert.Throws<OverflowException>(() => {
                var _ = new FrameInfo(frameInfoNative);
            });
        }
    }
}