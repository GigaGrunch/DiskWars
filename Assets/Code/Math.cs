using UnityEngine;

namespace DiskWars
{
    public static class Math
    {
        public static Vector3 Between(Vector3 a, Vector3 b)
        {
            return (a + b) / 2f;
        }

        public static float Square(float a) => a * a;

        public static float SquaredHorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            return Square(diff.x) + Square(diff.z);
        }

        public static float SquaredVerticalDistance(Vector3 a, Vector3 b)
        {
            Vector3 diff = a - b;
            return Square(diff.y);
        }
    }
}
