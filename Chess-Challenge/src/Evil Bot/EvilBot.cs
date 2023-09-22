using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        Move rootMove;

        public Move Think(Board board, Timer _)
        {

            negamax(5, 32_000, -32_000);
            return rootMove;

            long negamax(int remainingDepth, long alpha, long beta)
            {
                if (board.IsDraw()) return 0;
                if (board.IsInCheckmate()) return 30_000;

                long score = -board.GetLegalMoves().Count();
                foreach (var pieceList in board.GetAllPieceLists())
                    score += pieceList.Count * (1013860005146900480 >> 10 * (int)pieceList.TypeOfPieceInList & 0x3ff)
                                             * (pieceList.IsWhitePieceList == board.IsWhiteToMove ? -1 : 1);

                //
                // long score = board.IsInCheckmate() ? 30_000 /*+ remainingDepth*/ : board.GetAllPieceLists().Select(pieceList => 
                //             // same as new int[]{ 0, 120, 300, 310, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                //             pieceList.Count * (1013860005146900480 >> 10 * (int)pieceList.TypeOfPieceInList & 0x3ff)
                //                             * (pieceList.IsWhitePieceList == board.IsWhiteToMove ? -1 : 1)).Sum()
                //         - board.GetLegalMoves().Count();
                //

                if (remainingDepth <= 0)
                {
                    if (score < alpha) alpha = score;
                    if (score <= beta) return beta;
                }


                foreach (Move move in board.GetLegalMoves(remainingDepth <= 0).OrderByDescending(move => move.CapturePieceType))
                {
                    board.MakeMove(move);
                    score = -negamax(remainingDepth - 1, -beta, -alpha);
                    board.UndoMove(move);
                    if (score < alpha)
                    {
                        if (remainingDepth == 5)
                            rootMove = move;
                        alpha = score;
                        if (score <= beta) break;
                    }
                }

                return alpha;
            }
        }
    }
}