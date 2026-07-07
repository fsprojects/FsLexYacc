module Syntax

type Id = Id of string

type Expr =
    | Var of string
    | Int of int
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
