using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int negativeInfinity = -99999;
    int positiveInfinity = 99999;

    //tracking opposing pawn structure
    ulong pawnAttackBitboard;
    
    //testing & debugging
    //ulong myAttackBitboard = 0;
    //bool bitboardOn = false;

    //for selecting best move
    Move bestRootMove;
    int bestEval;

    public Move Think(Board board, Timer timer)
    {
        //get the best move
        Move moveToPlay = FindBestMove(board);

        //update bitboard
        //GenerateAttacks(board, moveToPlay);

        //ensure move is legal
        if (!board.GetLegalMoves().Contains(moveToPlay)) { throw new Exception("ERROR: Attempted Move: " + moveToPlay.StartSquare + moveToPlay.TargetSquare); }

        return moveToPlay;
    }

    Move FindBestMove(Board board)
    {
        // Always play checkmate in one
        foreach (Move move in board.GetLegalMoves())
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
        }

        bestEval = -99999;
        bestRootMove = Move.NullMove;
        Search(board, 4, negativeInfinity, positiveInfinity, Move.NullMove);
        return bestRootMove;
    }

    int Search(Board board, int depth, int alpha, int beta, Move rootMove)
    {
        if (depth == 0)
            return SearchCaptures(board, alpha, beta, rootMove);

        List<Move> moves = OrderMoves(board.GetLegalMoves(), board);
        //either in check or draw
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

        if (alpha > bestEval)
        {
            bestEval = alpha;
            bestRootMove = rootMove;
        }

        return alpha;
    }

    int Evaluate(Board board)
    {
        int white = 0;
        int black = 0;
        int numPieces = 0;

        PieceList[] pieces = board.GetAllPieceLists();
        foreach (PieceList pieceList in pieces)
        {
            foreach (Piece piece in pieceList)
            {
                numPieces++;
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

        //float endgameWeight = numPieces < 10 ? (float)1 / (float)(numPieces + 64) : 0;
        //int kingToCornerWeight = ForceKingToCorner(board.GetPieceList(PieceType.King, board.IsWhiteToMove)[0].Square.Index, board.GetPieceList(PieceType.King, !board.IsWhiteToMove)[0].Square.Index, endgameWeight);

        int whosTurn = board.IsWhiteToMove ? 1 : -1;
        Console.WriteLine("Endgame Weight: " + endgameWeight + " | King To Corner Weight: " + kingToCornerWeight);

        return ((white - black) * whosTurn) / 100;
        //return ((white - black + kingToCornerWeight) * whosTurn) / 100;
    }

    int ForceKingToCorner(int friendlyKingSquare, int opponentKingSquare, float endgameWeight)
    {
        int evaluation = 0;

        //prioritize positions where the opposing king has been forced from the center
        int opponentKingRank = opponentKingSquare >> 3;
        int opponentKingFile = opponentKingSquare & 0b000111;

        int oppKingDstToCenterRank = Math.Max(3 - opponentKingRank, opponentKingRank - 4);
        int oppKingDstToCenterFile = Math.Max(3 - opponentKingFile, opponentKingFile - 4);
        int oppKingDstFromCenter = oppKingDstToCenterFile + oppKingDstToCenterRank;
        evaluation += oppKingDstFromCenter;

        //incentivise moving king closer to opponent king
        int friendlyKingRank = friendlyKingSquare >> 3;
        int friendlyKingFile = friendlyKingSquare & 0b000111;

        int dstBetweenKingsRank = Math.Abs(friendlyKingRank - opponentKingRank);
        int dstBetweenKingsFile = Math.Abs(friendlyKingFile - opponentKingFile);
        int dstBetweenKings = dstBetweenKingsRank + dstBetweenKingsFile;
        evaluation += 14 - dstBetweenKings;

        return (int)(evaluation * 10 * endgameWeight);
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

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
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
            if (capturePieceType != PieceType.None)
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
