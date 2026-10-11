#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class FreezerVenusEditModeTests
{
    private GameObject root;
    private Tile tile;
    private Tilemap ground;
    private Tilemap walls;
    private FreezerVenusArena arena;

    [Test]
    public void Crown_PathUsesDistanceAndReturnsToOrigin()
    {
        Vector2[] path = { Vector2.zero, new Vector2(3f, 0f), new Vector2(3f, 4f), Vector2.zero };
        Assert.AreEqual(Vector2.zero, QueenOyobeeProjectile.PathPoint(path, 0f));
        Assert.AreEqual(new Vector2(3f, 3f), QueenOyobeeProjectile.PathPoint(path, 0.5f));
        Assert.AreEqual(Vector2.zero, QueenOyobeeProjectile.PathPoint(path, 1f));
    }

    [Test]
    public void GroundSnow_SelectsSixDistinctDestinationsWithoutPillars()
    {
        FillGround(-6, 6, -5, 5);
        for (int y = -4; y <= 4; y += 2)
            for (int x = -4; x <= 4; x += 2) walls.SetTile(new Vector3Int(x, y, 0), tile);
        var cells = QueenOyobeeProjectile.GroundSnowDestinations(arena, Vector2.zero);
        Assert.AreEqual(6, cells.Count);
        Assert.AreEqual(6, new HashSet<Vector3Int>(cells).Count);
        foreach (var cell in cells) Assert.IsTrue(arena.IsStaticWalkable(cell));
    }

    [Test]
    public void Snow_ChaseRejectsTightTurnsAndReversals()
    {
        Assert.IsTrue(QueenOyobeeProjectile.CanSnowTurn(Vector2.right, new Vector2(1f, 0.5f)));
        Assert.IsFalse(QueenOyobeeProjectile.CanSnowTurn(Vector2.right, Vector2.up));
        Assert.IsFalse(QueenOyobeeProjectile.CanSnowTurn(Vector2.right, Vector2.left));
        Assert.IsFalse(QueenOyobeeProjectile.CanSnowTurn(Vector2.right, Vector2.zero));
    }

    [Test]
    public void Crown_ExpandedRouteCoversMoreFloorAndRetainsOriginalDuration()
    {
        FillGround(-12, 12, -12, 12);
        Vector2[] path = { new Vector2(0.5f, 0.5f), new Vector2(3.5f, -2.5f), new Vector2(-2.5f, -2.5f), new Vector2(0.5f, 0.5f) };
        float seconds = QueenOyobeeProjectile.CrownFlightSeconds(path);
        Vector2[] expanded = QueenOyobeeProjectile.ExpandCrownPath(arena, path);
        Assert.AreEqual(path[0], expanded[0]);
        Assert.AreEqual(path[0], expanded[expanded.Length - 1]);
        Assert.Greater(QueenOyobeeProjectile.CrownFlightSeconds(expanded), seconds);
        Assert.Greater(Vector2.Distance(expanded[1], expanded[2]), Vector2.Distance(path[1], path[2]));
    }

    [Test]
    public void IceWave_ReachesPlayersOnlyWhenItsVisibleFrontArrives()
    {
        Assert.IsFalse(QueenOyobeeIceWave.HasReached(Vector2.zero, new Vector2(3f, 4f), 4f));
        Assert.IsTrue(QueenOyobeeIceWave.HasReached(Vector2.zero, new Vector2(3f, 4f), 5f));
        Assert.IsTrue(QueenOyobeeIceWave.HasReached(Vector2.zero, new Vector2(3f, 4f), 6f));
    }

    [SetUp]
    public void SetUp()
    {
        // Keep gameplay bootstraps inactive while exercising the real grid queries.
        root = new GameObject("FreezerVenus arena test");
        root.AddComponent<Grid>();
        var managerObject = new GameObject("Inactive manager");
        managerObject.transform.SetParent(root.transform, false);
        managerObject.SetActive(false);
        var manager = managerObject.AddComponent<GameManager>();
        var groundObject = new GameObject("Ground");
        groundObject.transform.SetParent(root.transform, false);
        ground = groundObject.AddComponent<Tilemap>();
        var wallObject = new GameObject("Indestructibles");
        wallObject.transform.SetParent(root.transform, false);
        walls = wallObject.AddComponent<Tilemap>();
        manager.groundTilemap = ground;
        manager.indestructibleTilemap = walls;
        tile = ScriptableObject.CreateInstance<Tile>();
        arena = new FreezerVenusArena(manager);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    [Test]
    public void Tornado_RoutesAroundPillarsUsingAdjacentFloorCells()
    {
        FillGround(-2, 2, -2, 2);
        walls.SetTile(Vector3Int.zero, tile);
        Vector3Int current = new(-1, 0, 0);
        Vector3Int destination = new(1, 0, 0);
        int steps = 0;
        while (current != destination && steps < 12)
        {
            Assert.IsTrue(arena.TryNextStep(current, destination, out var next));
            Assert.AreEqual(1, Mathf.Abs(next.x - current.x) + Mathf.Abs(next.y - current.y));
            Assert.IsTrue(arena.IsFloor(next));
            Assert.IsFalse(walls.HasTile(next));
            current = next;
            steps++;
        }
        Assert.AreEqual(destination, current);
        Assert.AreEqual(4, steps);
    }

    [Test]
    public void Tornado_DoesNotLeaveGroundToBypassASealedCorridor()
    {
        FillGround(-1, 1, 0, 0);
        walls.SetTile(Vector3Int.zero, tile);
        Assert.IsFalse(arena.TryNextStep(new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), out _));
    }

    [Test]
    public void Dolls_SelectFourNearestFullyOpenColumnsAndAllowTheTerminalWall()
    {
        FillGround(-7, 5, -6, 4);
        for (int x = -6; x <= 4; x += 2)
            walls.SetTile(new Vector3Int(x, -3, 0), tile);
        for (int x = -7; x <= 5; x++)
            walls.SetTile(new Vector3Int(x, -7, 0), tile);
        List<Vector3Int> cells = arena.DollSpawnCells(arena.Center(new Vector3Int(-1, 2, 0)));
        Assert.AreEqual(4, cells.Count);
        CollectionAssert.AreEquivalent(new[] { -1, -3, 1, -5 }, cells.ConvertAll(c => c.x));
        foreach (var cell in cells)
        {
            Assert.AreEqual(2, cell.y);
            for (int y = -6; y <= 4; y++)
                Assert.IsTrue(arena.IsStaticWalkable(new Vector3Int(cell.x, y, 0)));
        }
    }

    [Test]
    public void Dolls_RejectAColumnWithAnObstacleAboveTheSpawnPoint()
    {
        FillGround(-7, 5, -6, 4);
        walls.SetTile(new Vector3Int(-1, 4, 0), tile);
        var cells = arena.DollSpawnCells(arena.Center(new Vector3Int(-1, -4, 0)));
        Assert.AreEqual(4, cells.Count);
        Assert.IsFalse(cells.Exists(c => c.x == -1));
    }

    private void FillGround(int left, int right, int bottom, int top)
    {
        for (int y = bottom; y <= top; y++)
            for (int x = left; x <= right; x++)
                ground.SetTile(new Vector3Int(x, y, 0), tile);
    }
}
#endif
