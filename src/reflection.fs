module dotnetmarlowe.Reflection

    open System

    let createNTuple (t:Type option) (items:obj[]) =
        if t.IsSome then 
            Reflection.FSharpValue.MakeTuple(items, t.Value) |> unbox
        else
            let types = Microsoft.FSharp.Reflection.FSharpType.MakeTupleType(items|>Seq.map(fun x -> x.GetType())|>Seq.toArray)
            Reflection.FSharpValue.MakeTuple(items, types) |> unbox

    type ListHelper =
        static member EmptyList<'T> (items:obj[]) : 'T list = [ for item in items do yield item :?> 'T ]
        static member ListMagicFuckery<'T> (x:obj) = [for item in (x :?> 'T list) do yield item :> obj ]
        static member ToGenericList (items:obj) =
            let genericThingyType = items.GetType().GenericTypeArguments.[0]
            typeof<ListHelper>.GetMethod(nameof(ListHelper.ListMagicFuckery))
                .MakeGenericMethod(genericThingyType)
                .Invoke(null, [|items|]) :?> obj list 
        
    let createListWithTypeInfo (t:Type) (items:obj[]) =
        typeof<ListHelper>
            .GetMethod(nameof(ListHelper.EmptyList))
            .MakeGenericMethod(t)
            .Invoke(null, [|items|])