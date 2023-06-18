using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class Physics : Node
{
    public class Rod
    {
        public double Position { get; set; } = 0;
    }

    public class Cell
    {
        public double ThermalNeutrons { get; set; } = 0;
        public double FastNeutrons { get; set; } = 0;

        public double Xe135Concentration { get; set; } = Util.GetRandomDouble(5, 30);
        public double I135Concentration { get; set; } = Util.GetRandomDouble(5, 30);
        public double Pu235Concentration { get; set; } = 0;
        public double RemainingU235 { get; set; } = 100;
    }

    private static PhysicsUtil Util = new PhysicsUtil();

    public static int MaxNeutrons = 2^32;
    public static int StartupNeutrons = 1024;
    public static int NeutronTransferIterations = 4;

    public static double FissionByproductRate = 0.05;
    public static double FertileCaptureRate = 0.05;
    public static double XenonDecayRate = 0.05;
    public static double U235BurnupRate = 0.005;

    public static Vector3I Size = new Vector3I(2, 2, 2);

    public Rod[,] Rods = new Rod[Size.X, Size.Y];
    public Cell[,,] Cells = new Cell[Size.X, Size.Y, Size.Z];

    // TODO: Configure realistic startup sources location

    // SYNTAX: X * Y * Z
    int[] StartupSources = { 5 * 5 * 10, 10 * 10 * 10 };

    double NeutronsLastStep = 0;
    double NeutronsThisStep = 0;
    double RodPositions = 0;
    double CRCoefficients = 0;

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
                ref Rod Rod = ref Rods[X, Y];

                Rod.Position += 0.05d;
                Rod.Position = Mathf.Clamp(Rod.Position, 0d, 1d);

                RodPositions += Rod.Position;

                Parallel.For(1, Size.Z, (int Z) =>
                {
                    ref Cell Cell = ref Cells[X, Y, Z];

                    NeutronsLastStep += Cell.ThermalNeutrons;

                    Cell.FastNeutrons += StartupNeutrons;
                    Cell.ThermalNeutrons += StartupNeutrons / 100;

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

                    NeutronsThisStep += Cell.ThermalNeutrons;
                });
            });
        });

        // Neutron Transfer (per 26 neighbors for all cells, non-parallel)
        for (int I = 1; I < NeutronTransferIterations; I++)
        {
            for (int X = 1; X < Size.X; X++)
            {
                for (int Y = 1; Y < Size.Y; Y++)
                {
                    ref Rod Rod = ref Rods[X, Y];

                    for (int Z = 1; Z < Size.Z; Z++)
                    {
                        ref Cell Cell = ref Cells[X, Y, Z];

                        double ZPosition = (double)(Z / Size.Z) * 100d;

                        double CRCoefficient = 0;
                        double AbsorberCoefficient = ((100 - Cell.Xe135Concentration) / 500) * ((100 - Cell.I135Concentration) / 100);

                        /*                         if (ZPosition - Rod.Position > Size.Z)
                                                    CRCoefficient = (ZPosition - Rod.Position) / Size.Z; */
                        CRCoefficient = Rod.Position;
                        CRCoefficients += CRCoefficient;

                        double TransferedThermalNeutrons = (Cell.ThermalNeutrons * 0.8);
                        double TransferedFastNeutrons = Cell.FastNeutrons * 0.1;

                        Cell.ThermalNeutrons = Mathf.Clamp(Cell.ThermalNeutrons - TransferedThermalNeutrons, 0, (double)Mathf.Inf);
                        Cell.FastNeutrons = Mathf.Clamp(Cell.FastNeutrons - TransferedFastNeutrons, 0, (double)Mathf.Inf);

                        foreach (Vector3I NeighborCoord in Util.GetCellNeighborsCoords(X, Y, Z))
                        {
                            ref Cell Neighbor = ref Cells[NeighborCoord.X, NeighborCoord.Y, NeighborCoord.Z];

                            if (Neighbor == null)
                                continue;

                            Neighbor.ThermalNeutrons += (TransferedThermalNeutrons + (TransferedFastNeutrons * 0.9) * CRCoefficient * AbsorberCoefficient) / 26;
                            Neighbor.FastNeutrons += TransferedFastNeutrons * CRCoefficient * AbsorberCoefficient / 26;
                        }
                    }
                }
            }
        }

/*         double AverageNeutronsThisStep = NeutronsThisStep / (Size.X * Size.Y * Size.Z);
        double AverageNeutronsLastStep = NeutronsLastStep / (Size.X * Size.Y * Size.Z);
        double AverageCRCoefficient = CRCoefficients / (Size.X * Size.Y * Size.Z);
        double AverageRodPosition = RodPositions / (Size.X * Size.Y);

        double Period = 1 / Mathf.Log(NeutronsThisStep / NeutronsLastStep);

        double APRM = (NeutronsThisStep / (Size.X * Size.Y * Size.Z)) / MaxNeutrons * 100;

        string FormattedAPRM = String.Format("{0:#,0.0}", APRM) + "%";
        string FormattedPeriod = String.Format("{0:#,0.000}", Period) + "s";
        string FormattedARP = String.Format("{0:#,0.0}", AverageRodPosition) + "%";
        string FormattedCRCoef = String.Format("{0:#,0.00}", AverageCRCoefficient / NeutronTransferIterations);

        if (Mathf.IsNaN(Period) || Mathf.IsInf(Period))
            FormattedPeriod = "INF";

        GD.Print("APRM: ", FormattedAPRM, " / PERIOD:", FormattedPeriod, " / RODS:", FormattedARP, " / CR COEFF:", FormattedCRCoef); */

        NeutronsThisStep = 0;
        NeutronsLastStep = 0;
    }
}