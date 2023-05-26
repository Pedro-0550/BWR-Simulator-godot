class_name Physics
extends Node

var Size = {
	X = 5,
	Y = 5,
	Z = 10
}

var Sources = [5*5*10, 5*5*10, 5*5*10, 5*5*10, 5*5*10]
var Cells = {}
var Rods = {}

var MaxNeutrons = 10^5
var StartupNeutrons = 100

var NeutronTransferIterations = 5

var NeutronsLastStep = 0
var NeutronsThisStep = 0

var ByproductRate = 0.05
var DecayRate = 0.05
var BurnupRate = 0.005

func _ready():
	for X in range(1, Size.X):
		Rods[X] = {}
		Cells[X] = {}
		
		for Y in range(1, Size.Y):
			Rods[X][Y] = 0
			Cells[X][Y] = {}

			for Z in range(1, Size.Z):
				Cells[X][Y][Z] = {
					Neutrons = StartupNeutrons if Sources.has(X*Y*Z) else 0,

					Xe135Concentration = 10,
					I135Concentration = 10,

					RemainingU235 = 1000
				}
				

#TODO: Implement fuel burnup
func _physics_process(_delta):
	TransferNeutrons()

	for X in range(1, Size.X):
		for Y in range(1, Size.Y):
			for Z in range(1, Size.Z):
				Cells[X][Y][Z].Neutrons = Cells[X][Y][Z].Neutrons * 3
				Cells[X][Y][Z].Neutrons += StartupNeutrons if Sources.has(X*Y*Z) else 0
				
				Cells[X][Y][Z].I135Concentration += ((Cells[X][Y][Z].Neutrons/MaxNeutrons) * ByproductRate) - Cells[X][Y][Z].I135Concentration * DecayRate
				Cells[X][Y][Z].Xe135Concentration += Cells[X][Y][Z].I135Concentration * DecayRate

				print(Cells[X][Y][Z].Neutrons/MaxNeutrons)

#TODO: Implement xenon and iodine impact on absorption
func TransferNeutrons():
	for Iteration in NeutronTransferIterations:
		for X in range(1, Size.X):
			for Y in range(1, Size.Y):
				for Z in range(1, Size.Z):
					var Neighbors = GetNeighbors(X, Y, Z)

					var Cell = Cells[X][Y][Z]
					var ZPosition = (Z / Size.Z) * 100
					var TransferedNeutrons = Cell.Neutrons * 0.8

					var CRCoefficient = 0

					if abs(ZPosition - Rods[X][Y]) < Size.Z:
						CRCoefficient = (ZPosition - Rods[X][Y]) / Size.Z

					Cells[X][Y][Z].Neutrons -= TransferedNeutrons
					Cells[X][Y][Z].Neutrons = clamp(Cells[X][Y][Z].Neutrons, 0, MaxNeutrons^2)

					for Neighbor in Neighbors:
						if not Cells.has(Neighbor.X) or not Cells[Neighbor.X].has(Neighbor.Y) or not Cells[Neighbor.X][Neighbor.Y].has(Neighbor.Z):
							continue

						if Neighbor.X - X == 0 and Neighbor.Y - Y == 0 and abs(Neighbor.Z - Z) == 1:
							Cells[Neighbor.X][Neighbor.Y][Neighbor.Z].Neutrons += TransferedNeutrons / 26
						else:
							Cells[Neighbor.X][Neighbor.Y][Neighbor.Z].Neutrons += (TransferedNeutrons / 26) * CRCoefficient
						

					

func GetNeighbors(X, Y, Z):
	var Neighbors = []

	for I in [-1, 0, 1]:
		for J in [-1, 0, 1]:
			for K in [-1, 0, 1]:
				if I == 0 and J == 0 and K == 0:
					continue
				Neighbors.append({ X = X + I, Y = Y + J, Z = Z + K })

	return Neighbors
				
				
