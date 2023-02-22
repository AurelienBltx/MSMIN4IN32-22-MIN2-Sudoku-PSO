using Sudoku.Shared;

namespace Sudoku.PSO;

public class SolverPSO : ISudokuSolver
{
    private Random _rnd;

    public SudokuGrid Solve(SudokuGrid s)
    {
        //initialisation
        const int numOrganisms = 200;
        const int maxEpochs = 5000;
        const int maxRestarts = 20;
        //Affichage des paramËtres de base dans la console
        Console.WriteLine($"Setting numOrganisms: {numOrganisms}");
        Console.WriteLine($"Setting maxEpochs: {maxEpochs}");
        Console.WriteLine($"Setting maxRestarts: {maxRestarts}");

        //Convertir un SudokuGrid en Sudoku
        int[,] CellsSolver = new int[9,9];
        for(int i=0; i<9; i++){
            for(int j=0; j<9; j++){
                CellsSolver[i,j] = s.Cells[i][j];
            }
        }

        //CrÈation du nouveau Sudoku ‡ partir de SudokuGrid
        var sudoku = new Sudoku(CellsSolver);
        //RÈsolution
        var solvedSudoku = Solve(sudoku, numOrganisms, maxEpochs, maxRestarts);

        //Convertir un Sudoku en SudokuGrid
        for(int i=0; i<9; i++){
            for(int j=0; j<9; j++){
                s.Cells[i][j] = solvedSudoku.CellValues[i,j];
            }
        }

        //Retour d'un Sudoku au format SudokuGrid
        return s;
    }

    public Sudoku Solve(Sudoku sudoku, int numOrganisms, int maxEpochs, int maxRestarts)
    {
        var error = int.MaxValue;
        Sudoku bestSolution = null;
        var attempt = 0;
        while (error != 0 && attempt < maxRestarts)//Continuer temps que le nombre d'essais max n'est pas atteint
        {
            Console.WriteLine($"Attempt: {attempt}");//Affichage du numÈro de l'essai dans la console
            _rnd = new Random(attempt);
            bestSolution = SolveInternal(sudoku, numOrganisms, maxEpochs);
            error = bestSolution.Error;
            ++attempt;
        }

        return bestSolution;
    }

