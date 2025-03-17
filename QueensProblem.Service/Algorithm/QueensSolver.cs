using System;
using System.Collections.Generic;

namespace QueensProblem.Service.Algorithm
{
    public class QueensSolver
    {
        private Queen[] queens;
        private string[,] colorBoard;
        private int size;
        private HashSet<string> usedColors;
        private bool[] occupiedCols; // For faster lookup

        public Queen[] Solve(string[,] inputColorBoard)
        {
            colorBoard = inputColorBoard;
            size = colorBoard.GetLength(0);
            queens = new Queen[size];
            usedColors = new HashSet<string>();
            occupiedCols = new bool[size];

            return PlaceQueenByRow(0) ? queens : null;
        }

        private bool PlaceQueenByRow(int row)
        {
            if (row == size)
                return true; // All queens placed

            // Iterate only through available columns in this row
            for (int col = 0; col < size; col++)
            {
                string cellColor = colorBoard[row, col];

                // Skip if the color is already used or the column is occupied
                if (usedColors.Contains(cellColor) || occupiedCols[col])
                    continue;

                // Check immediate diagonal conflicts.
                // Since we place one queen per row in order,
                // only the queen in the previous row can be immediately diagonal.
                if (row > 0)
                {
                    // Check the queen in the previous row
                    Queen prevQueen = queens[row - 1];
                    if (prevQueen != null && Math.Abs(prevQueen.Col - col) == 1)
                        continue;
                }

                // Place queen at (row, col)
                queens[row] = new Queen(row, col);
                usedColors.Add(cellColor);
                occupiedCols[col] = true;

                if (PlaceQueenByRow(row + 1))
                    return true;

                // Backtrack
                usedColors.Remove(cellColor);
                occupiedCols[col] = false;
            }
            return false;
        }
    }

}
