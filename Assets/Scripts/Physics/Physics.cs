using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class Physics : Node
{
    public class Rod
    {
        public int Position = 0;
    }

    public class Cell
    {
        public int X;
        public int Y;
        public int Z;

        public double ThermalNeutrons = 0;
        public double FastNeutrons = 0;

        public double Xe135Concentration = Util.GetRandomDouble(5, 30);
        public double I135Concentration = Util.GetRandomDouble(5, 30);
        public double Pu235Concentration = 0;
        public double RemainingU235 = 100;
    }

    private static PhysicsUtil Util = new PhysicsUtil();

    public static int MaxNeutrons = 10 ^ 6;
    public static int StartupNeutrons = 1000;
    public static int NeutronTransferIterations = 10;

    public static double FissionByproductRate = 0.05;
    public static double FertileCaptureRate = 0.05;
    public static double XenonDecayRate = 0.05;
    public static double U235BurnupRate = 0.005;

    public static Vector3I Size = new Vector3I(15, 15, 10);

    public Rod[,] Rods = new Rod[Size.X, Size.Y];
    public Cell[,,] Cells = new Cell[Size.X, Size.Y, Size.Z];

    // TODO: Configure realistic startup sources location

    // SYNTAX: X * Y * Z
    public int[] StartupSources = { 5 * 5 * 10, 10 * 10 * 10 };

    public double NeutronsLastStep = 0;
    public double NeutronsThisStep = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Parallel.For(1, Size.X, (int X) =>
        {
            Parallel.For(1, Size.Y, (int Y) =>
            {
                Rods[X, Y] = new Rod();
                Parallel.For(1, Size.Z, (int Z) =>
                {
                    Cells[X, Y, Z] = new Cell();
                });
            });
        });
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        // Per-Cell Operations (Parallel)
        Parallel.For(1, Size.X, (int X) =>
        {
            Parallel.For(1, Size.Y, (int Y) =>
            {
                Parallel.For(1, Size.Z, (int Z) =>
                {
                    ref Cell Cell = ref Cells[X, Y, Z];

                    Cell.FastNeutrons += StartupSources.Contains(X * Y * Z) ? StartupNeutrons : 0;

                    Cell.FastNeutrons *= 3 * (1 + (Cell.Pu235Concentration / 100));
                    Cell.ThermalNeutrons *= 1.06;

                    Cell.RemainingU235 -= (Cell.ThermalNeutrons / MaxNeutrons) * U235BurnupRate;
                    Cell.Xe135Concentration += (Cell.I135Concentration / 100) - ((Cell.ThermalNeutrons / MaxNeutrons) * XenonDecayRate);
                    Cell.I135Concentration += (Cell.ThermalNeutrons / MaxNeutrons) * FissionByproductRate;
                    Cell.Pu235Concentration += (Cell.ThermalNeutrons / MaxNeutrons) * FertileCaptureRate;

                    // Clamping
                    Cell.RemainingU235 = Mathf.Clamp(Cell.RemainingU235, 0, 100);
                    Cell.Xe135Concentration = Mathf.Clamp(Cell.Xe135Concentration, 0, 100);
                    Cell.I135Concentration = Mathf.Clamp(Cell.I135Concentration, 0, 100);
                    Cell.Pu235Concentration = Mathf.Clamp(Cell.Pu235Concentration, 0, 100);

                });
            });
        });

        // Neutron Transfer (per 26 neighbors for all cells, non-parallel)
        for (int I = 1; I < NeutronTransferIterations; I++)
        {
            for (int X = 1; X < Size.X; X++)
            {
                for (int Y = 1; X < Size.Y; Y++)
                {
                    for (int Z = 1; X < Size.Z; Z++)
                    {
                        ref Cell Cell = ref Cells[X, Y, Z];
                        Vector3I[] NeighborCoords = Util.GetCellNeighborsCoords(X, Y, Z);

                        var ZPosition = (Cell.Z / Size.Z) * 100;
                        var Rod = Rods[X, Y];
                        var CRCoefficient = 0;

                        if (Mathf.Abs(ZPosition - Rod.Position) < Size.Z)
                            CRCoefficient = (ZPosition - Rod.Position) / Size.Z;

                        var TransferedThermalNeutrons = Cell.ThermalNeutrons * 0.8 + Cell.FastNeutrons * 0.7;
                        var TransferedFastNeutrons = Cell.FastNeutrons * 0.3;


                        Cell.ThermalNeutrons -= Mathf.Clamp(Cell.ThermalNeutrons - TransferedThermalNeutrons, 0, (double)Mathf.Inf);
                        Cell.FastNeutrons -= Mathf.Clamp(Cell.FastNeutrons - TransferedFastNeutrons, 0, (double)Mathf.Inf);

                        foreach (var NeighborCoord in NeighborCoords)
                        {
                            ref Cell Neighbor = ref Cells[NeighborCoord.X, NeighborCoord.Y, NeighborCoord.Z];

                            if (Neighbor == null)
                                continue;

                            Neighbor.ThermalNeutrons += TransferedThermalNeutrons * CRCoefficient / 26;
                            Neighbor.FastNeutrons += TransferedFastNeutrons * CRCoefficient / 26;
                        }
                    }
                }
            }
        }
    }
}