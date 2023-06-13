using Godot;
using System;
using System.Linq;

public partial class OldPhysics : Node
{
    public class Rod
    {
        public int X;
        public int Y;
        public int Position = 0;

        public Rod(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }

    public class Cell
    {
        public int X;
        public int Y;
        public int Z;

        public double Neutrons = 100;
        public double Xe135Concentration = 10;
        public double I135Concentration = 10;
        public double RemainingU235 = 1000;

        public Cell(int X, int Y, int Z, double Neutrons = 100)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            this.Neutrons = Neutrons;
        }
    }

    public static double MaxNeutrons = 10 ^ 6;
    public static double StartupNeutrons = 1000;

    public static int NeutronTransferIterations = 10;

    public static double ByproductRate = 0.05;
    public static double DecayRate = 0.05;
    public static double BurnupRate = 0.005;

    public static Vector3I Size = new Vector3I(5, 5, 10);

    public Cell[] Cells = new Cell[Size.X * Size.Y * Size.Z];
    public Rod[] Rods = new Rod[Size.X * Size.Y];

    public double NeutronsLastStep = 0;
    public double NeutronsThisStep = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        for (int X = 1; X < Size.X; X++)
        {
            for (int Y = 1; X < Size.Y; Y++)
            {
                var Rod = new Rod(X, Y);
                Rods.Append(Rod);

                for (int Z = 1; X < Size.Z; Z++)
                {
                    var Cell = new Cell(X, Y, Z, 0);
                    Cells.Append(Cell);
                }
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        for (int Iteration = 1; Iteration < NeutronTransferIterations; Iteration++)
        {
            foreach (var Cell in Cells)
            {
                var ZPosition = (Cell.Z / Size.Z) * 100;
                var TransferedNeutrons = Cell.Neutrons * 0.9;
                var Neighbors = GetNeighbors(Cell.X, Cell.Y, Cell.Z);

                var CRCoefficient = 0;
                var Rod = Array.Find(Rods, Rod => Rod.X == Cell.X && Rod.Y == Cell.Y);

                if (Mathf.Abs(ZPosition - Rod.Position) < Size.Z)
                    CRCoefficient = (ZPosition - Rod.Position) / Size.Z;

                Cell.Neutrons -= Mathf.Clamp(Cell.Neutrons - TransferedNeutrons, 0d, (double)Mathf.Inf);

                foreach (var Neighbor in Neighbors)
                {
                    
                }
            }
        }

        foreach (var Cell in Cells)
        {
            Cell.Neutrons *= 3d;
            GD.Print(Cell.Neutrons / MaxNeutrons * 100);
        }
    }

    public Cell[] GetNeighbors(int X, int Y, int Z)
    {
        Cell[] Neighbors = new Cell[26];
        int[] Offsets = { -1, 0, 1 };

        foreach (var I in Offsets)
        {
            foreach (var J in Offsets)
            {
                foreach (var K in Offsets)
                {
                    if (I == 0 && J == 0 && K == 0) 
                        continue;
                    var Cell = Array.Find(Cells, Cell => Cell.X == X + I && Cell.Y == Y + J && Cell.Z == Z + K);

                    if (Cell == null)
                        continue;

                    Neighbors.Append(Cell);
                }
            }
        }

        return Neighbors;
    }
}
