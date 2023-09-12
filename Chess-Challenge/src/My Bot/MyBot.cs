using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    ulong myAttackBitboard = 0;
    bool bitboardOn = false;

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find high value capture
            int capturedPieceValue = GetPieceValue(board.GetPiece(move.TargetSquare));

            if (GetPieceValue(board.GetPiece(move.StartSquare)) > capturedPieceValue)
            {
                continue;
            }
            else if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        GenerateAttacks(board);

        return moveToPlay;
    }

    public void GenerateAttacks(Board board)
    {
        board.MakeMove(moveToPlay);
        board.ForceSkipTurn();
        GenerateBitboard(board);
        board.ForceSkipTurn();
        board.UndoMove(moveToPlay);
    }

    public void GenerateBitboard(Board board)
    {
        myAttackBitboard = 0;
        foreach (Move move in board.GetLegalMoves()) 
        {
            myAttackBitboard = myAttackBitboard | BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        }
    }

    public void ToggleBitboard(Board board)
    {
        if (bitboardOn)
        {
            BitboardHelper.StopVisualizingBitboard();
            bitboardOn = false; 
        }
        else
        {
            BitboardHelper.VisualizeBitboard(myAttackBitboard);
            bitboardOn = true;
        }
            
    }

    int GetPieceValue(Piece piece)
    {
        return pieceValues[(int)piece.PieceType];
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}