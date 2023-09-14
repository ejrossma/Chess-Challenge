using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int negativeInfinity = int.MinValue;
    int positiveInfinity = int.MaxValue;

    //tracking opposing pawn structure
    ulong pawnAttackBitboard;
    
    //testing & debugging
    ulong myAttackBitboard = 0;
    bool bitboardOn = false;

    public Move Think(Board board, Timer timer)
    {
        //Test SimpleSearch
        //Console.WriteLine(SimpleSearch(board, 4));

        //get the best move
        Move moveToPlay = FindBestMove(board);
        //update bitboard
        //GenerateAttacks(board, moveToPlay);
        //ensure move is legal
        if (!board.GetLegalMoves().Contains(moveToPlay)) { throw new Exception("ERROR: I messed smt up"); }

        return moveToPlay;
    }

    Move FindBestMove(Board board)
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

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }
        return moveToPlay;
    }

    int SimpleSearch(Board board, int depth)
    {
        if (depth == 0)
            return Evaluate(board);

        Move[] moves = board.GetLegalMoves();
        //either in check or draw
        if (moves.Length == 0)
        {
            if (board.IsInCheck())
                return negativeInfinity;
            return 0;
        }

        int bestEvaluation = negativeInfinity;

        foreach(Move move in moves)
        {
            board.MakeMove(move);
            int evaluation = -SimpleSearch(board, depth - 1);
            bestEvaluation = Math.Max(evaluation, bestEvaluation);
            board.UndoMove(move);
        }

        return bestEvaluation;
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
        board.ForceSkipTurn();
        foreach (Move move in board.GetLegalMoves())
        {
            if (move.MovePieceType == PieceType.Pawn)
            {
                pawnAttackBoard = pawnAttackBoard | BitboardHelper.GetPawnAttacks(move.StartSquare, board.IsWhiteToMove);
            }
        }
        board.ForceSkipTurn();
        return pawnAttackBoard;
    }

    public void GenerateAttacks(Board board, Move moveToPlay)
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
        if (bitboardOn)
            BitboardHelper.VisualizeBitboard(myAttackBitboard);
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

    //int SearchForMoves(Board board, int depth, int alpha, int beta)
    //{
    //    //cuts off when there could be a blunder
    //    //need to check for captures
    //    if (depth == 0)
    //        return Evaluate(board);

    //    List<Move> moves = OrderMoves(board.GetLegalMoves(), board);
    //    //either in check or draw
    //    if (moves.Count == 0)
    //    {
    //        if (board.IsInCheck())
    //            return negativeInfinity;
    //        return 0;
    //    }

    //    foreach (Move move in moves)
    //    {
    //        board.MakeMove(move);
    //        int materialCount = -SearchForMoves(board, depth - 1, -beta, -alpha);
    //        board.UndoMove(move);
    //        if (materialCount >= beta)
    //        {
    //            //opponent will avoid this outcome so skip rest of move sequence
    //            return beta;
    //        }
    //        alpha = Math.Max(alpha, materialCount);
    //    }

    //    return alpha;
    //}

    //prioritize high value moves
    //unsure how to implement this into the algorithm
    //List<Move> OrderMoves(Move[] moves, Board board)
    //{
    //    List<Tuple<Move, int>> moveOrder = new List<Tuple<Move, int>>();
    //    pawnAttackBitboard = GeneratePawnAttackMap(board);
    //    //based on what category a move falls into it should be placed either at the front, middle, or back of the new array
    //    foreach (Move move in moves) 
    //    {
    //        int movePriority = 0;
    //        PieceType movePieceType = move.MovePieceType;
    //        PieceType capturePieceType = move.CapturePieceType;

    //        //should capture opponents best pieces
    //        if (capturePieceType != PieceType.None)
    //        {
    //            movePriority = 10 * GetPieceValue(capturePieceType) - GetPieceValue(movePieceType);
    //        }

    //        //promoting pawns is good
    //        if (move.IsPromotion)
    //        {
    //            movePriority += GetPieceValue(move.PromotionPieceType);
    //        }

    //        //moving into opponents pawns is bad
    //        bool movingIntoPawnAttack = IsPawnAttacking(move.TargetSquare, pawnAttackBitboard);
    //        if (movingIntoPawnAttack)
    //        {
    //            movePriority -= GetPieceValue(movePieceType);
    //        }

    //        Console.WriteLine("Move being inserted into Tuple: " + move);
    //        //place move into correct spot in moveOrder
    //        Tuple<Move, int> moveTuple = new Tuple<Move, int>(move, movePriority);
    //        bool inserted = false;
    //        for (int i = 0; i < moveOrder.Count; i++)
    //        {
    //            //if higher priority than another move insert it at that spot
    //            if (movePriority >= moveOrder[i].Item2)
    //            {
    //                moveOrder.Insert(i, moveTuple);
    //                Console.WriteLine("Move being inserted into moveOrder: " + moveTuple.Item1);
    //                inserted = true;
    //                break;
    //            }
    //        }

    //        //if not greater or equal to any of the values in moveOrder add to back
    //        if (!inserted)
    //        {
    //            Console.WriteLine("Move being inserted into moveOrder: " + moveTuple.Item1);
    //            moveOrder.Add(moveTuple);
    //        }
    //    }

    //    //get the order but leave the int values
    //    List<Move> orderedMoveList = new List<Move>();
    //    foreach (Tuple<Move, int> move in moveOrder)
    //    {
    //        orderedMoveList.Add(move.Item1);
    //    }

    //    return orderedMoveList;
    //}
}
