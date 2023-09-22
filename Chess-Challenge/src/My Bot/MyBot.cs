using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Data;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 120, 300, 310, 500, 900, 10000 };
    Move rootMove;

    public Move Think(Board board, Timer timer)
    {
        Negamax(5, 99999, -99999);
        return rootMove;

        //functions inside of Think so don't need to reference board again to save tokens
        long Negamax(int remainingDepth, long alpha, long beta)
        {
            if (board.IsDraw())
                return 0;
            if (board.IsInCheckmate())
                return 90000;

            //more moves = more ability find tactics/take control of the board
            long score = -board.GetLegalMoves().Count() + Evaluate();

            //check alpha & beta
            if (remainingDepth <= 0)
            {
                if (score < alpha)
                    alpha = score;
                if (score <= beta)
                    return beta;
            }

            //for each move in an ordered set of legal moves (ordered based on the type of capture piece, so prioritizes captures first) (if depth is 0 then only do captures)
            foreach (Move move in board.GetLegalMoves(remainingDepth <= 0).OrderByDescending(move => move.CapturePieceType))
            {
                board.MakeMove(move);
                score = -Negamax(remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);
                //check board state
                if (score < alpha)
                {
                    //store root move (current depth value is 5)
                    if (remainingDepth == 5)
                        rootMove = move;
                    alpha = score;
                    if (score <= beta)
                        break;
                }
            }

            return alpha;
        }

        long Evaluate()
        {
            long eval = 0;
            foreach (PieceList pieces in board.GetAllPieceLists())
            {
                //count of pieces in list * value of that type of piece * color of that type of piece
                eval += pieces.Count * (pieceValues[(int)pieces.TypeOfPieceInList]) * (pieces.IsWhitePieceList == board.IsWhiteToMove ? -1 : 1);
            }
            return eval;
        }
    }
}
