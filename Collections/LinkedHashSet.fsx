open System
open System.Collections
open System.Collections.Generic

type LinkedHashSet<'a when 'a : equality> () =
    let mutable index = 0
    let indexed = SortedDictionary<int, 'a> ()
    let hash = HashSet<'a> ()
    let theLock = obj ()

    interface ICollection<'a> with
        member __.Count = lock theLock (fun () -> hash.Count)
        member __.IsReadOnly = false
        member __.Add e =
            lock theLock (fun () -> 
                if hash.Add e
                then
                    indexed.[index] <- e
                    index <- index + 1
            )            
        member __.Clear () =
            lock theLock (fun () ->
                index <- 0
                indexed.Clear ()
                hash.Clear ()
            )            
        member __.Contains e =
            lock theLock (fun () -> hash.Contains e)
        member __.CopyTo (array, arrayIndex) =
            lock theLock (fun () ->
                let kvps = 
                    Seq.toArray indexed
                let kvpLength = Array.length kvps            
                for i in arrayIndex..kvpLength - 1 do
                    array.[i - arrayIndex] <- kvps.[i].Value
            )            
        member __.GetEnumerator () =
            let values = lock theLock (fun () ->
                indexed
                |> Seq.map (fun kvp -> kvp.Value)
            )
            values.GetEnumerator ()           
        member this.GetEnumerator () : IEnumerator =
            (this :> IEnumerable<'a>).GetEnumerator ()
            :> IEnumerator
        member __.Remove e =
            lock theLock (fun () ->
                if hash.Remove e
                then
                    let kvps = indexed :> seq<KeyValuePair<int, 'a>>
                    match Seq.tryFind (fun (kvp: KeyValuePair<int, 'a>) -> kvp.Value = e) kvps with
                    | Some x -> indexed.Remove x.Key
                    | None -> false
                else false
            )