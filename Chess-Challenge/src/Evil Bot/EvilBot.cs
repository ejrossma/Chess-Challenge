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
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        int negativeInfinity = -99999;
        int positiveInfinity = 99999;

        //tracking opposing pawn structure
        ulong pawnAttackBitboard;

        Move bestRootMove;
        int bestEval;

        public Move Think(Board board, Timer timer)
        {
            //get the best move
            Move moveToPlay = FindBestMove(board);

            //ensure move is legal
            if (!board.GetLegalMoves().Contains(moveToPlay)) { throw new Exception("ERROR: Attempted Move: " + moveToPlay.StartSquare + moveToPlay.TargetSquare); }

            return moveToPlay;
        }

        Move FindBestMove(Board board)
        {
            bestEval = -99999;
            bestRootMove = Move.NullMove;
            Search(board, 4, negativeInfinity, positiveInfinity, Move.NullMove);
            return bestRootMove;
        }

        int Search(Board board, int depth, int alpha, int beta, Move rootMove)
        {
            if (depth == 0)
            {
                int eval = SearchCaptures(board, alpha, beta, rootMove);
                if (eval > bestEval && rootMove != Move.NullMove)
                {
                    bestEval = eval;
                    bestRootMove = rootMove;

                    Console.WriteLine("Evil Bot New Best Evaluation: " + eval + " by moving " + rootMove);
                }
                return eval;
            }

            List<Move> moves = OrderMoves(board.GetLegalMoves(), board);
            //either in checkmate or draw
            if (moves.Count == 0)
            {
                if (board.IsInCheck())
                    return negativeInfinity;
                return 0;
            }

            foreach (Move move in moves)
            {
                int evaluation;
                board.MakeMove(move);
                if (rootMove == Move.NullMove)
                    evaluation = -Search(board, depth - 1, -beta, -alpha, move);
                else
                    evaluation = -Search(board, depth - 1, -beta, -alpha, rootMove);
                board.UndoMove(move);
                if (evaluation >= beta)
                    return beta;
                alpha = Math.Max(alpha, evaluation);
            }

            return alpha;
        }

        int SearchCaptures(Board board, int alpha, int beta, Move rootMove)
        {
            //check evaluation see what the position is before capturing for comparison after
            int evaluation = Evaluate(board);
            if (evaluation >= beta)
                return beta;
            alpha = Math.Max(alpha, evaluation);

            List<Move> captureMoves = OrderMoves(board.GetLegalMoves(true), board);

            foreach (Move capMove in captureMoves)
            {
                board.MakeMove(capMove);
                evaluation = -SearchCaptures(board, -beta, -alpha, rootMove);
                board.UndoMove(capMove);
                if (evaluation >= beta)
                    return beta;
                alpha = Math.Max(alpha, evaluation);
            }

            return alpha;
        }

        int Evaluate(Board board)
        {
            int white = 0;
            int black = 0;

            PieceList[] pieces = board.GetAllPieceLists();
            foreach (PieceList pieceList in pieces)
            {
                foreach (Piece piece in pieceList)
                {
                    if (piece.IsWhite)
                    {
                        white += pieceValues[(int)piece.PieceType];
                    }
                    else
                    {
                        black += pieceValues[(int)piece.PieceType];
                    }
                }
            }
            int whosTurn = board.IsWhiteToMove ? 1 : -1;

            return ((white - black) * whosTurn) / 100;
        }

        bool IsPawnAttacking(Square targetSquare, ulong pawnAttackBoard)
        {
            //check if move is in
            if (BitboardHelper.SquareIsSet(pawnAttackBoard, targetSquare))
                return true;
            return false;
        }

        ulong GeneratePawnAttackMap(Board board)
        {
            ulong pawnAttackBoard = 0;

            foreach (Piece piece in board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove))
                pawnAttackBoard = pawnAttackBoard | BitboardHelper.GetPawnAttacks(piece.Square, !board.IsWhiteToMove);

            return pawnAttackBoard;
        }

        int GetPieceValue(PieceType pieceType)
        {
            return pieceValues[(int)pieceType];
        }

        //prioritize high value moves
        //unsure how to implement this into the algorithm
        List<Move> OrderMoves(Move[] moves, Board board)
        {
            List<Tuple<Move, int>> moveOrder = new List<Tuple<Move, int>>();

            pawnAttackBitboard = GeneratePawnAttackMap(board);
            //based on what category a move falls into it should be placed either at the front, middle, or back of the new array
            foreach (Move move in moves)
            {
                int movePriority = 0;
                PieceType movePieceType = move.MovePieceType;
                PieceType capturePieceType = move.CapturePieceType;

                //should capture opponents best pieces
                if (capturePieceType != PieceType.None && movePieceType != PieceType.King)
                {
                    movePriority = 10 * GetPieceValue(capturePieceType) - GetPieceValue(movePieceType);
                }

                //promoting pawns is good
                if (move.IsPromotion)
                {
                    movePriority += GetPieceValue(move.PromotionPieceType);
                }

                //moving into opponents pawns is bad
                bool movingIntoPawnAttack = IsPawnAttacking(move.TargetSquare, pawnAttackBitboard);
                if (movingIntoPawnAttack)
                {
                    movePriority -= GetPieceValue(movePieceType);
                }

                //place move into correct spot in moveOrder
                Tuple<Move, int> moveTuple = new Tuple<Move, int>(move, movePriority);

                bool inserted = false;
                for (int i = 0; i < moveOrder.Count; i++)
                {
                    //if higher priority than another move insert it at that spot
                    if (movePriority >= moveOrder[i].Item2)
                    {
                        moveOrder.Insert(i, moveTuple);
                        inserted = true;
                        break;
                    }
                }
                //if not greater or equal to any of the values in moveOrder add to back
                if (!inserted)
                    moveOrder.Add(moveTuple);
            }

            //get the order but leave the int values
            List<Move> orderedMoveList = new List<Move>();
            foreach (Tuple<Move, int> move in moveOrder)
            {
                orderedMoveList.Add(move.Item1);
            }

            return orderedMoveList;
        }

        //public void ToggleBitboard(Board board)
        //{
        //    return;
        //}

        //public void GenerateBitboard(Board board)
        //{
        //    return;
        //}
    }
}