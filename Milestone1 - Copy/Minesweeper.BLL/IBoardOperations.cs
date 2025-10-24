using Minesweeper.Models;

namespace Minesweeper.BLL
{
    // contract the console app and tests use
    public interface IBoardOperations
    {
        void SetupBombs(BoardModel board);
        void CountBombsNearby(BoardModel board);

        bool VisitCell(BoardModel board, int r, int c);
        void ToggleFlag(BoardModel board, int r, int c);
        string UseRewardPeek(BoardModel board, int r, int c);
        GameState DetermineGameState(BoardModel board);
    }
}
