open System
open System.Collections.Concurrent

type ICache<'TKey, 'TValue> =
    abstract TryGet : 'TKey -> 'TValue option
    abstract Set : TimeSpan -> 'TKey -> 'TValue -> unit

let createInMemoryCache () =
    let dict = ConcurrentDictionary ()
    { new ICache<_, _> with
        member this.TryGet key =
            match dict.TryGetValue key with
            | true, (o, expiryTimestamp) when DateTimeOffset.UtcNow <= expiryTimestamp -> Some o 
            | _ -> None 
        member this.Set expiry key value =
            dict.[key] <- (value, DateTimeOffset.UtcNow + expiry) }

let testCache = createInMemoryCache ()

TimeSpan.FromSeconds 2.
|> testCache.Set <||
("testKey", "testValue")

let cacheValue = testCache.TryGet "testKey"