    private Sudoku SolveInternal(Sudoku sudoku, int numOrganisms, int maxEpochs)
    {
        //Initialisation
        var numberOfWorkers = (int)(numOrganisms * 0.90);
        var hive = new Organism[numOrganisms];

        var bestError = int.MaxValue;
        Sudoku bestSolution = null;

        for (var i = 0; i < numOrganisms; ++i)//Pour chaque organisme
        {
            var organismType = i < numberOfWorkers
              ? OrganismType.Worker
              : OrganismType.Explorer;

            var randomSudoku = Sudoku.New(MatrixHelper.RandomMatrix(_rnd, sudoku.CellValues));//Remplis la grille de sudoku de maniËre alÈatoire
            var err = randomSudoku.Error;//Calcule le nombre d'erreure sur cette grille

            hive[i] = new Organism(organismType, randomSudoku.CellValues, err, 0);

            if (err >= bestError) continue;//Si err est supÈrieur ou Ègale au meilleur nombre d'erreur continuer sans effectuer les 2 derniËres lignes
            bestError = err;
            bestSolution = Sudoku.New(randomSudoku);
        }

        var epoch = 0;
        while (epoch < maxEpochs)//Temps que l'Èpoque est inferieur ‡ l'Èpoque max
        {
            if (epoch % 1000 == 0)//Toutes les 1000 Èpoque afficher la meilleure erreur
                Console.WriteLine($"Epoch: {epoch}, Best error: {bestError}");

            if (bestError == 0)//Si l'erreur = 0 tout arreter
                break;

            for (var i = 0; i < numOrganisms; ++i) //Pour chaque organisme
            {
                if (hive[i].Type == OrganismType.Worker)//Si l'organisme est un worker
                {
                    var neighbor = MatrixHelper.NeighborMatrix(_rnd, sudoku.CellValues, hive[i].Matrix);
                    var neighborSudoku = Sudoku.New(neighbor);
                    var neighborError = neighborSudoku.Error;

                    var p = _rnd.NextDouble();
                    if (neighborError < hive[i].Error || p < 0.001) //Si l'ereur est meillieur ou que p est <0.001
                    {
                        hive[i].Matrix = MatrixHelper.DuplicateMatrix(neighbor); //Accept le nouveau sudoku
                        hive[i].Error = neighborError;
                        if (neighborError < hive[i].Error) hive[i].Age = 0;

                        if (neighborError >= bestError) continue;//Si l'erreu est meillieur que le sudoku de r√©f√©rence, prend sa place
                        bestError = neighborError;
                        bestSolution = neighborSudoku;
                    }
                    else //Passe √† la g√©n√©ration suivante
                    {
                        hive[i].Age++;
                        if (hive[i].Age <= 1000) continue; //Si l'age est sup√©rieur √† 1000 l'ouvrier prend un nouveau sudoku al√©atoirement g√©n√©r√©
                        var randomSudoku = Sudoku.New(MatrixHelper.RandomMatrix(_rnd, sudoku.CellValues));
                        hive[i] = new Organism(0, randomSudoku.CellValues, randomSudoku.Error, 0);
                    }
                }
                else//Si l'organism est un Explorer
                {
                    var randomSudoku = Sudoku.New(MatrixHelper.RandomMatrix(_rnd, sudoku.CellValues));//Cr√©√© un nouveau sudoku al√©atoirement g√©n√©r√©
                    hive[i].Matrix = MatrixHelper.DuplicateMatrix(randomSudoku.CellValues);
                    hive[i].Error = randomSudoku.Error;//Calcule son erreur

                    if (hive[i].Error >= bestError) continue;//Si elle est meilleur que celui de r√©f√©rence prend sa place
                    bestError = hive[i].Error;
                    bestSolution = randomSudoku;
                }
            }

            // merge best worker with best explorer into worst worker
            var bestWorkerIndex = 0;
            var smallestWorkerError = hive[0].Error;
            for (var i = 0; i < numberOfWorkers; ++i) //Pour chaque travailleur
            {
                if (hive[i].Error >= smallestWorkerError) continue; //Trouver le meilleur travailleur
                smallestWorkerError = hive[i].Error;
                bestWorkerIndex = i;
            }

            var bestExplorerIndex = numberOfWorkers;
            var smallestExplorerError = hive[numberOfWorkers].Error;
            for (var i = numberOfWorkers; i < numOrganisms; ++i) //Pour chaque exploreur
            {
                if (hive[i].Error >= smallestExplorerError) continue; //Trouver le meilleur exploreur
                smallestExplorerError = hive[i].Error;
                bestExplorerIndex = i;
            }

            var worstWorkerIndex = 0;
            var largestWorkerError = hive[0].Error;
            for (var i = 0; i < numberOfWorkers; ++i) //Trouver chaque travilleur
            {
                if (hive[i].Error <= largestWorkerError) continue; //Trouver le pire travilleur
                largestWorkerError = hive[i].Error;
                worstWorkerIndex = i;
            }

            //Merger le meilleur travailleur et le meilleur exploreur
            var merged = MatrixHelper.MergeMatrices(_rnd, hive[bestWorkerIndex].Matrix, hive[bestExplorerIndex].Matrix);
            var mergedSudoku = Sudoku.New(merged);
            //Attribuer cette nouvelle grille au pire travailleur
            hive[worstWorkerIndex] = new Organism(0, merged, mergedSudoku.Error, 0);
            if (hive[worstWorkerIndex].Error < bestError)//Regarder si cette nouvelle grille est la meilleur solution
            {
                bestError = hive[worstWorkerIndex].Error;
                bestSolution = mergedSudoku;
            }

            ++epoch;
        }

        return bestSolution;
    }

}

