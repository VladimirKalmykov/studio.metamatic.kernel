/*
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Metamatic.Pathfinder
{
    public class Pathfinder2DQuadTreeTests
    {
        private Quadtree quadtree;
        private Rect bounds;

        [SetUp]
        public void SetUp()
        {
            // Определяем границы квадродерева
            bounds = new Rect(-100, -100, 200, 200);
            quadtree = new Quadtree(bounds);
        }

        [Test]
        public void TestInsertSingleWaypoint()
        {
            // Arrange
            Waypoint2D waypoint = new Waypoint2D(new Vector2(0, 0));

            // Act
            quadtree.Insert(waypoint);
            List<Waypoint2D> results = quadtree.QueryRange(bounds);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(waypoint, results[0]);
        }

        [Test]
        public void TestInsertMultipleWaypoints()
        {
            // Arrange
            Waypoint2D waypoint1 = new Waypoint2D(new Vector2(-10, -10));
            Waypoint2D waypoint2 = new Waypoint2D(new Vector2(10, 10));
            Waypoint2D waypoint3 = new Waypoint2D(new Vector2(50, 50));

            // Act
            quadtree.Insert(waypoint1);
            quadtree.Insert(waypoint2);
            quadtree.Insert(waypoint3);
            List<Waypoint2D> results = quadtree.QueryRange(bounds);

            // Assert
            Assert.AreEqual(3, results.Count);
            Assert.Contains(waypoint1, results);
            Assert.Contains(waypoint2, results);
            Assert.Contains(waypoint3, results);
        }

        [Test]
        public void TestQueryRangeWithinBounds()
        {
            // Arrange
            Waypoint2D waypoint1 = new Waypoint2D(new Vector2(-10, -10));
            Waypoint2D waypoint2 = new Waypoint2D(new Vector2(10, 10));
            Waypoint2D waypoint3 = new Waypoint2D(new Vector2(50, 50));
            quadtree.Insert(waypoint1);
            quadtree.Insert(waypoint2);
            quadtree.Insert(waypoint3);

            Rect queryBounds = new Rect(-20, -20, 40, 40);

            // Act
            List<Waypoint2D> results = quadtree.QueryRange(queryBounds);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.Contains(waypoint1, results);
            Assert.Contains(waypoint2, results);
            Assert.IsFalse(results.Contains(waypoint3));
        }

        [Test]
        public void TestQueryRangeOutsideBounds()
        {
            // Arrange
            Waypoint2D waypoint = new Waypoint2D(new Vector2(150, 150));
            quadtree.Insert(waypoint);

            Rect queryBounds = new Rect(-20, -20, 40, 40);

            // Act
            List<Waypoint2D> results = quadtree.QueryRange(queryBounds);

            // Assert
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void TestQueryCircle()
        {
            // Arrange
            Waypoint2D waypoint1 = new Waypoint2D(new Vector2(0, 0));
            Waypoint2D waypoint2 = new Waypoint2D(new Vector2(5, 0));
            Waypoint2D waypoint3 = new Waypoint2D(new Vector2(10, 0));
            quadtree.Insert(waypoint1);
            quadtree.Insert(waypoint2);
            quadtree.Insert(waypoint3);

            Vector2 center = new Vector2(0, 0);
            float radius = 6f;

            // Act
            List<Waypoint2D> results = quadtree.QueryCircle(center, radius);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.Contains(waypoint1, results);
            Assert.Contains(waypoint2, results);
            Assert.IsFalse(results.Contains(waypoint3));
        }

        [Test]
        public void TestInsertWaypointOutsideBounds()
        {
            // Arrange
            Waypoint2D waypoint = new Waypoint2D(new Vector2(250, 250));

            // Act
            quadtree.Insert(waypoint);
            List<Waypoint2D> results = quadtree.QueryRange(bounds);

            // Assert
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void TestQuadtreeSubdivision()
        {
            // Arrange
            List<Waypoint2D> waypoints = new List<Waypoint2D>();
            for (int i = 0; i < 10; i++)
            {
                Waypoint2D waypoint = new Waypoint2D(new Vector2(i * 10, i * 10));
                waypoints.Add(waypoint);
                quadtree.Insert(waypoint);
            }

            // Act
            List<Waypoint2D> results = quadtree.QueryRange(bounds);

            // Assert
            Assert.AreEqual(10, results.Count);
            foreach (var waypoint in waypoints)
            {
                Assert.Contains(waypoint, results);
            }
        }

        [Test]
        public void TestQueryCircleEdgeCase()
        {
            // Arrange
            Waypoint2D waypoint = new Waypoint2D(new Vector2(5, 0));
            quadtree.Insert(waypoint);

            Vector2 center = new Vector2(0, 0);
            float radius = 5f;

            // Act
            List<Waypoint2D> results = quadtree.QueryCircle(center, radius);

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.Contains(waypoint, results);
        }

        [Test]
        public void TestEmptyQuadtreeQuery()
        {
            // Arrange
            Rect queryBounds = new Rect(-10, -10, 20, 20);

            // Act
            List<Waypoint2D> results = quadtree.QueryRange(queryBounds);

            // Assert
            Assert.AreEqual(0, results.Count);
        }
    }
}
*/