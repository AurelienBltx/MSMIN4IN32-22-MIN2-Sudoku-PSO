using Sudoku.shared;

namespace Sudoku.DlxLib
{
    public class DlxLibSolver : ISudokuSolver
    {
        public SudokuGrid Solve(SudokuGrid s)
        {
            return s.CloneSudoku();
        }
    }
}