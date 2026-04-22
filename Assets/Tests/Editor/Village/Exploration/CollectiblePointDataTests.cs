using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village.Exploration;
using ProjectDR.Village.Exploration.Collection;

namespace ProjectDR.Tests.Village.Exploration
{
    /// <summary>
    /// CollectiblePointData and CollectibleItemEntry unit tests.
    /// Covers: construction validation, immutability, edge cases.
    /// </summary>
    [TestFixture]
    public class CollectiblePointDataTests
    {
        // ===== CollectibleItemEntry =====

        [Test]
        public void ItemEntry_ValidParams_StoresValues()
        {
            CollectibleItemEntry entry = new CollectibleItemEntry("Wood", 3, 2.0f);

            Assert.AreEqual("Wood", entry.ItemId);
            Assert.AreEqual(3, entry.Quantity);
            Assert.AreEqual(2.0f, entry.UnlockDurationSeconds, 0.001f);
        }

        [Test]
        public void ItemEntry_ZeroUnlockDuration_Allowed()
        {
            CollectibleItemEntry entry = new CollectibleItemEntry("Wood", 1, 0f);

            Assert.AreEqual(0f, entry.UnlockDurationSeconds, 0.001f);
        }

        [Test]
        public void ItemEntry_NullItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CollectibleItemEntry(null, 1, 0f));
        }

        [Test]
        public void ItemEntry_EmptyItemId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CollectibleItemEntry("", 1, 0f));
        }

        [Test]
        public void ItemEntry_ZeroQuantity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CollectibleItemEntry("Wood", 0, 0f));
        }

        [Test]
        public void ItemEntry_NegativeQuantity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CollectibleItemEntry("Wood", -1, 0f));
        }

        [Test]
        public void ItemEntry_NegativeUnlockDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CollectibleItemEntry("Wood", 1, -1f));
        }

        // ===== CollectiblePointData =====

        [Test]
        public void PointData_ValidParams_StoresValues()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 2, 1.0f),
                new CollectibleItemEntry("Stone", 1, 2.0f)
            };

            CollectiblePointData data = new CollectiblePointData(3, 4, 5.0f, items);

            Assert.AreEqual(3, data.X);
            Assert.AreEqual(4, data.Y);
            Assert.AreEqual(5.0f, data.GatherDurationSeconds, 0.001f);
            Assert.AreEqual(2, data.Items.Count);
            Assert.AreEqual("Wood", data.Items[0].ItemId);
            Assert.AreEqual("Stone", data.Items[1].ItemId);
        }

        [Test]
        public void PointData_ZeroGatherDuration_Allowed()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f)
            };

            CollectiblePointData data = new CollectiblePointData(0, 0, 0f, items);

            Assert.AreEqual(0f, data.GatherDurationSeconds, 0.001f);
        }

        [Test]
        public void PointData_NegativeGatherDuration_ThrowsArgumentOutOfRangeException()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f)
            };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CollectiblePointData(0, 0, -1f, items));
        }

        [Test]
        public void PointData_NullItems_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CollectiblePointData(0, 0, 0f, null));
        }

        [Test]
        public void PointData_EmptyItems_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CollectiblePointData(0, 0, 0f, new List<CollectibleItemEntry>()));
        }

        [Test]
        public void PointData_NullItemEntry_ThrowsArgumentException()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry> { null };

            Assert.Throws<ArgumentException>(() =>
                new CollectiblePointData(0, 0, 0f, items));
        }

        [Test]
        public void PointData_ItemsAreDefensivelyCopied()
        {
            List<CollectibleItemEntry> items = new List<CollectibleItemEntry>
            {
                new CollectibleItemEntry("Wood", 1, 0f)
            };

            CollectiblePointData data = new CollectiblePointData(0, 0, 0f, items);

            // Modifying original list should not affect data
            items.Add(new CollectibleItemEntry("Stone", 1, 0f));

            Assert.AreEqual(1, data.Items.Count);
        }
    }
}
