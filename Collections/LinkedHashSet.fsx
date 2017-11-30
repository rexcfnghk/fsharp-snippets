open System
open System.Collections
open System.Collections.Generic

type LinkedHashSet<'a when 'a : equality> (elements) as this =
    let mutable index = 0
    let indexed = SortedDictionary ()
    let hash = HashSet ()
    let theLock = obj ()

    do Seq.iter (this :> ICollection<'a>).Add elements
    new () = LinkedHashSet Seq.empty    

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
            let copyValues (xs: 'a[]) = xs.CopyTo (array, arrayIndex)
            lock theLock (fun () ->
                indexed
                |> Seq.map (fun i -> i.Value)
                |> Seq.toArray
                |> copyValues
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
            let predicate (kvp: KeyValuePair<int, 'a>) = 
                kvp.Value = e
            lock theLock (fun () ->
                if not <| hash.Remove e
                then false
                else
                    match Seq.tryFind predicate indexed with
                    | Some x -> indexed.Remove x.Key
                    | None -> false
            )
