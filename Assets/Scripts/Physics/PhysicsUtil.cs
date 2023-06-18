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
        Vector3I[] Coords = new Vector3I[0];

        for (int I = -1; I <= 1; I++)
        {
            for (int J = -1; J <= 1; J++)
            {
                for (int K = -1; K <= 1; K++)
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