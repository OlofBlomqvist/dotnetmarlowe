module dotnetmarlowe.Types

// Based on:
// https://github.com/input-output-hk/marlowe/blob/master/src/Language/Marlowe/Semantics.hs
// https://alpha.marlowe.iohkdev.io/doc/marlowe/tutorials/marlowe-data.html

type RAW() = inherit System.Attribute()

type SlotValue = 
    | [<RAW>] SlotConstant of int64 
    | SlotInterval of SlotValue * SlotValue
    | SlotIntervalStart of SlotValue
    | SlotParam of string
    | SlotIntervalEnd of SlotValue
    with static member FromInt64(x:int64) = SlotConstant(x)
         
and Party = 
    | PK of string
    | Role of string
    | Account of Party
    | Party of Party
           
and ChoiceName = string
and NumAccount = int64
and Timeout = Slot
and ChosenNum = ChosenNum of int64
and Bound = | Bound of int64 * int64
and ChoiceId = ChoiceId of ChoiceName * Party
and ValueId = string
and Payment = Party * Party * Token * Value
  
and Token =
    | Token of string * string
    static member Ada() =
        Token("","")
    
and ScaleOperator = 
    | ScaleOperator of string
    with 
        override x.ToString() =
            match x with 
            | ScaleOperator s -> s

and Value = 
    | AvailableMoney of Party * Token
    | Constant of int64
    | ConstantParam of string
    | NegValue of Value
    | AddValue of Value * Value
    | SubValue of Value * Value
    | MulValue of Value * Value
    | Scale of (int64 * ScaleOperator * int64) * Value
    | ChoiceValue of ChoiceId
    | UseValue of ValueId
    | Cond of Observation * Value * Value

and Observation = 
    | AndObs of Observation * Observation
    | OrObs of Observation * Observation
    | NotObs of Observation
    | ChoseSomething of ChoiceId
    | ValueGE of Value * Value
    | ValueGT of Value * Value
    | ValueLT of Value * Value
    | ValueLE of Value * Value
    | ValueEQ of Value * Value
    | TrueObs
    | FalseObs

and Action = 
    | Deposit of Party * Party * Token * Value
    | Choice of ChoiceId * Bound list
    | Notify of Observation

and Case = 
    | Case of Action * Contract
    | IgnoreMe
    
and Contract = 
  | Close
  | Pay of Payment * Contract
  | If of Observation * Contract * Contract
  | When of Case list * SlotValue * Contract
  | Let of ValueId * Value * Contract
  | Assert of Observation * Contract
