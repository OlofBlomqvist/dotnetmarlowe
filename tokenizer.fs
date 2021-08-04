module dotnetmarlowe.Tokenizer

open System.Collections.Generic

type MiUnionInfo = {
    NameQ:Queue<char>
    Fields:(Queue<MI>)
} with 
    member x.Name with get() = 
        System.String.Join("",x.NameQ)

and MI =
    | Union of MiUnionInfo
    | List of Queue<MI>
    | StrLit of Queue<char> 
    | Num of Queue<char> 
    | Operator of char

let (|Int|_|) (x:char list) =
    match System.Int64.TryParse (x.Head.ToString()) with
    | true,_ -> Some (x.Head , x.Tail)
    | _ -> None

let TokenizeMarlowe (serialized:string) =
    
    let text = serialized
                .Replace("\r","")
                .Replace("\n","")
                .Replace("\t","")
                .Replace("("," ( ")
                .Replace(")"," ) ")
                .Replace("]"," ] ")
                .Replace("["," [ ")
                .Replace("  "," ")

    let mutable stack = Stack<MI>()
    stack.Push(Queue<MI>()|>MI.List)

    let foldBack () =
        let leaf = stack.Pop()
        match stack.Peek() with
        | MI.List items -> items.Enqueue leaf
        | MI.Union u -> u.Fields.Enqueue leaf
        | _ -> failwith "unable to invoke foldBack due to unexpected parent type."

    let rec inner (remainder:char list) =
                
        let foldAndContinue t = 
            foldBack()
            inner t

        match remainder, stack.Peek() with 
        | [], StrLit _ -> failwith "Expected string literal - found end of file."
        | [],Num _ -> failwith "Expected number - found end of file."
        | [] , _ -> while stack.Count > 1 do foldBack()     
                    stack.Pop()
        | '"'::t ,  StrLit _ -> t |> foldAndContinue
        | '"'::t ,  Union _ ->
            Queue<char>() |> MI.StrLit |> stack.Push
            inner t
        | h::t , MI.StrLit s -> 
            s.Enqueue h
            inner t 
        | Int(h,t) , Union u -> 
            let newNum = Queue<char>()
            newNum.Enqueue h
            let newItem = MI.Num (newNum)
            stack.Push newItem
            inner t
        | Int(h,t) , Num nn -> 
            nn.Enqueue h
            inner t
        | ' '::t , Num _ -> 
            t |> foldAndContinue
        | ' '::t , Union u when u.NameQ.Count = 0 ->
            inner t
        | ']'::t, Union u ->
            foldBack()
            foldBack()
            inner t
        | ']'::t, Num _ -> 
            foldBack()
            foldBack()
            inner t
        | ')'::t, Union u -> 
            if u.Fields.Count = 0 then foldBack()
            t |> foldAndContinue
        | ']'::t, List _ -> 
            t  |> foldAndContinue
        | '('::t, _ ->
            let newItem = MI.Union { NameQ=Queue<char>(); Fields = Queue<MI>() }
            stack.Push(newItem)
            inner t
        | '['::t, _ -> 
            let newItem = MI.List (Queue<MI>())
            stack.Push(newItem)
            inner t
        | ','::t, MI.List l -> inner t
        | '%'::t, MI.Union u -> 
            MI.Operator '%' |> u.Fields.Enqueue
            inner t
        | (h::t , Union u) when (h <> ' ' && u.Fields.Count > 0) -> 
            let q = Queue<char>()
            q.Enqueue h
            let newItem = MI.Union { NameQ=q; Fields = Queue<MI>() }
            stack.Push(newItem)
            inner t
        | h::t , Union u when h <> ' ' -> 
            u.NameQ.Enqueue h
            inner t
        | ' '::t , Union u when u.NameQ.Count > 0 -> inner t
        | h::t, List l when h = ' ' -> inner t
        | h::t, List l when h <> ' ' && h <> ')' -> 
            let q = Queue<char>()
            q.Enqueue h
            let newItem = MI.Union { NameQ=q; Fields = Queue<MI>() }
            stack.Push(newItem)
            inner t
        | x::_,List l -> 
            sprintf "unexpected character in list --> %A ... remaining: %A" x (System.String.Join("",remainder)) |> failwith
        | x::_,y -> 
            failwith $"unexpected character --> {x} "

    text |> Seq.toList |> inner