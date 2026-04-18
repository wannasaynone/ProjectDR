using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectDR.Village;

namespace ProjectDR.Tests.Village
{
    /// <summary>
    /// GuardReturnConfig 的單元測試（B10）。
    /// 驗證 JSON DTO 反序列化、依 sequence 排序、依 phase 查詢。
    /// </summary>
    [TestFixture]
    public class GuardReturnConfigTests
    {
        [Test]
        public void Constructor_NullData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GuardReturnConfig(null));
        }

        [Test]
        public void Constructor_EmptyData_ProducesNoLines()
        {
            GuardReturnConfig sut = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[0],
            });
            Assert.AreEqual(0, sut.OrderedLines.Count);
            Assert.AreEqual(0, sut.GetAllLineTexts().Length);
        }

        [Test]
        public void Constructor_SortsLinesBySequence()
        {
            GuardReturnConfig sut = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { line_id = "l3", sequence = 3, text = "third", phase_id = GuardReturnPhaseIds.Sheathe },
                    new GuardReturnLineData { line_id = "l1", sequence = 1, text = "first", phase_id = GuardReturnPhaseIds.Alert },
                    new GuardReturnLineData { line_id = "l2", sequence = 2, text = "second", phase_id = GuardReturnPhaseIds.Clarify },
                },
            });
            Assert.AreEqual(3, sut.OrderedLines.Count);
            Assert.AreEqual("first", sut.OrderedLines[0].text);
            Assert.AreEqual("second", sut.OrderedLines[1].text);
            Assert.AreEqual("third", sut.OrderedLines[2].text);
        }

        [Test]
        public void GetAllLineTexts_ReturnsTextsInOrder()
        {
            GuardReturnConfig sut = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { sequence = 1, text = "a" },
                    new GuardReturnLineData { sequence = 2, text = "b" },
                },
            });
            Assert.AreEqual(new[] { "a", "b" }, sut.GetAllLineTexts());
        }

        [Test]
        public void GetLinesByPhase_ReturnsOnlyMatchingPhase()
        {
            GuardReturnConfig sut = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { sequence = 1, text = "a1", phase_id = GuardReturnPhaseIds.Alert },
                    new GuardReturnLineData { sequence = 2, text = "a2", phase_id = GuardReturnPhaseIds.Alert },
                    new GuardReturnLineData { sequence = 3, text = "c1", phase_id = GuardReturnPhaseIds.Clarify },
                },
            });
            IReadOnlyList<GuardReturnLineData> alertLines = sut.GetLinesByPhase(GuardReturnPhaseIds.Alert);
            Assert.AreEqual(2, alertLines.Count);
            Assert.AreEqual("a1", alertLines[0].text);
            Assert.AreEqual("a2", alertLines[1].text);
        }

        [Test]
        public void GetLinesByPhase_UnknownPhase_ReturnsEmpty()
        {
            GuardReturnConfig sut = new GuardReturnConfig(new GuardReturnConfigData
            {
                guard_return_lines = new GuardReturnLineData[]
                {
                    new GuardReturnLineData { sequence = 1, text = "x", phase_id = GuardReturnPhaseIds.Alert },
                },
            });
            Assert.AreEqual(0, sut.GetLinesByPhase("unknown").Count);
            Assert.AreEqual(0, sut.GetLinesByPhase(null).Count);
            Assert.AreEqual(0, sut.GetLinesByPhase("").Count);
        }
    }
}
