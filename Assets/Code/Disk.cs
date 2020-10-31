namespace DiskWars
{
    public class Disk
    {
        public int ID;
        public string Name;
        public float Diameter;
        public Position Position;
    }

    public struct DiskJson
    {
        public string name;
        public float diameter;
        public string texture;
    }

    public struct Position
    {
        public float X;
        public float Y;
        public float Z;

        public static Position operator +(Position a, Position b)
        {
            return new Position
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
                Z = a.Z + b.Z
            };
        }

        public static Position operator -(Position a, Position b)
        {
            return new Position
            {
                X = a.X - b.X,
                Y = a.Y - b.Y,
                Z = a.Z - b.Z
            };
        }

        public static float SquareDistance(Position a, Position b)
        {
            Position diff = a - b;
            return Math.Square(diff.X) + Math.Square(diff.Y) + Math.Square(diff.Z);
        }
    }
}
