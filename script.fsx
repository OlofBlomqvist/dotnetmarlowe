//#r "src/bin/Debug/net5.0/dotmarlowe.dll"
#load "src/types.fs"
#load "src/reflection.fs"
#load "src/tokenizer.fs"
#load "src/deserializer.fs"
#load "src/serializer.fs"

open dotnetmarlowe.Serializer
open dotnetmarlowe.Deserializer
open dotnetmarlowe.Tokenizer

let args : string array = fsi.CommandLineArgs

type HAX = static member Deserialize<'T>(tok:MI) = ProcessTokens<'T> tok

let genericDeserialize (sample:string) =
    let mutable t = typeof<dotnetmarlowe.Types.Contract>
    let tokenized = TokenizeMarlowe sample
    let autoTypeName = 
        match tokenized with 
        | List l when l.Count = 1 -> 
            match l|>Seq.head with 
            | Union u -> Some u.Name 
            | _ -> None
        | Union u -> Some u.Name
        | _ -> None
    if autoTypeName.IsSome then 
        let types = t.Assembly.GetTypes()
        t <- types |> Seq.find(fun x -> x.Name.ToUpper()=autoTypeName.Value.ToUpper())
    let method = typeof<HAX>.GetMethod(nameof(HAX.Deserialize))
    let generic = method.MakeGenericMethod(t)
    generic.Invoke(null, [|tokenized|]) 

match args with 
| [||]|[|_|] -> 
    printfn "Give a filepath as input.."
| [|selfPath;"test"|] -> 
    let samplePath = selfPath.Split("/")
                    |>Seq.rev
                    |>Seq.skip 1
                    |>Seq.rev
                    |>Seq.toList
                    |>fun plist -> 
                        System.String.Join("/", plist @ ["samples"])
    let samples = System.IO.Directory.GetFiles samplePath
    for file in samples do
        let sample = System.IO.File.ReadAllText file
        let dese1 = genericDeserialize sample 
        let c = dese1|> serializeUnionTypeIntoMarlowe
        let redes = genericDeserialize c
        let roundTwoTok = redes |> serializeUnionTypeIntoMarlowe
        let strip (x:string) = x.Replace("\n","")
                                .Replace("\r\n","")
                                .Replace("\r","")
                                .Replace("\t","")
                                .Replace(" ","")
                                .Replace("(","\n(")
        let strippedOriginal = sample |> strip
        let strippedRedes = roundTwoTok |> strip
        if strippedOriginal = strippedRedes then  printfn "> %s : OK" file
        else
            printfn "Original input: %s" sample
            printfn "Generated output: %s" roundTwoTok
            failwith "Theres a difference.. There should not have been any difference :-("
| [|_;filePath|] when not(System.IO.File.Exists filePath) -> 
    failwith "File not found: {filePath}"          
| [|_;filePath|] ->
    let sample = System.IO.File.ReadAllText filePath
    let timer = System.Diagnostics.Stopwatch.StartNew()
    let result = genericDeserialize sample
    timer.Stop()
    printfn $"Deserialization completed in: {timer.ElapsedMilliseconds}ms" 
    printfn "------------------------------------------"
    printfn $"{result}"
| _ -> failwith "Unknown error"