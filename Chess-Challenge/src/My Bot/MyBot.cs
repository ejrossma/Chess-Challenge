using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Diagnostics;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int negativeInfinity = -99999;
    int positiveInfinity = 99999;

    //tracking opposing pawn structure
    ulong pawnAttackBitboard;

    //tracking best move
    Move bestMove = Move.NullMove;
    int bestEval = -99999;

    struct SearchResult
    {
        public int bestEvaluation;
        public Move bestMoveThisSearch;
    }

    public Move Think(Board board, Timer timer)
    {
        //get the best move
        Move moveToPlay = FindBestMove(board);

        //ensure move is legal
        if (!board.GetLegalMoves().Contains(moveToPlay)) { throw new Exception("ERROR: Attempted Move: " + moveToPlay.StartSquare + moveToPlay.TargetSquare); }

        pawnAttackBitboard = GeneratePawnAttackMap(board);
        BitboardHelper.VisualizeBitboard(pawnAttackBitboard);

        return moveToPlay;
    }

    Move FindBestMove(Board board)
    {
        return Search(board, 4, negativeInfinity, positiveInfinity).bestMoveThisSearch;
    }

    SearchResult Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0)
            return SearchCaptures(board, alpha, beta);

        List<Move> moves = OrderMoves(board.GetLegalMoves(), board);
        //either in checkmate or draw
        if (moves.Count == 0)
        {
            if (board.IsInCheck())
                return new SearchResult { bestEvaluation = negativeInfinity, bestMoveThisSearch = Move.NullMove };
            return new SearchResult { bestEvaluation = 0, bestMoveThisSearch = Move.NullMove };
        }

        bestMove = Move.NullMove;
        bestEval = negativeInfinity;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int evaluation = -Search(board, depth - 1, -beta, -alpha).bestEvaluation;
            board.UndoMove(move);
            
            if (evaluation >= beta)
                return new SearchResult { bestMoveThisSearch = Move.NullMove, bestEvaluation = beta };
            
            alpha = Math.Max(alpha, evaluation);
            if (alpha >= bestEval)
            {
                bestEval = alpha;
                bestMove = move;
            }
        }
        
        return new SearchResult { bestMoveThisSearch = bestMove, bestEvaluation = bestEval };
    }

    SearchResult SearchCaptures(Board board, int alpha, int beta)
    {
        int evaluation = Evaluate(board);
        if (evaluation >= beta)
            return new SearchResult { bestEvaluation = beta, bestMoveThisSearch = Move.NullMove };
        alpha = Math.Max(alpha, evaluation);

        List<Move> captureMoves = OrderMoves(board.GetLegalMoves(true), board);
        bestMove = Move.NullMove;
        bestEval = negativeInfinity;

        foreach (Move capMove in captureMoves)
        {
            board.MakeMove(capMove);
            evaluation = -SearchCaptures(board, -beta, -alpha).bestEvaluation;
            board.UndoMove(capMove);
            
            if (evaluation >= beta)
                return new SearchResult { bestMoveThisSearch = Move.NullMove, bestEvaluation = beta };
            alpha = Math.Max(alpha, evaluation);
            if (alpha >= bestEval)
            {
                bestEval = alpha;
                bestMove = capMove;
            }
        }

        return new SearchResult { bestEvaluation = bestEval, bestMoveThisSearch = bestMove };
    }

    int Evaluate(Board board)
    {
        int white = 0;
        int black = 0;
        int numPiecesWhite = 0;
        int numPiecesBlack = 0;

        PieceList[] pieces = board.GetAllPieceLists();
        foreach (PieceList pieceList in pieces)
        {
            foreach (Piece piece in pieceList)
            {
                
                if (piece.IsWhite)
                {
                    numPiecesWhite++;
                    white += pieceValues[(int)piece.PieceType];
                }
                else
                {
                    numPiecesBlack++;
                    black += pieceValues[(int)piece.PieceType];
                }
            }
        }

        float endgameWeight = ( (board.IsWhiteToMove && numPiecesBlack <= 3) || (!board.IsWhiteToMove && numPiecesWhite <= 3) ) ? (float)1 / (float)(64) : 0;
        int kingToCornerWeight = ForceKingToCorner(board.GetPieceList(PieceType.King, board.IsWhiteToMove)[0].Square.Index, board.GetPieceList(PieceType.King, !board.IsWhiteToMove)[0].Square.Index, endgameWeight);

        int whosTurn = board.IsWhiteToMove ? 1 : -1;

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

    bool IsPawnAttacking(Square targetSquare)
    {
        //check if move is in
        if (BitboardHelper.SquareIsSet(pawnAttackBitboard, targetSquare))
            return true;
        return false;
    }

    ulong GeneratePawnAttackMap(Board board)
    {
        ulong pawns = board.GetPieceBitboard(PieceType.Pawn, !board.IsWhiteToMove);
        ulong pawnAttackBoard = 0b0;

        for (int i = 0; i < 64; i++)
        {
            //if value on bitboard at index of i is 1
            if ( (pawns & ((ulong)1 << i) ) != 0)
                pawnAttackBoard = pawnAttackBoard | BitboardHelper.GetPawnAttacks(new Square(i), !board.IsWhiteToMove);
        }

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
            bool movingIntoPawnAttack = IsPawnAttacking(move.TargetSquare);
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
}
