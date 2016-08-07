open System
open System.Collections.Concurrent

type ICache<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract Set : TimeSpan -> 'TKey -> 'TValue -> unit

type InMemoryCache<'TKey, 'TValue> (concurrentDict: ConcurrentDictionary<'TKey, ('TValue * DateTimeOffset)>) =
    let dict = concurrentDict
    interface ICache<'TKey, 'TValue> with
        member this.TryGet key =
            match dict.TryGetValue key with
            | true, (o, expiryTimestamp) when DateTimeOffset.UtcNow <= expiryTimestamp -> Some o 
            | _ -> None 
        member this.Set expiry key value =
            dict.[key] <- (value, DateTimeOffset.UtcNow + expiry)

let testCache = 
    InMemoryCache 
    <| ConcurrentDictionary ()
    :> ICache<string, string>

TimeSpan.FromSeconds 2.
|> testCache.Set <||
("testKey", "testValue")

let cacheValue = testCache.TryGet "testKey"
