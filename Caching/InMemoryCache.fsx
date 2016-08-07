open System
open System.Collections.Concurrent

type ICache<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract Get : (unit -> 'TValue) -> 'TKey -> 'TValue
    abstract Set : TimeSpan -> 'TKey -> 'TValue -> unit

type InMemoryCache<'TKey, 'TValue> (concurrentDict: ConcurrentDictionary<'TKey, ('TValue * DateTimeOffset)>) =
    interface ICache<'TKey, 'TValue> with
        member this.TryGet key =
            match concurrentDict.TryGetValue key with
            | true, (o, expiryTimestamp) when DateTimeOffset.UtcNow <= expiryTimestamp -> Some o 
            | _ -> None 
        member this.Get getter key =
            match (this :> ICache<'TKey, 'TValue>).TryGet key with
            | Some o -> o 
            | None -> getter ()
        member this.Set expiry key value =
            concurrentDict.[key] <- (value, DateTimeOffset.UtcNow + expiry)

let testCache = 
    InMemoryCache 
    <| ConcurrentDictionary ()
    :> ICache<string, string>

TimeSpan.FromSeconds 2.
|> testCache.Set <||
("testKey", "testValue")

let cacheValue = testCache.TryGet "testKey"
