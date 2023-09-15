
namespace ChessChallenge.API
{
    public interface IChessBot
    {
        Move Think(Board board, Timer timer);
        //void ToggleBitboard(Board board);
        //void GenerateBitboard(Board board);
    }
}
