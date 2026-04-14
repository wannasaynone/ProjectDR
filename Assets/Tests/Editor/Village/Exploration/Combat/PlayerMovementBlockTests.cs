using System.Collections.Generic;
using KahaGameCore.GameEvent;
using NUnit.Framework;
using UnityEngine;
using ProjectDR.Village.Exploration;

namespace ProjectDR.Tests.Village.Exploration.Combat
{
    /// <summary>
    /// Tests for GDD rule 47: visible monsters on explored cells block player movement.
    /// Tests the SetAdditionalBlockCheck extension on PlayerGridMovement.
    /// </summary>
    [TestFixture]
    public class PlayerMovementBlockTests
    {
        private static CellType[] CreateAllExplorableCells(int w, int h)
        {
            CellType[] cells = new CellType[w * h];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = CellType.Explorable;
            return cells;
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ForceClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ForceClearAll();
        }

        [Test]
        public void TryMove_BlockedByAdditionalCheck_ReturnsFalse()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);
            gridMap.InitializeExplored(2, -1);

            var speedCalc = new FixedMoveSpeedCalculator(0.5f);
            var sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), speedCalc);

            // Block cell (3, 2) - to the right
            sut.SetAdditionalBlockCheck((x, y) => x == 3 && y == 2);

            bool result = sut.TryMove(MoveDirection.Right);

            Assert.IsFalse(result);
            Assert.IsFalse(sut.IsMoving);
        }

        [Test]
        public void TryMove_NotBlockedByAdditionalCheck_Succeeds()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);
            gridMap.InitializeExplored(2, -1);

            var speedCalc = new FixedMoveSpeedCalculator(0.5f);
            var sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), speedCalc);

            // Block cell (3, 2) but move left (1, 2) which is not blocked
            sut.SetAdditionalBlockCheck((x, y) => x == 3 && y == 2);

            bool result = sut.TryMove(MoveDirection.Left);

            Assert.IsTrue(result);
        }

        [Test]
        public void TryMove_NoAdditionalBlockCheck_DefaultBehavior()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);
            gridMap.InitializeExplored(2, -1);

            var speedCalc = new FixedMoveSpeedCalculator(0.5f);
            var sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), speedCalc);

            // No block check set - all walkable cells are accessible
            bool result = sut.TryMove(MoveDirection.Right);

            Assert.IsTrue(result);
        }

        [Test]
        public void SetAdditionalBlockCheck_NullRemovesCheck()
        {
            CellType[] cells = CreateAllExplorableCells(5, 5);
            MapData mapData = new MapData(5, 5, cells, new Vector2Int(2, 2), new List<List<Vector2Int>>());
            GridMap gridMap = new GridMap(mapData, null);
            gridMap.InitializeExplored(2, -1);

            var speedCalc = new FixedMoveSpeedCalculator(0.5f);
            var sut = new PlayerGridMovement(gridMap, new Vector2Int(2, 2), speedCalc);

            // Set block, then remove it
            sut.SetAdditionalBlockCheck((x, y) => x == 3 && y == 2);
            sut.SetAdditionalBlockCheck(null);

            bool result = sut.TryMove(MoveDirection.Right);

            Assert.IsTrue(result);
        }
    }
}
