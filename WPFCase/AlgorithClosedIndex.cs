using System;
using System.Collections.Generic;
using System.Linq;
using BestDelivery;

namespace WPFCase
{
    internal class AlgorithClosedIndex
    {
        public static int[] BuildRoute(BestDelivery.Point depot, Order[] orders, string algorithm)
        {
            return algorithm switch
            {
                "Nearest Neighbor" => NearestNeighbor(depot, orders),
                "Random" => RandomRoute(orders),
                _ => NearestNeighbor(depot, orders),
            };
        }

        private static int[] NearestNeighbor(BestDelivery.Point depot, Order[] orders)
        {
            var route = new List<int> { -1 };
            var visited = new HashSet<int>();
            BestDelivery.Point current = depot;

            while (visited.Count < orders.Length)
            {
                int index = GetClosestOrderIndex(current, orders, visited);
                if (index == -1) break;

                visited.Add(orders[index].ID);
                route.Add(orders[index].ID);
                current = orders[index].Destination;
            }

            route.Add(-1);
            return route.ToArray();
        }

        private static int[] RandomRoute(Order[] orders)
        {
            var rnd = new Random();
            var shuffled = orders.OrderBy(x => rnd.Next()).Select(o => o.ID).ToList();
            shuffled.Insert(0, -1);
            shuffled.Add(-1);
            return shuffled.ToArray();
        }

        private static int GetClosestOrderIndex(BestDelivery.Point from, Order[] orders, HashSet<int> visited)
        {
            double minDistance = double.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < orders.Length; i++)
            {
                if (visited.Contains(orders[i].ID)) continue;
                double dist = RoutingTestLogic.CalculateDistance(from, orders[i].Destination);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
    }
}
