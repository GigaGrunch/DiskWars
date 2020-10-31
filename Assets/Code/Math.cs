﻿using UnityEngine;

namespace DiskWars
{
    public static class Math
    {
        public static float Square(float a) => a * a;

        public static float SquaredHorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            return Math.Square(diff.x) + Math.Square(diff.z);
        }

        public static float SquaredVerticalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            return Math.Square(diff.y);
        }
    }
}
