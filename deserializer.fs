module dotnetmarlowe.Deserializer
open dotnetmarlowe.Tokenizer
open dotnetmarlowe.Reflection
open dotnetmarlowe.Types
open System    
open System.Collections.Generic

let ProcessTokens<'T> (tokenized:MI) =
    let qStr (x:Queue<char>) = System.String.Join("",x)
    let types = (typeof<'T>).Assembly.GetTypes()
    let rec deserializeMI (item:MI) =
        match item with
        | List l -> [|for x in l do yield deserializeMI x |] |> box
        | Operator o -> Types.ScaleOperator(o.ToString()) :> obj
        | Union u -> 

            let mutable t = 
                if u.Name = "" then Some typeof<Tuple> 
                else types |> Seq.tryFind(fun t -> t.Name = u.Name)

            if t.IsNone then 

                let possibleCases =
                    types 
                    |> Seq.filter(Microsoft.FSharp.Reflection.FSharpType.IsUnion)
                    |> Seq.collect(fun tt ->
                        let typecases = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases tt
                        typecases |> Seq.filter(fun tc -> tc.Name = u.Name)) 
                    
                let nrOne = possibleCases |> Seq.tryHead

                if nrOne.IsSome then
                    let isTheSame = possibleCases |> Seq.forall (fun pc -> pc.DeclaringType = nrOne.Value.DeclaringType)
                    if isTheSame then t <- Some nrOne.Value.DeclaringType 
                    else failwith $"There are multiple possible types to create for: {u.Name}"
                else failwith $"Unable to find a type to create for: {u.Name}"

            let mutable unionFieldItems = [| for x in u.Fields do yield deserializeMI x|]

            if t.Value = typeof<Tuple> then createNTuple None unionFieldItems 
            else
                if not(Microsoft.FSharp.Reflection.FSharpType.IsUnion t.Value) then
                    failwith $"The reference type is not a union! ({u.Name})"
                let cases = Microsoft.FSharp.Reflection.FSharpType.GetUnionCases t.Value
                let specialCase = cases |> Seq.tryFind(fun c -> c.Name = u.Name)
                if specialCase.IsNone then 
                    sprintf "no case exists for this. from contract text: %A , the type we found via reflection: %A , the input item: %A" 
                        u.Name t.Value.Name item |> failwith
                let fields = specialCase.Value.GetFields()
                let constructorArgumentObjects = Array.zeroCreate (fields.Length)
                let mutable packagedArguments = 
                    if unionFieldItems.Length > fields.Length then 
                        let mutable count = 0
                        let mutable loopcount = 0
                        [|
                            for field in fields do
                                if Microsoft.FSharp.Reflection.FSharpType.IsTuple(field.PropertyType) then
                                    let els = Microsoft.FSharp.Reflection.FSharpType.GetTupleElements(field.PropertyType)
                                    let items = unionFieldItems.[count..els.Length - 1]
                                    count <- count + els.Length
                                    yield createNTuple (Some field.PropertyType) (seq{for x in items do yield box x}|>Seq.toArray)
                                    
                                else
                                    yield unionFieldItems.[count]
                                    count <- count + 1
                                loopcount <- loopcount + 1
                        |] 
                    else unionFieldItems

                packagedArguments |> Seq.iteri(fun i o -> 
                    let f = fields.[i]
                    let int64Converter = // because in some places an raw int64 should get turned into Value.Constant(x)
                        f.PropertyType.GetMethods() |> Seq.tryFind(fun f -> f.Name.ToUpper() = "FROMINT64")
                    
                    if Microsoft.FSharp.Reflection.FSharpType.IsTuple(o.GetType()) && not(Microsoft.FSharp.Reflection.FSharpType.IsTuple(f.PropertyType)) then
                        let elstypes = Microsoft.FSharp.Reflection.FSharpType.GetTupleElements(o.GetType())
                        if elstypes.Length = 1 && elstypes.[0] = f.PropertyType then
                            let elval = Microsoft.FSharp.Reflection.FSharpValue.GetTupleField(o,0)
                            constructorArgumentObjects.SetValue(value=elval,index=i)
                        else 
                            failwith "unexpected tuple item."
                    elif o.GetType() =  typeof<System.Int64> && int64Converter.IsSome then 
                        let converted = int64Converter.Value.Invoke(null,[|o|])
                        constructorArgumentObjects.SetValue(value=converted,index=i)
                    elif f.PropertyType =  typeof<System.Int64> || f.PropertyType = typeof<System.String> then
                        constructorArgumentObjects.SetValue(index=i,value=o)
                    elif o.GetType() = typeof<_[]> then
                        let eltyp = f.PropertyType.GetGenericArguments().[0]
                        let mylist = createListWithTypeInfo (eltyp) (o:?>_[]) 
                        constructorArgumentObjects.SetValue(value=(mylist),index=i)
                    else constructorArgumentObjects.SetValue(value=o,index=i)
                )

                Microsoft.FSharp.Reflection.FSharpValue.MakeUnion(unionCase=specialCase.Value,args=constructorArgumentObjects)
                
        | StrLit s -> System.String.Join("",s) :> obj
        | Num n -> System.String.Join("",n) |> System.Int64.Parse :> obj
    (deserializeMI tokenized :?> obj []).[0] :?> 'T

let DeserializeMarlowe<'T> (text:string) = 
    text |> Tokenizer.TokenizeMarlowe |> ProcessTokens<'T>