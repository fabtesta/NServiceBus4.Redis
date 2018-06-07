# NServiceBus4.Redis [![Build status](https://ci.appveyor.com/api/projects/status/i2b2phpdhk6boq5q?svg=true)](https://ci.appveyor.com/project/fabtesta/nservicebus4-redis) [![NuGet Status](http://img.shields.io/nuget/v/NServiceBus.Redis.svg)](https://www.nuget.org/packages/NServiceBus.Redis/)

Redis Persistence for NServiceBus 4.4.x.  
Version 1.x supports only TimeoutManager (IPersistTimeouts).  

Redis connection is based on [`ServiceStack.Redis`](https://github.com/ServiceStack/ServiceStack.Redis) library latest .NET 4.5 [`version`](https://github.com/ServiceStack/ServiceStack.Redis/tree/v5.1.0).

### Installation
* Get the source and build locally  
or  
* Install the [`NServiceBus.Redis`](https://www.nuget.org/packages/NServiceBus.Redis/) NuGet package using the Visual Studio NuGet Package Manager or Package Manager
```powershell
Install-Package NServiceBus.Redis -Version 1.1.0
```

### Configuration
After adding a reference to it from your project, specify `RedisPersistence` to be used for persistence.

```csharp
using NServiceBus;
using NServiceBus.Redis;

public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
{
  public void Init()
  {
    var conf = Configure.With();
    conf.UseRedisTimeoutPersister(endpointName);

    //or

    conf.UseRedisTimeoutPersister(endpointName, defaultPollingTimeout = 5); //MINUTES, default 10
  }
}
```

This base configuration connects using  redis settings strings in the app.config.
```xml
 <appSettings>       
        <add key="NServiceBus/Redis/RedisSentinelHosts" value=""/>
        <add key="NServiceBus/Redis/RedisClusterName" value=""/>
        <add key="NServiceBus/Redis/RedisConnectionString" value="redis://localhost?db=0;"/>
 </appSettings>

  ```
It is possible to pass an already configured instance of IRedisClientsManager.
```csharp
using NServiceBus;
using NServiceBus.Redis;

public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
{
  public void Init()
  {
    var conf = Configure.With();
    
    var yourRedisClientsManagerInstance = new RedisManagerPool(redisConnectionString);
    conf.UseRedisTimeoutPersister(endpointName, yourRedisClientsManagerInstance);

    //or

    conf.UseRedisTimeoutPersister(endpointName, yourRedisClientsManagerInstance, defaultPollingTimeout = 5); //MINUTES, default 10
  }
}
```
