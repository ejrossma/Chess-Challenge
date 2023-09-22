public class ScrappedFeatures
{
    //testing & debugging
    //ulong myAttackBitboard = 0;
    //bool bitboardOn = false;

    //public void GenerateAttacks(Board board, Move moveToPlay)
    //{
    //    board.MakeMove(moveToPlay);
    //    board.ForceSkipTurn();
    //    GenerateBitboard(board);
    //    board.ForceSkipTurn();
    //    board.UndoMove(moveToPlay);
    //}

    //public void GenerateBitboard(Board board)
    //{
    //    myAttackBitboard = 0;
    //    foreach (Move move in board.GetLegalMoves())
    //    {
    //        myAttackBitboard = myAttackBitboard | BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
    //    }
    //    if (bitboardOn)
    //        BitboardHelper.VisualizeBitboard(myAttackBitboard);
    //}

    //public void ToggleBitboard(Board board)
    //{
    //    if (bitboardOn)
    //    {
    //        BitboardHelper.StopVisualizingBitboard();
    //        bitboardOn = false;
    //    }
    //    else
    //    {
    //        BitboardHelper.VisualizeBitboard(myAttackBitboard);
    //        bitboardOn = true;
    //    }
    //}
}
