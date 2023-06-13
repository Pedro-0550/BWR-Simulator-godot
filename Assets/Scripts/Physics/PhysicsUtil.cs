using System;
using System.Linq;
using Godot;

public class PhysicsUtil
{
    private Random RNG = new Random();
    public double GetRandomDouble(double Min, double Max)
    {
        return RNG.NextDouble() * (Max - Min) + Max;
    }

    public Vector3I[] GetCellNeighborsCoords(int X, int Y, int Z)
    {
        Vector3I[] Coords = new Vector3I[26];
        int[] Offsets = { -1, 0, 1 };

        foreach (var I in Offsets)
        {
            foreach (var J in Offsets)
            {
                foreach (var K in Offsets)
                {
                    if (I == 0 && J == 0 && K == 0)
                        continue;

                    Coords.Append(new Vector3I(X + I, Y + J, Z + K));
                }
            }
        }

        return Coords;
    }

}