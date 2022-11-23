from sudoku import Sudoku
import sys

if __name__ == '__main__':
    rawData = sys.argv[1]
    row = []
    table = []
    counter = 0
    for i in rawData:
        row.append(int(i))
        if (counter == 8):
            table.append(row)
            row = []
            counter = 0
        else:
            counter += 1 
    #print(table)
    board = table
    puzzle = Sudoku(3, 3, board=board)
    with open('C:/Users/Judit/Desktop/SudokuSolver/inputSudoku.txt', 'w') as f:
        f.write(str(puzzle))
    puzzle.solve().show()

    with open('C:/Users/Judit/Desktop/SudokuSolver/solvedSudoku.txt', 'w') as f:
        f.write(str(puzzle.solve()))
    