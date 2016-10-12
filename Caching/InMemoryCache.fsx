open System
open System.Collections.Concurrent

type ICache<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract Get : (unit -> 'TValue * TimeSpan) -> 'TKey -> 'TValue
    abstract Set : TimeSpan -> 'TKey -> 'TValue -> unit

type InMemoryCache<'TKey, 'TValue> (concurrentDict: ConcurrentDictionary<'TKey, ('TValue * DateTimeOffset)>) =
    interface ICache<'TKey, 'TValue> with
        member __.TryGet key =
            match concurrentDict.TryGetValue key with
            | true, (o, expiryTimestamp) when DateTimeOffset.UtcNow <= expiryTimestamp -> Some o 
            | _ -> None 
        member __.Get getter key =
            let cache = __ :> ICache<'TKey, 'TValue>
            match cache.TryGet key with
            | Some o -> o 
            | None -> 
                let result, timeSpan = getter ()
                cache.Set timeSpan key result
                result
        member __.Set expiry key value =
            concurrentDict.[key] <- (value, DateTimeOffset.UtcNow + expiry)

let testCache = 
    InMemoryCache 
    <| ConcurrentDictionary ()
    :> ICache<string, string>

TimeSpan.FromSeconds 2.
|> testCache.Set <||
    ("testKey", "testValue")

let cacheValue = testCache.Get (fun _ -> "testValue2", TimeSpan.FromSeconds 2.) "testKey"
