module dotnetmarlowe.Serializer

open System
open dotnetmarlowe.Reflection
open dotnetmarlowe.Tokenizer
open dotnetmarlowe.Types

let (|Union|Num64|String|Other|List|Tuple|RawString|) (outerx:obj) =
    
    let t = outerx.GetType()
    
    let isfsharplist = 
        t.FullName.StartsWith("Microsoft.FSharp.Collections.FSharpList")
    
    let rec inner (o:obj) = 
        match o with 
        | xx when isfsharplist -> List xx       
        | :? Types.ScaleOperator as o -> RawString (o.ToString())
        | _ when t |> Microsoft.FSharp.Reflection.FSharpType.IsUnion -> 

            let cases = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases t
            let mutable specialCase = cases |> Seq.tryFind(fun c -> c.Name = t.Name)
            if specialCase.IsNone then
                let zeroFieldCases = cases |> Seq.filter(fun c -> c.GetFields().Length = 0)
                if zeroFieldCases |> Seq.length = 1 then
                    specialCase <- Some(zeroFieldCases|>Seq.head)
                else
                    sprintf "%A? I've never heard of such a thing!" t.FullName  |>failwith
            let fields = specialCase.Value.GetFields()|>Seq.map(fun f -> f.GetValue(o) )|>Seq.toList
            let unionTypeName = specialCase.Value.DeclaringType.Name
            let raw = specialCase.Value.GetCustomAttributesData() |> Seq.tryFind(fun a ->a.AttributeType.Name = "RAW" )
            if raw.IsSome then 
                match fields.[0] with 
                | :? Int64 as n -> Num64 n
                | :? string as s -> String s
                | _ -> sprintf "Expected Int64 or String - Found: %A" 
                        (fields.[0].GetType().Name)
                        |> failwith
            
            else Union(unionTypeName,specialCase.Value.Name, fields)
        | :? string as s -> String s
        | :? Int64 as i -> Num64 i
        | _ when t |> Microsoft.FSharp.Reflection.FSharpType.IsTuple -> Tuple 
        | _ -> Other

    inner outerx

let serializeUnionTypeIntoMarlowe (o : obj) = 
    
    let rec inner (x:obj) (wrap:bool) (first:bool)= 
   
        match x with 
        | Num64 n -> string n
        | RawString s -> s
        | String s -> $"\"{s}\""
        | List l -> 
            let objlist = ListHelper.ToGenericList l
            let resultStrings = [| for item in objlist do yield inner item false false |]
            let items = System.String.Join(",",resultStrings)
            $"[{items}]"
        | Union(name,caseName,fields) ->

            if fields.Length = 0 then sprintf "%s" caseName
            else 
                let fieldstrings = [
                    for f in fields do 
                        // todo : ewww, this is just the worst :<
                        inner f (caseName = "Scale") false ]
                if first then sprintf "%s %s" caseName (System.String.Join(" ",fieldstrings))
                else sprintf "(%s %s)" caseName (System.String.Join(" ",fieldstrings))
        | Tuple -> 
                let tupfields = Reflection.FSharpValue.GetTupleFields(x)
                let tuplevaluestrings = [ for e in tupfields do inner e false false ] 
                let joinedTupString = System.String.Join(" ", tuplevaluestrings)
                if wrap then $"({joinedTupString})"
                else joinedTupString
            
        | Other -> 
            sprintf "Unable to serialize object of type %A" (x.GetType().Name) |> failwith

    inner o false true