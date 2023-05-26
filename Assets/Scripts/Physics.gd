var Size = {
    X = 58,
    Y = 58,
    Z = 20
}

var Sources = {}
var Cells = {}
var Rods = {}

var MaxNeutrons = 10^6
var StartupNeutrons = 100

var NeutronTransferIterations = 5

var NeutronsLastStep = 0
var NeutronsThisStep = 0

func _ready():
    for X in Size.X:
        for Y in Size.Y:
            for Z in Size.Z:
                Cells[X][Y][Z] = {
                    Neutrons = StartupNeutrons if Sources.has(X*Y*Z) else 0
                }
                


func _physics_process(_delta):
    return

func TransferNeutrons():
    for Iteration in NeutronTransferIterations:
        for X in Size.X:
            for Y in Size.Y:
                for Z in Size.Z:
                    var Neighbors = GetNeighbors(X, Y, Z)

                    var Cell = Cells[X][Y][Z]
                    var ZPosition = (Z / Size.Z) * 100
                    var TransferedNeutrons = Cell.Neutrons * 0.8

                    var CRCoefficient = 0

                    if abs(ZPosition - Rods[X][Y]) < Size.Z:
                        CRCoefficient = (ZPosition - Rods[X][Y]) / Size.Z

                    Cells[X][Y][Z].Neutrons -= TransferedNeutrons

                    for Neighbor in Neighbors:
                        if not Cells[Neighbor.X] and not Cells[Neighbor.X][Neighbor.Y] and not Cells[Neighbor.X][Neighbor.Y][Neighbor.Z]:
                            pass

                        if Neighbor.X == 0 and Neighbor.Y == 0 and abs(Neighbor.Z) == 1:
                            Cells[Neighbor.X][Neighbor.Y][Neighbor.Z].Neutrons += TransferedNeutrons / 26
                        else:
                            Cells[Neighbor.X][Neighbor.Y][Neighbor.Z].Neutrons += (TransferedNeutrons / 26) * CRCoefficient
                        

                    

func GetNeighbors(X, Y, Z):
    var Neighbors = []

    for I in [-1, 0, 1]:
        for J in [-1, 0, 1]:
            for K in [-1, 0, 1]:
                if I == 0 or J == 0 or K == 0:
                    pass
                Neighbors.append({ X = X + I, Y = Y + J, Z = Z + K })

    return Neighbors
                
                